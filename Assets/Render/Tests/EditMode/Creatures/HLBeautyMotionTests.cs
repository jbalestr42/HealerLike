using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public class HLBeautyMotionTests
    {
        [Test]
        public void VariationIsStableBoundedAndPreservesAlphaAndRandomState()
        {
            Color original = Color.HSVToRGB(0.3f, 0.7f, 0.6f);
            original.a = 0.4f;
            Random.State random = Random.state;
            Color.RGBToHSV(original, out float h, out float s, out float v);
            for (int seed = -200; seed < 200; seed++)
            {
                Color varied = HLBeautyMotion.Vary(original, seed);
                Assert.AreEqual(varied, HLBeautyMotion.Vary(original, seed));
                Color.RGBToHSV(varied, out float vh, out float vs, out float vv);
                Assert.LessOrEqual(Mathf.Abs(Mathf.DeltaAngle(h * 360f, vh * 360f)), 6.001f);
                Assert.That(vv / v, Is.InRange(0.91999f, 1.08001f));
                Assert.That(vs, Is.EqualTo(s).Within(0.00001f));
                Assert.AreEqual(original.a, varied.a);
            }

            Assert.AreEqual(random, Random.state);
            Assert.AreNotEqual(HLBeautyMotion.Vary(original, 1), HLBeautyMotion.Vary(original, 2));
        }
    }
}
