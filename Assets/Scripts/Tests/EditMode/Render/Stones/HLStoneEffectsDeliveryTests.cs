using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Stones
{
    public class HLStoneEffectsDeliveryTests
    {
        [Test] public void ContactBurstExpiresAndReusesPool()
        {
            var go=new GameObject("HLContactEffects");
            try
            {
                var effects=go.AddComponent<HLStoneEffects>(); effects.EmitThrownContact(Vector3.one,91);
                int count=effects.LiveCount; Assert.That(count,Is.InRange(8,10));
                effects.Advance(.2f); Assert.AreEqual(count-5,effects.LiveCount);
                effects.Advance(.3f); Assert.AreEqual(0,effects.LiveCount);
                effects.EmitThrownContact(Vector3.one,91); Assert.AreEqual(count,effects.LiveCount);
                Assert.AreEqual(count,go.transform.childCount);
            }
            finally { TestHelpers.InvokePrivate(go.GetComponent<HLStoneEffects>(),"OnDestroy"); Object.DestroyImmediate(go); }
        }
    }
}
