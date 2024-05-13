using System;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class AreaOfEffectProjectileBehaviourData
{
    [AssetsOnly]
    public AreaOfEffect areaOfEffectPrefab;
    public float radius = 1f;
}

public class AreaOfEffectProjectileBehaviour : AProjectileBehaviour<AreaOfEffectProjectileBehaviourData>
{
    bool _isDone = false;
    public override void Init(GameObject source)
    {
        projectile.OnHit.AddListener(OnHit);
    }

    void OnHit(OnHitData onHitData)
    {
        // Used to avoid infinite OnHit chain
        if (!_isDone)
        {
            _isDone = true;

            AreaOfEffect areaOfEffect = Instantiate(data.areaOfEffectPrefab, onHitData.target.transform.position, Quaternion.identity);
            areaOfEffect.source = onHitData.source;
            areaOfEffect.target = onHitData.target;
            areaOfEffect.radius = data.radius;
        }
	}
}