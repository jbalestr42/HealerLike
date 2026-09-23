using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    public class HLStoneEffectsDeliveryTests
    {
        [Test]
        public void ContactBurstExpiresAndReusesPool()
        {
            GameObject go = new GameObject("HLContactEffects");
            try
            {
                HLStoneEffects effects = go.AddComponent<HLStoneEffects>();
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
                TestHelpers.InvokePrivate(go.GetComponent<HLStoneEffects>(), "OnDestroy");
                Object.DestroyImmediate(go);
            }
        }
    }
}
