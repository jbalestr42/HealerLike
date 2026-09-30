using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class MarkedStrikeSkillData : SkillDataBase
{
    // Time before each mark, counted from the previous strike (or from the start of the battle)
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
    // Optional, put on the marked unit until the strike (e.g. an icon over its health bar)
    [CreateDataButton]
    public ABuffHandlerFactory markBuffHandler;
}

// Telegraphed strike: marks one unit, then strikes it hard a few seconds later. The strike is lost if the
// marked unit dies or disappears before it lands.
public class MarkedStrikeSkill : ASkill<MarkedStrikeSkillData>
{
    public UnityEvent<GameObject> OnMarked = new UnityEvent<GameObject>();
    public UnityEvent<GameObject> OnStrike = new UnityEvent<GameObject>();

    ATargetBehaviour _targetBehaviour;
    Entity _owner;
    float _timer = 0f;
    GameObject _markedTarget;

    public GameObject markedTarget => _markedTarget;
    public bool isMarking => _markedTarget != null;
    // Seconds left before the strike, 0 when nothing is marked
    public float remainingDelay => isMarking ? Mathf.Max(0f, data.delay - _timer) : 0f;

    // The data is set right after AddComponent, so after Awake: the targeting is built once here
    void Start()
    {
        _owner = GetComponent<Entity>();
        _targetBehaviour = ATargetBehaviour.Create(data.targetBehaviourType);
        foreach (ATargetValidatorFactory targetValidator in data.targetValidators)
        {
            _targetBehaviour.targetValidators.Add(targetValidator.GetTargetValidator());
        }
    }

    public override void UpdateBehaviour(GameObject source)
    {
        Tick(Time.deltaTime);
    }

    public void Tick(float deltaTime)
    {
        _timer += deltaTime;
        if (!isMarking)
        {
            if (_timer >= data.interval)
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

        entity.health.AddResourceModifier(ResourceModifier.Create(data.strikeConsumer, gameObject, target));
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
    }
}
