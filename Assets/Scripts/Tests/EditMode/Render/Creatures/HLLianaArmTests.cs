using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Creatures
{
    public class HLLianaArmTests
    {
        HLCreatureRecipe recipe;
        HLLianaArm arm;
        [SetUp] public void Setup() { recipe = HLCreatureValidatorTests.Recipe(); arm = new HLLianaArm(recipe.arms[0], null, null); arm.Tick(0, Vector3.zero, Quaternion.identity); }
        [TearDown] public void Cleanup() { arm.Dispose(); Object.DestroyImmediate(recipe); }
        void Lengths() { for (int i = 0; i < arm.SegmentCount; i++) Assert.That(Vector3.Distance(arm.Joint(i), arm.Joint(i + 1)), Is.EqualTo(.2f).Within(1e-5)); }
        [Test] public void ContactBypassesAnticipationAndReturnRestoresExactPose()
        {
            arm.Begin(1, HLGestureKind.Heal, Vector3.one); arm.Contact(1, Vector3.one); arm.Tick(.001f, Vector3.zero, Quaternion.identity);
            Assert.Less(Vector3.Distance(arm.Tip, Vector3.one), .001f); Lengths();
            for (int i = 0; i < 30; i++) { arm.Tick(.01f, Vector3.zero, Quaternion.identity); Lengths(); }
            Assert.AreEqual(HLGesturePhase.Rest, arm.Phase);
            for (int i = 0; i <= arm.SegmentCount; i++) Assert.AreEqual(recipe.arms[0].restJoints[i], arm.Joint(i));
        }
        [Test] public void StaleAndRepeatedEndsDoNotRetractNewGesture()
        {
            arm.Begin(1, HLGestureKind.Attack, Vector3.one); arm.Begin(2, HLGestureKind.Attack, Vector3.up);
            arm.End(1); arm.Cancel(1); Assert.AreEqual(HLGesturePhase.Extend, arm.Phase);
            arm.Tick(.02f, Vector3.zero, Quaternion.identity); arm.Cancel(2); arm.Cancel(2);
            Assert.AreEqual(HLGesturePhase.Retract, arm.Phase);
            arm.Tick(.1f, Vector3.zero, Quaternion.identity); Lengths(); arm.End(2);
            arm.Tick(.11f, Vector3.zero, Quaternion.identity); Assert.AreEqual(HLGesturePhase.Rest, arm.Phase);
        }
        [Test] public void ProjectileGoalIsFollowedWithoutInventedExtension()
        {
            arm.Begin(1, HLGestureKind.Attack, Vector3.one); arm.SetTipGoal(1, Vector3.up * 2);
            arm.Tick(.001f, Vector3.zero, Quaternion.identity);
            Assert.Less(Vector3.Distance(arm.Tip, Vector3.up * 2), .001f); Lengths();
        }
        [Test] public void DestructionAfterHitStillDisplaysContactBeforeReturn()
        {
            arm.Begin(1, HLGestureKind.Attack, Vector3.one); arm.Contact(1, Vector3.one); arm.End(1);
            arm.Tick(.016f, Vector3.zero, Quaternion.identity);
            Assert.AreEqual(HLGesturePhase.Contact, arm.Phase); Assert.Less(Vector3.Distance(arm.Tip, Vector3.one), .001f);
        }
    }
}
