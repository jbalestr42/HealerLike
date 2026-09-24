using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{

public class IdleMotionTests
{
    [Test]
    public void Evaluate_SameSettingsAndTime_IsDeterministicAndBounded()
    {
        IdleDefinition settings = new IdleDefinition
        {
            swayDegrees = 2.5f,
            swayFrequency = 0.12f,
            breathAmount = 0.025f,
            breathFrequency = 0.25f,
            seed = 17
        };
        IdlePose first = IdleMotion.Evaluate(settings, 8f);
        for (int i = 0; i < 200; i++)
        {
            IdlePose pose = IdleMotion.Evaluate(settings, i * 0.17f);
            Assert.That(pose.bodyScale.x, Is.InRange(0.975f, 1.025f));
            Assert.LessOrEqual(Quaternion.Angle(Quaternion.identity, pose.sway), 3.54f);
        }

        Assert.AreEqual(first.sway, IdleMotion.Evaluate(settings, 8f).sway);
        Assert.AreNotEqual(first.sway, IdleMotion.Evaluate(settings, 9f).sway);
        settings.seed++;
        Assert.AreNotEqual(first.sway, IdleMotion.Evaluate(settings, 8f).sway);
    }
}

}
