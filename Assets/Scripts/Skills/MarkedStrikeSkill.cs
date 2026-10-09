using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class MarkedStrikeSkillData : SkillDataBase
{
    // Time before each mark, counted from the previous strike (or from the start of the battle), with the
    // SkillCooldownMultiplier of the entity applied
    public float interval = 8f;
    // Time between the mark and the strike: the time the player has to protect the marked units
    public float delay = 3f;
    // Which units are marked, among the enemies of the entity in range
    public TargetBehaviourType targetBehaviourType = TargetBehaviourType.HighestHealth;
    // Part of the strike taken by each marked unit, in the order of the targeting: one unit marked per value
    // (1, 0.5, 0.25: the first unit takes the whole strike, the second half of it, the third a quarter)
    public List<float> targetDamageMultipliers = new List<float> { 1f };
    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<ATargetValidatorFactory>, ATargetValidatorFactory>(targetValidators)")]
    public List<ATargetValidatorFactory> targetValidators = new List<ATargetValidatorFactory>();
    public float range = 100f;
    // Damage of the strike, applied like any hit (armor, invincibility and shields apply)
    [CreateDataButton]
    public AConsumerFactory strikeConsumer;
    // Extra strike damage at 0 health of the entity, growing linearly with its missing health (0.5: x1.5)
    [Min(0f)] public float missingHealthDamageBonus = 0f;
    // Optional, put on the marked units until the strike (e.g. an icon over their health bar)
    [CreateDataButton]
    public ABuffHandlerFactory markBuffHandler;

    [Header("Visuals")]
    // Put on each marked unit until the strike, smaller on the units taking less damage, the seconds left on the
    // main one
    public StrikeMarker markerPrefab;
    // Arc from the entity to the main marked unit, then from each marked unit to the next, its dashes moving along
    public LineRenderer arcPrefab;
    public Color arcColor = Color.red;
    public float arcHeightPerDistance = 0.35f;
    [Min(0.01f)] public float arcDashLength = 0.3f;
    public float arcScrollSpeed = 1.2f;
    // Played where the strike lands
    public GameObject impactPrefab;
    // Camera shake when the strike lands, 0 for none
    [Min(0f)] public float impactShakeForce = 0.4f;
    [Min(0.01f)] public float impactShakeDuration = 0.25f;

    public bool hasVisuals => markerPrefab != null || arcPrefab != null || impactPrefab != null || impactShakeForce > 0f;

    // Units marked by the skill: one per damage multiplier, at least one
    public int targetCount => Mathf.Max(1, targetDamageMultipliers != null ? targetDamageMultipliers.Count : 0);

    // Part of the strike taken by the unit marked at this rank of the targeting, the whole strike without value
    public float GetTargetDamageMultiplier(int rank)
    {
        return targetDamageMultipliers != null && rank >= 0 && rank < targetDamageMultipliers.Count ? targetDamageMultipliers[rank] : 1f;
    }
}

// A unit marked by a MarkedStrikeSkill and the part of the strike it takes
public struct StrikeMark
{
    public GameObject target;
    public float damageMultiplier;
}

// Telegraphed strike: marks one or several units, then strikes them hard a few seconds later, each for its part
// of the strike. The strike on a unit is lost if it dies or disappears before it lands.
public class MarkedStrikeSkill : ASkill<MarkedStrikeSkillData>, ICooldownSkill
{
    public UnityEvent<GameObject> OnMarked = new UnityEvent<GameObject>();
    public UnityEvent<GameObject> OnStrike = new UnityEvent<GameObject>();

    ATargetBehaviour _targetBehaviour;
    Entity _owner;
    float _timer = 0f;
    readonly List<StrikeMark> _marks = new List<StrikeMark>();
    // Kept apart from _marks: a destroyed unit compares equal to null for Unity
    bool _isMarking = false;

    // In the order of the targeting, the unit taking the whole strike first
    public IReadOnlyList<StrikeMark> marks => _marks;
    // The first marked unit still there, null when there is none
    public GameObject markedTarget => _marks.Find(mark => mark.target != null).target;
    public bool isMarking => _isMarking;
    // Seconds left before the strike, 0 when nothing is marked
    public float remainingDelay => isMarking ? Mathf.Max(0f, data.delay - _timer) : 0f;
    // Seconds left before the next mark, 0 while units are marked
    public float remainingInterval => isMarking ? 0f : Mathf.Max(0f, interval - _timer);
    // Time between a strike and the next mark, shortened by the SkillCooldownMultiplier of the entity
    public float interval => DurationValidator.GetDuration(data.interval, gameObject);
    // Multiplier of the strike damage, growing as the entity loses health
    public float strikeMultiplier
    {
        get
        {
            Entity owner = GetComponent<Entity>();
            float missingPercent = owner != null && owner.health != null ? 1f - owner.health.percent : 0f;
            return 1f + data.missingHealthDamageBonus * missingPercent;
        }
    }

    // The data is set right after AddComponent, so after Awake: the targeting is built once here
    void Start()
    {
        _owner = GetComponent<Entity>();
        _targetBehaviour = ATargetBehaviour.Create(data.targetBehaviourType);
        foreach (ATargetValidatorFactory targetValidator in data.targetValidators)
        {
            _targetBehaviour.targetValidators.Add(targetValidator.GetTargetValidator());
        }

        if (data.hasVisuals)
        {
            gameObject.AddComponent<MarkedStrikeView>().Init(this);
        }
    }

    public override void UpdateBehaviour(GameObject source)
    {
        Tick(Time.deltaTime);
    }

    // Part of the strike the unit takes, 0 when it isn't marked
    public float GetDamageMultiplier(GameObject target)
    {
        foreach (StrikeMark mark in _marks)
        {
            if (target != null && mark.target == target)
            {
                return mark.damageMultiplier;
            }
        }
        return 0f;
    }

    public void Tick(float deltaTime)
    {
        // A marked unit left the game before the strike (killed by something else): its part of the strike is
        // lost, the whole strike when every marked unit left
        if (_isMarking)
        {
            _marks.RemoveAll(mark => mark.target == null);
            if (_marks.Count == 0)
            {
                ClearMark();
                _timer = 0f;
            }
        }

        // The time beyond the interval or the delay counts for the next one, whatever the frame rate. When nobody
        // could be marked for a while, at most the time of this update is kept
        _timer += deltaTime;
        if (!isMarking)
        {
            List<GameObject> targets = _timer >= interval ? FindTargets(data.targetCount) : null;
            // Nobody to mark: tries again on the next frame
            if (targets != null && targets.Count > 0)
            {
                _timer = Mathf.Min(_timer - interval, deltaTime);
                Mark(targets);
            }
        }
        else if (_timer >= data.delay)
        {
            _timer -= data.delay;
            Strike();
        }
    }

    public override void Reset()
    {
        ClearMark();
        _timer = 0f;
    }

    // The units picked by the targeting of the skill, at most count of them, empty when nobody is in range
    protected virtual List<GameObject> FindTargets(int count)
    {
        _targetBehaviour.targetCount = count;
        return new List<GameObject>(_targetBehaviour.GetTargets(gameObject, transform.position, data.range, _owner.GetTargetType()));
    }

    void Mark(List<GameObject> targets)
    {
        _isMarking = true;
        for (int rank = 0; rank < targets.Count && rank < data.targetCount; rank++)
        {
            GameObject target = targets[rank];
            _marks.Add(new StrikeMark { target = target, damageMultiplier = data.GetTargetDamageMultiplier(rank) });
            if (data.markBuffHandler != null)
            {
                BuffManager buffManager = target.GetComponent<BuffManager>();
                if (buffManager != null)
                {
                    buffManager.AddHandler(data.markBuffHandler, gameObject, target);
                }
            }
            OnMarked.Invoke(target);
        }
    }

    void Strike()
    {
        List<StrikeMark> struck = new List<StrikeMark>(_marks);
        ClearMark();

        foreach (StrikeMark mark in struck)
        {
            Entity entity = mark.target != null ? mark.target.GetComponent<Entity>() : null;
            if (entity == null || entity.health == null || entity.health.Value <= 0f || data.strikeConsumer == null)
            {
                continue;
            }

            entity.health.AddResourceModifier(ResourceModifier.Create(data.strikeConsumer, gameObject, mark.target, strikeMultiplier * mark.damageMultiplier));
            OnStrike.Invoke(mark.target);
        }
    }

    void ClearMark()
    {
        foreach (StrikeMark mark in _marks)
        {
            if (mark.target == null || data.markBuffHandler == null)
            {
                continue;
            }

            BuffManager buffManager = mark.target.GetComponent<BuffManager>();
            // Only a mark still there: removing an expired one would leave a dead entry blocking the next mark
            if (buffManager != null && buffManager.GetActiveHandlers().Exists(handler => handler.buffHandlerFactory == data.markBuffHandler && handler.source == gameObject))
            {
                buffManager.RemoveHandler(data.markBuffHandler, gameObject, mark.target, true);
            }
        }
        _marks.Clear();
        _isMarking = false;
    }

    #region ICooldownSkill

    // The cooldown is the interval between a strike and the next mark
    public float cooldownDuration => interval;
    public float cooldownProgress => interval > 0f ? remainingInterval / interval : 0f;

    #endregion
}
