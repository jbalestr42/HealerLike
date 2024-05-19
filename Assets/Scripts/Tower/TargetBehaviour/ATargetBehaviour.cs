using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TargetBehaviourType
{
    First = 0,
    Nearest = 1,
    Fastest = 2,
    LowestHealth = 3,
}

public abstract class ATargetBehaviour
{
    public static readonly int MaxTarget = 100;

    List<ATargetValidator> _targetValidators = new List<ATargetValidator>();
    public List<ATargetValidator> targetValidators { get { return _targetValidators; } set { _targetValidators = value; } }

    int _targetCount = 1;
    public int targetCount { get { return _targetCount; } set { _targetCount = value; } }

    protected List<GameObject> _targets = new List<GameObject>();

    public virtual List<GameObject> GetTargets(Vector3 position, float range, Entity.EntityType entityType)
    {
        _targets.Clear();

        foreach (GameObject target in EntityManager.instance.GetEntities(entityType))
        {
            if (Vector3.Distance(target.transform.position, position) <= range)
            {
                if (CanAddTarget(target))
                {
                    _targets.Add(target);
                }
            }
        }

        ApplyBehaviour(_targets, position, range);

        // Focus marked enemy first
        //if at some point we want multiple marked enemy
        //_targets.Sort((GameObject a, GameObject b) =>
        //{
        //    return MarkManager.instance.IsEnemyMarked(b).CompareTo(MarkManager.instance.IsEnemyMarked(a));
        //});

        // Move single marked enemy at first index
        GameObject marked = _targets.Find((GameObject a) => MarkManager.instance.IsEnemyMarked(a));
        if (marked)
        {
            _targets.Remove(marked);
            _targets.Insert(0, marked);
        }

        if (_targetCount > 0 && _targetCount < _targets.Count)
        {
            _targets.RemoveRange(_targetCount, _targets.Count - _targetCount);
        }
        return _targets;
    }

    public bool CanAddTarget(GameObject target)
    {
        foreach (ATargetValidator targetValidator in _targetValidators)
        {
            if (!targetValidator.IsValid(target))
            {
                return false;
            }
        }
        return true;
    }

    public bool IsValidTarget(GameObject target)
    {
        return target != null && _targets.Contains(target);
    }

    public abstract TargetBehaviourType targetType { get; }
    public abstract void ApplyBehaviour(List<GameObject> targets, Vector3 position, float range);

    public static ATargetBehaviour Create(TargetBehaviourType type)
    {
        ATargetBehaviour targetBehaviour = null;
        switch (type)
        {
            case TargetBehaviourType.First:
                targetBehaviour = new FirstTargetBehaviour();
                break;
            case TargetBehaviourType.Nearest:
                targetBehaviour = new NearestTargetBehaviour();
                break;
            case TargetBehaviourType.LowestHealth:
                targetBehaviour = new LowestHealthTargetBehaviour();
                break;
            default:
                Debug.LogError($"[ATargetBehaviour] Unkown target behaviour '{type}'");
                break;
        }
        return targetBehaviour;
    }
}