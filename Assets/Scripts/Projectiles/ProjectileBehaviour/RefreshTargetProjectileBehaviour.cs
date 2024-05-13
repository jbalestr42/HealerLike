using System;
using UnityEngine;

[Serializable]
public class RefreshTargetProjectileBehaviourData
{
}

public class RefreshTargetProjectileBehaviour : AProjectileBehaviour<RefreshTargetProjectileBehaviourData>
{
    bool _hasHit = false;

    public override void Init(GameObject source)
    {
        projectile.OnUpdate.AddListener(OnUpdate);
        projectile.OnHit.AddListener(OnHit);
    }

    void OnUpdate()
    {
		if (!_hasHit && !projectile.target && projectile.source)
        {
            ITargetProvider targetProvider = projectile.source.GetComponent<ITargetProvider>();
            var targets = targetProvider.GetTargets();
            projectile.SetTarget(targets.Count > 0 ? targets[0] : null);
        }
	}

    public void OnHit(OnHitData onHitData)
    {
        _hasHit = true;
    }
}