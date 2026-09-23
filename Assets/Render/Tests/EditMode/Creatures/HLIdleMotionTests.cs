using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public class HLIdleMotionTests
    {
        [Test]
        public void AbsoluteNoiseIsDeterministicAndBounded()
        {
            HLIdleDefinition settings = new HLIdleDefinition
            {
                swayDegrees = 2.5f,
                swayFrequency = 0.12f,
                breathAmount = 0.025f,
                breathFrequency = 0.25f,
                seed = 17
            };
            HLIdlePose first = HLIdleMotion.Evaluate(settings, 8f);
            for (int i = 0; i < 200; i++)
            {
                HLIdlePose pose = HLIdleMotion.Evaluate(settings, i * 0.17f);
                Assert.That(pose.bodyScale.x, Is.InRange(0.975f, 1.025f));
                Assert.LessOrEqual(Mathf.Abs(pose.bodyLift), 0.008f);
                Assert.LessOrEqual(Quaternion.Angle(Quaternion.identity, pose.sway), 3.54f);
            }

            Assert.AreEqual(first.sway, HLIdleMotion.Evaluate(settings, 8f).sway);
            Assert.AreNotEqual(first.sway, HLIdleMotion.Evaluate(settings, 9f).sway);
            settings.seed++;
            Assert.AreNotEqual(first.sway, HLIdleMotion.Evaluate(settings, 8f).sway);
        }
    }
}
