using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    public class HLStoneDeathBridgeTests
    {
        static HLStoneEffects CreateEffects()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Render/Stones/Prefabs/StoneEffects.prefab");
            return Object.Instantiate(prefab).GetComponent<HLStoneEffects>();
        }

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
        public void LivingRemovalDoesNothingAndLethalDepartureCollapsesSynchronouslyOnce()
        {
            HLStoneEnemyVisual visual = null;
            HLStoneEffects fx = CreateEffects();
            GameObject target = new GameObject("HLTarget");
            GameObject effectsObject = fx.gameObject;
            GameObject bridgeObject = new GameObject("HLBridge");
            try
            {
                ResourceAttribute health = TestHelpers.CreateResourceAttribute(target, AttributeType.HealthMax, 100);
                visual = CreateVisual(target);
                visual.Init(health, 1, fx);
                HLStoneDeathBridge bridge = bridgeObject.AddComponent<HLStoneDeathBridge>();
                bridge.Bind(null, fx);

                bridge.HandleDeparture(health, visual);
                Assert.AreEqual(0, fx.liveCount);

                TestHelpers.SetPrivateField(health, "_value", 0f);
                bridge.HandleDeparture(health, visual);
                Assert.AreEqual(17, fx.liveCount);

                bridge.HandleDeparture(health, visual);
                Assert.AreEqual(17, fx.liveCount);

                visual.Init(health, 1, fx);
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
                TestHelpers.InvokePrivate(fx, "OnDestroy");
                Object.DestroyImmediate(bridgeObject);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(effectsObject);
            }
        }
    }
}
