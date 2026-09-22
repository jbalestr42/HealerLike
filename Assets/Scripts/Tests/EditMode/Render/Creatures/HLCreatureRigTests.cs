using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Creatures
{
    public class HLCreatureRigTests
    {
        GameObject parent;
        Material material;
        HLCreatureRecipe recipe;
        HLCreatureRig rig;
        [SetUp] public void Setup()
        {
            parent = new GameObject("HLTestRig"); material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            recipe = HLCreatureValidatorTests.Recipe(); rig = HLCreatureRig.Build(recipe, parent.transform, material);
        }
        [TearDown] public void Cleanup() { rig.Dispose(); Object.DestroyImmediate(parent); Object.DestroyImmediate(material); Object.DestroyImmediate(recipe); HLPrimitiveMeshes.ReleaseAll(); }
        [Test] public void PoolSaturatesWithoutStealingLeasesAndDisposeIsIdempotent()
        {
            for (int i = 0; i < 8; i++) Assert.AreNotEqual(0, rig.Begin(HLGestureKind.Attack, Vector3.one));
            Assert.AreEqual(0, rig.Begin(HLGestureKind.Attack, Vector3.one)); Assert.AreEqual(8, rig.ActiveArmCount);
            rig.Contact(0, Vector3.one); rig.End(0); Assert.AreEqual(8, rig.ActiveArmCount);
            rig.CancelAll(); rig.Tick(.79f, .05f, new HLFootFrame(Vector3.zero, Vector3.up, 1)); rig.Tick(1, .21f, new HLFootFrame(Vector3.zero, Vector3.up, 1)); Assert.AreEqual(0, rig.ActiveArmCount);
            rig.Dispose(); rig.Dispose(); Assert.IsFalse(rig.Root);
        }
        [Test] public void FrameReplantsRootsAfterTranslationWithoutMovingParent()
        {
            parent.transform.position = new Vector3(3, 2, 4); var before = parent.transform.position;
            rig.Tick(2, .016f, new HLFootFrame(new Vector3(3, .5f, 4), Vector3.up, 1));
            Assert.AreEqual(before, parent.transform.position); Assert.AreEqual(new Vector3(3, .5f, 4), rig.Root.position);
            var root = rig.Root.Find("HLRoot"); Assert.NotNull(root);
            Assert.IsEmpty(parent.GetComponentsInChildren<Collider>());
        }
        [Test] public void ParentMeshDimensionsDoNotScaleChildPivots()
        {
            rig.Dispose(); recipe.parts = new[] { recipe.parts[0], new HLPart { id = "HLChild", parent = 0, localPosition = Vector3.up, dimensions = Vector3.one, colour = Color.green } };
            recipe.parts[0].dimensions = Vector3.one * 3;
            rig = HLCreatureRig.Build(recipe, parent.transform, material);
            var child = rig.Root.Find("HLSway/HLBody/HLChild"); Assert.AreEqual(Vector3.up, child.localPosition); Assert.AreEqual(Vector3.one, child.lossyScale);
        }
        [Test] public void SaturatedContactCannotReviveCancelledLease()
        {
            for (int i = 0; i < 8; i++) { int token = rig.Begin(HLGestureKind.Attack, Vector3.one); rig.Contact(token, Vector3.one); }
            rig.CancelAll();
            rig.Tick(.05f, .05f, new HLFootFrame(Vector3.zero, Vector3.up, 1));
            rig.Contact(0, Vector3.one);
            rig.Tick(.26f, .21f, new HLFootFrame(Vector3.zero, Vector3.up, 1));
            Assert.AreEqual(0, rig.ActiveArmCount);
        }
        [Test] public void NonuniformAncestorsAreRejected()
        {
            parent.transform.localScale = new Vector3(1, 2, 1);
            Assert.Throws<System.ArgumentException>(() => HLCreatureRig.Build(recipe, parent.transform, material));
        }
        [Test] public void DeliveryLeasesCapSwarmAndRejectStaleOrUnsupportedTokens()
        {
            Assert.IsFalse(rig.BeginDelivery(1, HLDeliveryStyle.Thrown, null, Vector3.one));
            for (int i = 1; i <= 4; i++) Assert.IsTrue(rig.BeginDelivery(i, HLDeliveryStyle.Swarm, null, Vector3.one));
            Assert.IsFalse(rig.BeginDelivery(5, HLDeliveryStyle.Swarm, null, Vector3.one));
            Assert.IsFalse(rig.BeginDelivery(1, HLDeliveryStyle.Direct, null, Vector3.one));
            rig.EndDelivery(1); rig.EndDelivery(1);
            Assert.IsTrue(rig.BeginDelivery(5, HLDeliveryStyle.Swarm, null, Vector3.one));
        }
        [Test] public void ChainCollectsContactsAndRetractsTogether()
        {
            Assert.IsTrue(rig.BeginDelivery(123, HLDeliveryStyle.ChainSync, null, Vector3.one));
            rig.ContactDelivery(123, Vector3.one, null); rig.ContactDelivery(123, Vector3.right * 2, null);
            Assert.AreEqual(2, rig.ActiveArmCount);
            rig.Tick(.1f, .1f, new HLFootFrame(Vector3.zero, Vector3.up, 1));
            Assert.AreEqual(2, rig.ActiveArmCount);
            rig.EndDelivery(123);
            rig.Tick(.4f, .3f, new HLFootFrame(Vector3.zero, Vector3.up, 1));
            Assert.AreEqual(0, rig.ActiveArmCount);
        }
        [Test] public void AimDampsAndHealthRecoversWithoutMovingFootFrame()
        {
            rig.SetReadout(Vector3.right * 4, .2f, .8f, .8f);
            rig.Tick(1, .1f, new HLFootFrame(Vector3.zero, Vector3.up, 1));
            Assert.Greater(rig.Aim.eulerAngles.y, 0); Assert.Less(rig.Aim.eulerAngles.y, 90);
            var sway = rig.Root.Find("HLSway"); Assert.Less(sway.localPosition.y, 0);
            rig.SetReadout(Vector3.right * 4, 1, 0, 0); rig.Tick(2, 1, new HLFootFrame(Vector3.zero, Vector3.up, 1));
            Assert.AreEqual(0, sway.localPosition.y); Assert.AreEqual(Vector3.zero, rig.Root.position);
        }
        [Test] public void HitShakeDecaysAndHealthTintRecovers()
        {
            var frame = new HLFootFrame(Vector3.zero, Vector3.up, 1);
            rig.SetReadout(Vector3.forward, .1f, 0, 0); rig.Hit(); rig.Tick(0, .01f, frame);
            var sway = rig.Root.Find("HLSway"); Assert.Greater(Mathf.Abs(sway.localRotation.z), .001f);
            var renderer = sway.GetComponentInChildren<Renderer>(); var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block); Color hurt = block.GetColor("_BaseColor");
            rig.SetReadout(Vector3.forward, 1, 0, 0); rig.Tick(1, 1, frame);
            renderer.GetPropertyBlock(block); Assert.AreNotEqual(hurt, block.GetColor("_BaseColor"));
            Assert.That(Quaternion.Angle(sway.localRotation, Quaternion.identity), Is.LessThan(.001f));
        }
    }
}
