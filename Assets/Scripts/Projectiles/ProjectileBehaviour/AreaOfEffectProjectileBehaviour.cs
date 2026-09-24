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

            GameObject areaOfEffectGo = EntityManager.instance.SpawnProjectile(data.areaOfEffectPrefab.gameObject, onHitData.target.transform.position, Quaternion.identity);
            AreaOfEffect areaOfEffect = areaOfEffectGo.GetComponent<AreaOfEffect>();
            areaOfEffect.source = onHitData.source;
            areaOfEffect.target = onHitData.target;
            areaOfEffect.radius = data.radius;
        }
	}
}