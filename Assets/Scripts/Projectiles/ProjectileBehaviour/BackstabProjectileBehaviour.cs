using System;
using UnityEngine;

[Serializable]
public class BackstabProjectileBehaviourData
{
    public float multiplier = 1.2f;
    public float angleThreshold = 30f;
}

public class BackstabProjectileBehaviour : AProjectileBehaviour<BackstabProjectileBehaviourData>
{
    public override void Init(GameObject source)
    {
        projectile.OnHit.AddListener(OnHit);
    }

    void OnHit(OnHitData onHitData)
    {
        Vector3 direction = (onHitData.target.transform.position - onHitData.source.transform.position).normalized;
        float angle = Vector3.Angle(direction, onHitData.target.transform.forward);
        float dot = Vector3.Dot(direction, onHitData.target.transform.forward);

        if (angle < data.angleThreshold)
        {
            onHitData.resourceModifier.multiplier *= data.multiplier;
        }
        //Debug.Log("[BackstabProjectileBehaviour] " + angle + " | " + dot + " | " + (angle < data.angleThreshold));
    }
}