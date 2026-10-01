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
    // Time between the mark and the strike: the time the player has to protect the marked unit
    public float delay = 3f;
    // Which unit is marked, among the enemies of the entity in range
    public TargetBehaviourType targetBehaviourType = TargetBehaviourType.HighestHealth;
    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<ATargetValidatorFactory>, ATargetValidatorFactory>(targetValidators)")]
    public List<ATargetValidatorFactory> targetValidators = new List<ATargetValidatorFactory>();
    public float range = 100f;
    // Damage of the strike, applied like any hit (armor, invincibility and shields apply)
    [CreateDataButton]
    public AConsumerFactory strikeConsumer;
    // Extra strike damage at 0 health of the entity, growing linearly with its missing health (0.5: x1.5)
    [Min(0f)] public float missingHealthDamageBonus = 0f;
    // Optional, put on the marked unit until the strike (e.g. an icon over its health bar)
    [CreateDataButton]
    public ABuffHandlerFactory markBuffHandler;

    [Header("Visuals")]
    // Put on the marked unit until the strike, with the seconds left
    public StrikeMarker markerPrefab;
    // Arc from the entity to the marked unit, its dashes moving toward it
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
}

// Telegraphed strike: marks one unit, then strikes it hard a few seconds later. The strike is lost if the
// marked unit dies or disappears before it lands.
public class MarkedStrikeSkill : ASkill<MarkedStrikeSkillData>, ICooldownSkill
{
    public UnityEvent<GameObject> OnMarked = new UnityEvent<GameObject>();
    public UnityEvent<GameObject> OnStrike = new UnityEvent<GameObject>();

    ATargetBehaviour _targetBehaviour;
    Entity _owner;
    float _timer = 0f;
    GameObject _markedTarget;
    // Kept apart from _markedTarget: a destroyed unit compares equal to null for Unity
    bool _isMarking = false;

    public GameObject markedTarget => _markedTarget;
    public bool isMarking => _isMarking;
    // Seconds left before the strike, 0 when nothing is marked
    public float remainingDelay => isMarking ? Mathf.Max(0f, data.delay - _timer) : 0f;
    // Seconds left before the next mark, 0 while a unit is marked
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

    // ICooldownSkill: the cooldown is the interval between a strike and the next mark
    public float cooldownDuration => interval;
    public float cooldownProgress => interval > 0f ? remainingInterval / interval : 0f;

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

    public void Tick(float deltaTime)
    {
        // The marked unit left the game before the strike (killed by something else): the strike is lost
        if (_isMarking && _markedTarget == null)
        {
            ClearMark();
            _timer = 0f;
        }

        _timer += deltaTime;
        if (!isMarking)
        {
            if (_timer >= interval)
            {
                Mark(FindTarget());
            }
        }
        else if (_timer >= data.delay)
        {
            Strike();
        }
    }

    public override void Reset()
    {
        ClearMark();
        _timer = 0f;
    }

    // The unit picked by the targeting of the skill, null when nobody is in range
    protected virtual GameObject FindTarget()
    {
        List<GameObject> targets = _targetBehaviour.GetTargets(gameObject, transform.position, data.range, _owner.GetTargetType());
        return targets.Count > 0 ? targets[0] : null;
    }

    void Mark(GameObject target)
    {
        // Nobody to mark: tries again on the next frame
        if (target == null)
        {
            return;
        }

        _markedTarget = target;
        _isMarking = true;
        _timer = 0f;
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

    void Strike()
    {
        GameObject target = _markedTarget;
        ClearMark();
        _timer = 0f;

        Entity entity = target != null ? target.GetComponent<Entity>() : null;
        if (entity == null || entity.health == null || entity.health.Value <= 0f || data.strikeConsumer == null)
        {
            return;
        }

        entity.health.AddResourceModifier(ResourceModifier.Create(data.strikeConsumer, gameObject, target, strikeMultiplier));
        OnStrike.Invoke(target);
    }

    void ClearMark()
    {
        if (_markedTarget != null && data.markBuffHandler != null)
        {
            BuffManager buffManager = _markedTarget.GetComponent<BuffManager>();
            // Only a mark still there: removing an expired one would leave a dead entry blocking the next mark
            if (buffManager != null && buffManager.GetActiveHandlers().Exists(handler => handler.buffHandlerFactory == data.markBuffHandler && handler.source == gameObject))
            {
                buffManager.RemoveHandler(data.markBuffHandler, gameObject, _markedTarget, true);
            }
        }
        _markedTarget = null;
        _isMarking = false;
    }
}
