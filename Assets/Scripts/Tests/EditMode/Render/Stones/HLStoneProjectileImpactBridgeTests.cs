using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Stones
{
    public class HLStoneProjectileImpactBridgeTests
    {
        [Test] public void UsesCallbackTargetAfterProjectileTargetClearedAndUnsubscribes()
        {
            HLStoneEnemyVisual visual=null;
            var target=new GameObject("HLTarget"); var projectileObject=new GameObject("HLProjectile");
            try {
                var health=TestHelpers.CreateResourceAttribute(target,AttributeType.HealthMax,100);
                visual=target.AddComponent<HLStoneEnemyVisual>(); visual.Initialize(health,1,null);
                var projectile=projectileObject.AddComponent<Projectile>(); var bridge=projectileObject.AddComponent<HLStoneProjectileImpactBridge>();
                TestHelpers.InvokePrivate(bridge,"OnEnable");
                var modifier=new ResourceModifier(); Assert.IsNull(projectile.target);
                projectile.OnHit.Invoke(new OnHitData{target=target,resourceModifier=modifier}); Assert.AreEqual(1,visual.PendingImpactCount);
                bridge.enabled=false; TestHelpers.InvokePrivate(bridge,"OnDisable"); projectile.OnHit.Invoke(new OnHitData{target=target,resourceModifier=new ResourceModifier()}); Assert.AreEqual(1,visual.PendingImpactCount);
            } finally {if(visual!=null)TestHelpers.InvokePrivate(visual,"OnDestroy");Object.DestroyImmediate(target);Object.DestroyImmediate(projectileObject);}
        }
    }
}
