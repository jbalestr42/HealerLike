using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    public class HLStoneProjectileImpactBridgeTests
    {
        static HLStoneEnemyVisual CreateVisual(GameObject target)
        {
            Transform pivot = new GameObject("BodyPivot").transform;
            pivot.SetParent(target.transform, false);
            Transform presentation = new GameObject("HLStonePresentation").transform;
            presentation.SetParent(pivot, false);
            HLStoneEnemyVisual visual = target.AddComponent<HLStoneEnemyVisual>();
            TestHelpers.SetPrivateField(visual, "_bodyPivot", pivot);
            TestHelpers.SetPrivateField(visual, "_presentation", presentation);
            return visual;
        }

        [Test]
        public void UsesCallbackTargetAfterProjectileTargetClearedAndUnsubscribes()
        {
            HLStoneEnemyVisual visual = null;
            GameObject target = new GameObject("HLTarget");
            GameObject projectileObject = new GameObject("HLProjectile");
            try
            {
                ResourceAttribute health = TestHelpers.CreateResourceAttribute(target, AttributeType.HealthMax, 100);
                visual = CreateVisual(target);
                visual.Init(health, 1, null);
                Projectile projectile = projectileObject.AddComponent<Projectile>();
                HLStoneProjectileImpactBridge bridge = projectileObject.AddComponent<HLStoneProjectileImpactBridge>();
                TestHelpers.InvokePrivate(bridge, "OnEnable");

                ResourceModifier modifier = new ResourceModifier();
                Assert.IsNull(projectile.target);
                projectile.OnHit.Invoke(new OnHitData { target = target, resourceModifier = modifier });
                Assert.AreEqual(1, visual.pendingImpactCount);

                bridge.enabled = false;
                TestHelpers.InvokePrivate(bridge, "OnDisable");
                projectile.OnHit.Invoke(new OnHitData { target = target, resourceModifier = new ResourceModifier() });
                Assert.AreEqual(1, visual.pendingImpactCount);
            }
            finally
            {
                if (visual != null)
                {
                    TestHelpers.InvokePrivate(visual, "OnDestroy");
                }
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(projectileObject);
            }
        }
    }
}
