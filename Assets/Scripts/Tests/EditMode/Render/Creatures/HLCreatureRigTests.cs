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
        [Test] public void NonuniformAncestorsAreRejected()
        {
            parent.transform.localScale = new Vector3(1, 2, 1);
            Assert.Throws<System.ArgumentException>(() => HLCreatureRig.Build(recipe, parent.transform, material));
        }
    }
}
