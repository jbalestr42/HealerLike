using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetProvider : MonoBehaviour, ITargetProvider
{
    // Tag of the entities the attacks target first

    ATargetBehaviour _targetBehaviour;
    List<GameObject> _targets;
    Attribute _range;
    Entity _owner;

    public bool isEnabled { get; set; }
    public int targetCount { get { return _targetBehaviour.targetCount; } set { _targetBehaviour.targetCount = value; } }
    public TargetBehaviourType targetBehaviourType { get => _targetBehaviour.targetType; set => SetTargetBehaviour(value); }

    public void Init(TargetBehaviourType targetBehaviourType, List<ATargetValidatorFactory> targetValidators)
    {
        _targetBehaviour = ATargetBehaviour.Create(targetBehaviourType);
        foreach (ATargetValidatorFactory targetValidator in targetValidators)
        {
            _targetBehaviour.targetValidators.Add(targetValidator.GetTargetValidator());
        }

        // Only the attacks of the entity are taunted, not its area of effects, bounces or heals
        _targetBehaviour.tauntTag = DataManager.instance.GetTagWithName(TagNames.Taunt);

        _range = GetComponent<AttributeManager>().GetOrAdd(AttributeType.Range);
        _owner = GetComponent<Entity>();
    }

    void Update()
    {
        if (isEnabled)
        {
            _targets = _targetBehaviour.GetTargets(gameObject, transform.position, _range.Value, _owner.GetTargetType());
        }
    }

    public void Reset()
    {
        _targets.Clear();
    }

    public void SetTargetBehaviour(TargetBehaviourType targetBehaviourType)
    {
        ATargetBehaviour previousBehaviour = _targetBehaviour;
        _targetBehaviour = ATargetBehaviour.Create(targetBehaviourType);
        _targetBehaviour.targetCount = previousBehaviour.targetCount;
        // The validators come from the entity data, whatever the way targets are picked
        _targetBehaviour.targetValidators = previousBehaviour.targetValidators;
        _targetBehaviour.tauntTag = previousBehaviour.tauntTag;
    }

    public List<GameObject> GetTargets()
    {
        return _targets;
    }

    // The targets of the battle, or the ones the entity would aim at from where it stands when it doesn't
    // fight (placement): a copy, the entity doesn't attack them
    public List<GameObject> GetPreviewTargets()
    {
        if (isEnabled)
        {
            return _targets != null ? new List<GameObject>(_targets) : new List<GameObject>();
        }
        return new List<GameObject>(_targetBehaviour.GetTargets(gameObject, transform.position, _range.Value, _owner.GetTargetType()));
    }
}
