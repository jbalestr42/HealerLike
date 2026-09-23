using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    public class HLStoneEffectsDeliveryTests
    {
        static HLStoneEffects CreateEffects()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Render/Stones/Prefabs/StoneEffects.prefab");
            return Object.Instantiate(prefab).GetComponent<HLStoneEffects>();
        }

        [Test]
        public void ContactBurstExpiresAndReusesPool()
        {
            HLStoneEffects effects = CreateEffects();
            GameObject go = effects.gameObject;
            try
            {
                effects.EmitThrownContact(Vector3.one, 91);
                int count = effects.liveCount;
                Assert.That(count, Is.InRange(8, 10));

                effects.Advance(0.2f);
                Assert.AreEqual(count - 5, effects.liveCount);

                effects.Advance(0.3f);
                Assert.AreEqual(0, effects.liveCount);

                effects.EmitThrownContact(Vector3.one, 91);
                Assert.AreEqual(count, effects.liveCount);
                Assert.AreEqual(count, go.transform.childCount);
            }
            finally
            {
                TestHelpers.InvokePrivate(effects, "OnDestroy");
                Object.DestroyImmediate(go);
            }
        }
    }
}
