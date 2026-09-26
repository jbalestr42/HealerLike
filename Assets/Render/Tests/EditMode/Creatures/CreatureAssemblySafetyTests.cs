using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{

public class CreatureAssemblySafetyTests : CreatureRigFixture
{
    [Test]
    public void Recompose_RejectedEditToTheSameRecipe_KeepsAcceptedPartsRootsAndArmJoints()
    {
        _rig.Tick(0f, 0f, ground);
        Transform part = _rig.partTransforms[0];
        Vector3 scale = part.localScale;
        CreaturePart accepted = _rig.parts[0];
        RootDefinition roots = _rig.roots;
        Vector3 joint = _rig.GetArm(0).restJoints[1];
        int revision = _rig.revision;
        _recipe.parts = new CreaturePart[0];
        _recipe.roots.footRadius = 100f;
        _recipe.arms[0].restJoints[1] = Vector3.one * 100f;
        Assert.IsFalse(_rig.Recompose(_recipe, _material, _material, RenderTestAssets.LoadMeshes()));
        Assert.DoesNotThrow(() => _rig.Tick(0f, 0f, ground));
        Assert.AreSame(_recipe, _rig.recipe);
        Assert.AreEqual(accepted, _rig.parts[0]);
        Assert.AreEqual(roots, _rig.roots);
        Assert.AreEqual(joint, _rig.GetArm(0).restJoints[1]);
        Assert.AreEqual(scale, part.localScale);
        Assert.AreEqual(revision, _rig.revision);
        Assert.IsTrue(_rig.TryGetAnchors(out _));
    }

    [Test]
    public void GetArm_EditToTheReturnedJoints_DoesNotMutateTheAcceptedPose()
    {
        ArmDefinition arm = _rig.GetArm(0);
        Vector3 accepted = arm.restJoints[1];
        arm.restJoints[1] = Vector3.one * 100f;
        Assert.AreEqual(accepted, _rig.GetArm(0).restJoints[1]);
        Assert.AreEqual(accepted, _recipe.arms[0].restJoints[1]);
    }

    [Test]
    public void Recompose_MineralThenGrowthThenMineral_ClearsObsoleteMaterialBlocks()
    {
        _recipe.parts[0].shape = ShapeProfile.Block();
        _recipe.parts[0].colour = Color.red;
        Assert.IsTrue(_rig.Recompose(_recipe, _material, _material, RenderTestAssets.LoadMeshes()));
        Renderer renderer = _rig.partTransforms[0].GetComponent<Renderer>();
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block, 0);
        Assert.Greater(block.GetColor("_BaseColor").r, 0f);
        _recipe.parts[0].shape = ShapeProfile.Bulb();
        _recipe.parts[0].colour = Color.green;
        _recipe.parts[0].role = PartRole.Tip;
        Assert.IsTrue(_rig.Recompose(_recipe, _material, _material, RenderTestAssets.LoadMeshes()));
        _rig.Tick(0f, 0f, ground);
        renderer.GetPropertyBlock(block, 0);
        Assert.IsTrue(block.isEmpty, "An old indexed tint must not override the renderer-wide growth tint.");
        renderer.GetPropertyBlock(block);
        Assert.AreEqual(Color.green, block.GetColor("_BaseColor"));
        Assert.AreEqual(PartPaint.TipOutlineWidth, block.GetFloat("_HLOutlineWidthMultiplier"));
        _recipe.parts[0].shape = ShapeProfile.Block();
        _recipe.parts[0].role = PartRole.Body;
        _recipe.parts[0].colour = Color.blue;
        Assert.IsTrue(_rig.Recompose(_recipe, _material, _material, RenderTestAssets.LoadMeshes()));
        _rig.Tick(0f, 0f, ground);
        renderer.GetPropertyBlock(block);
        Assert.IsTrue(block.isEmpty);
        renderer.GetPropertyBlock(block, 0);
        Assert.Greater(block.GetColor("_BaseColor").b, 0f);
        Assert.IsFalse(block.HasFloat("_HLOutlineWidthMultiplier"));
    }

    [Test]
    public void Init_MissingBakedMesh_ReleasesTheTemporaryRootAndLeavesSharedMeshesAlive()
    {
        PrimitiveMeshes meshes = Object.Instantiate(RenderTestAssets.LoadMeshes());
        CreatureRig rig = new CreatureRig();
        int before = _parent.transform.childCount;
        meshes.sphere = null;
        try
        {
            Assert.IsFalse(rig.Init(_recipe, _parent.transform, _material, meshes));
            Assert.IsFalse(rig.root);
            Assert.AreEqual(before, _parent.transform.childCount);
            Assert.IsTrue(RenderTestAssets.LoadMeshes().sphere);
        }
        finally
        {
            rig.Dispose();
            Object.DestroyImmediate(meshes);
        }
    }

    [Test]
    public void Snapshot_BeforeInitAndAfterDispose_HasNoPartsOrArms()
    {
        using (CreatureRig empty = new CreatureRig())
        {
            Assert.IsEmpty(empty.parts);
            Assert.AreEqual(0, empty.armCount);
            Assert.AreEqual(0, empty.roots.count);
        }

        _rig.Dispose();
        Assert.IsNull(_rig.root);
        Assert.IsEmpty(_rig.partTransforms);
        Assert.DoesNotThrow(() => _rig.Tick(0f, 0f, ground));
        Assert.IsEmpty(_rig.parts);
        Assert.AreEqual(0, _rig.armCount);
        Assert.AreEqual(0, _rig.roots.count);
    }
}
}
