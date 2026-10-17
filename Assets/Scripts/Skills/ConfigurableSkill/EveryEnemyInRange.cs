using System.Collections.Generic;
using UnityEngine;

// Every enemy within the range of a unit, without limit nor taunt, the targets of the unit left untouched (e.g. a
// slam on the whole team)
public class EveryEnemyInRange
{
    readonly List<ATargetValidatorFactory> _targetValidators;
    ATargetBehaviour _targetBehaviour;

    // Only the enemies these validators accept (e.g. not the untargetable ones)
    public EveryEnemyInRange(List<ATargetValidatorFactory> targetValidators)
    {
        _targetValidators = targetValidators;
    }

    // A copy, the unit's own list kept intact
    public List<GameObject> Find(GameObject unit)
    {
        if (_targetBehaviour == null)
        {
            _targetBehaviour = ATargetBehaviour.Create(TargetBehaviourType.First);
            _targetBehaviour.targetCount = 0;
            if (_targetValidators != null)
            {
                foreach (ATargetValidatorFactory targetValidator in _targetValidators)
                {
                    _targetBehaviour.targetValidators.Add(targetValidator.GetTargetValidator());
                }
            }
        }
        float range = unit.GetComponent<AttributeManager>().GetOrAdd(AttributeType.Range).Value;
        return new List<GameObject>(_targetBehaviour.GetTargets(unit, unit.transform.position, range, unit.GetComponent<Entity>().GetTargetType()));
    }
}
