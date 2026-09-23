using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    public class HLStoneDeathBridgeTests
    {
        [Test]
        public void LivingRemovalDoesNothingAndLethalDepartureCollapsesSynchronouslyOnce()
        {
            HLStoneEnemyVisual visual = null;
            HLStoneEffects fx = null;
            GameObject target = new GameObject("HLTarget");
            GameObject effectsObject = new GameObject("HLEffects");
            GameObject bridgeObject = new GameObject("HLBridge");
            try
            {
                ResourceAttribute health = TestHelpers.CreateResourceAttribute(target, AttributeType.HealthMax, 100);
                visual = target.AddComponent<HLStoneEnemyVisual>();
                fx = effectsObject.AddComponent<HLStoneEffects>();
                visual.Initialize(health, 1, fx);
                HLStoneDeathBridge bridge = bridgeObject.AddComponent<HLStoneDeathBridge>();
                bridge.Bind(null, fx);

                bridge.HandleDeparture(health, visual);
                Assert.AreEqual(0, fx.liveCount);

                TestHelpers.SetPrivateField(health, "_value", 0f);
                bridge.HandleDeparture(health, visual);
                Assert.AreEqual(17, fx.liveCount);

                bridge.HandleDeparture(health, visual);
                Assert.AreEqual(17, fx.liveCount);

                visual.Initialize(health, 1, fx);
                bridge.enabled = false;
                bridge.HandleDeparture(health, visual);
                Assert.AreEqual(17, fx.liveCount);
            }
            finally
            {
                if (visual != null)
                {
                    TestHelpers.InvokePrivate(visual, "OnDestroy");
                }
                if (fx != null)
                {
                    TestHelpers.InvokePrivate(fx, "OnDestroy");
                }
                Object.DestroyImmediate(bridgeObject);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(effectsObject);
            }
        }
    }
}
