using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetProvider : MonoBehaviour, ITargetProvider
{
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

        _range = GetComponent<AttributeManager>().Get(AttributeType.Range);
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
        int targetCount = _targetBehaviour.targetCount;
        _targetBehaviour = ATargetBehaviour.Create(targetBehaviourType);
        _targetBehaviour.targetCount = targetCount;
    }

    public List<GameObject> GetTargets()
    {
        return _targets;
    }
}
