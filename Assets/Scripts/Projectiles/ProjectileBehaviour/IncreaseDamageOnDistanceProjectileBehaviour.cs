using System;
using UnityEngine;

[Serializable]
public class IncreaseDamageOnDistanceProjectileBehaviourData
{
    public float minDistance = 2f;
    public float maxDistance = 3f;
    public float minMultiplier = 0.5f;
    public float maxMultiplier = 2f;
}

public class IncreaseDamageOnDistanceProjectileBehaviour : AProjectileBehaviour<IncreaseDamageOnDistanceProjectileBehaviourData>, IStackableBuff
{
    Vector3 _prevPos;
    float _distance = 0f;
    int _stacks = 1;

    public override void Init(GameObject source)
    {
        projectile.OnUpdate.AddListener(OnUpdate);
        projectile.OnHit.AddListener(OnHit);

        _prevPos = transform.position;
    }

    public void OnUpdate()
    {
        Vector3 pos = transform.position;
        _distance += Vector3.Distance(pos, _prevPos);
        _prevPos = pos;
    }

    void OnHit(OnHitData onHitData)
    {
        Debug.Log("[DEBUG] " + _distance + " | " + Math.RemapClamped(_distance, data.minDistance, data.maxDistance, data.minMultiplier, data.maxMultiplier));
        onHitData.resourceModifier.multiplier *= Math.RemapClamped(_distance, data.minDistance, data.maxDistance, data.minMultiplier, data.maxMultiplier) * _stacks;
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