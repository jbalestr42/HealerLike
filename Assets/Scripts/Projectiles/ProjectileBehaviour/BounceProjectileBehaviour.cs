using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BounceProjectileBehaviourData
{
    public float rangeMultiplier = 0.5f;
    public float decreaseMultiplierPerBounce = 0.5f;
    public int bounce = 1;
}

public class BounceProjectileBehaviour : AProjectileBehaviour<BounceProjectileBehaviourData>, IStackableBuff
{
    [SerializeField] int _bounceCount = 0;
    float _multiplier = 1f;
    int _stacks = 1;

    ATargetBehaviour _targetBehaviour;

    float _range = 1f;
    public Vector3 position => transform.position;

    List<GameObject> _hitEnemies = new List<GameObject>();

    public override void Init(GameObject source)
    {
        _targetBehaviour = ATargetBehaviour.Create(TargetBehaviourType.Nearest);
        _targetBehaviour.targetCount = ATargetBehaviour.MaxTarget;

        _range = source.GetComponent<AttributeManager>().Get(AttributeType.Range).Value * data.rangeMultiplier;
        _bounceCount = data.bounce * _stacks;

        projectile.OnHit.AddListener(OnHit);
    }

    public bool IsValidTarget(GameObject target)
    {
        return _hitEnemies.Contains(target);
    }

    public void OnHit(OnHitData onHitData)
    {
        if (!IsValidTarget(onHitData.target))
        {
            _hitEnemies.Add(onHitData.target);
            _bounceCount--;
            onHitData.resourceModifier.multiplier *= _multiplier;
            _multiplier *= data.decreaseMultiplierPerBounce;

            projectile.SetTarget(_bounceCount >= 0 ? GetNextTarget(onHitData.target.transform.position) : null);
        }
    }

    GameObject GetNextTarget(Vector3 position)
    {
        List<GameObject> targets = _targetBehaviour.GetTargets(position, _range, projectile.source.GetComponent<Entity>().GetTargetType());
        foreach (GameObject nextTarget in targets)
        {
            if (!_hitEnemies.Contains(nextTarget))
            {
                return nextTarget;
            }
        }

        return null;
    }

    public void Stack(GameObject source, GameObject target)
    {
        _stacks++;
    }

    public void Unstack(GameObject source, GameObject target)
    {
        _stacks--;
    }
}