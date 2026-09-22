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
        [Test] public void AuthoredArmUsesOneMeshOnlyDuringGesturesAndDisposesIt()
        {
            var authored = UnityEditor.AssetDatabase.LoadAssetAtPath<HLCreatureRecipe>("Assets/Render/Creatures/Data/HLHealer.asset");
            var parent = new GameObject("HLArmFixture");
            var material = new Material(Shader.Find("HL/Look/Primitive"));
            var rendered = new HLLianaArm(authored.arms[0], parent.transform, material);
            Mesh mesh = null;
            try
            {
                var renderers = parent.GetComponentsInChildren<Renderer>(true);
                Assert.AreEqual(1, renderers.Length); Assert.IsFalse(renderers[0].enabled);
                rendered.Tick(0, Vector3.zero, Quaternion.identity);
                Assert.AreEqual(0, rendered.MeshRevision);
                rendered.Begin(1, HLGestureKind.Attack, Vector3.one);
                rendered.SetTipGoal(1, Vector3.one);
                rendered.Tick(.016f, Vector3.zero, Quaternion.identity);
                Assert.IsTrue(renderers[0].enabled);
                mesh = parent.GetComponentInChildren<MeshFilter>().sharedMesh;
                Assert.Greater(mesh.vertexCount, 0);
                foreach (var vertex in mesh.vertices) Assert.IsTrue(HLChainSolver.Finite(vertex));
                Assert.That(Vector3.Distance(rendered.Tip, Vector3.one), Is.LessThan(.001f));
                rendered.End(1); rendered.Tick(1, Vector3.zero, Quaternion.identity);
                Assert.IsFalse(renderers[0].enabled);
                int revision = rendered.MeshRevision;
                var vertices = mesh.vertices;
                rendered.SetVisible(true); rendered.Tick(1, Vector3.one, Quaternion.identity);
                Assert.IsFalse(renderers[0].enabled); Assert.AreEqual(revision, rendered.MeshRevision);
                CollectionAssert.AreEqual(vertices, mesh.vertices);
            }
            finally { rendered.Dispose(); Object.DestroyImmediate(parent); Object.DestroyImmediate(material); }
            Assert.IsFalse(mesh);
        }
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
        [TestCase(HLDeliveryStyle.Arc)] [TestCase(HLDeliveryStyle.Rigid)] [TestCase(HLDeliveryStyle.Bounce)]
        public void DeliveryProfilesFollowLiveEndpoint(HLDeliveryStyle style)
        {
            arm.Style = style; arm.DeliveryProfile = true;
            arm.Begin(1, HLGestureKind.Attack, Vector3.right * 2); arm.SetTipGoal(1, Vector3.right * 2);
            arm.Tick(.016f, Vector3.zero, Quaternion.identity);
            Assert.That(Vector3.Distance(arm.Tip, Vector3.right * 2), Is.LessThan(.001f));
            if (style == HLDeliveryStyle.Arc) Assert.Greater(arm.Joint(arm.SegmentCount / 2).y, .1f);
            else Assert.AreEqual(0, arm.Joint(arm.SegmentCount / 2).y);
            arm.Contact(1, Vector3.right * 2); arm.SetTipGoal(1, Vector3.forward);
            arm.Tick(.016f, Vector3.zero, Quaternion.identity);
            Assert.That(Vector3.Distance(arm.Tip, Vector3.forward), Is.LessThan(.001f));
        }
    }
}
