using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Creatures
{
    public class HLIdleMotionTests
    {
        [Test] public void AbsoluteNoiseIsDeterministicAndBounded()
        {
            var settings = new HLIdleDefinition { swayDegrees = 2.5f, swayFrequency = .12f, breathAmount = .025f, breathFrequency = .25f, seed = 17 };
            var first = HLIdleMotion.Evaluate(settings, 8);
            for (int i = 0; i < 200; i++)
            {
                var p = HLIdleMotion.Evaluate(settings, i * .17f);
                Assert.That(p.bodyScale.x, Is.InRange(.975f, 1.025f)); Assert.LessOrEqual(Mathf.Abs(p.bodyLift), .008f);
                Assert.LessOrEqual(Quaternion.Angle(Quaternion.identity, p.sway), 3.54f);
            }
            Assert.AreEqual(first.sway, HLIdleMotion.Evaluate(settings, 8).sway);
            Assert.AreNotEqual(first.sway, HLIdleMotion.Evaluate(settings, 9).sway);
            settings.seed++; Assert.AreNotEqual(first.sway, HLIdleMotion.Evaluate(settings, 8).sway);
        }
    }
}
