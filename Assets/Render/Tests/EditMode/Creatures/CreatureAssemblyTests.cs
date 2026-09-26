using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine;
using UnityEditor;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{

public class CreatureAssemblyTests : CreatureRigFixture
{
    [Test]
    public void Recompose_RepeatedPaletteChanges_KeepsTransformsAndReadoutWithoutGrowingHierarchy()
    {
        Transform root = _rig.root;
        Transform part = _rig.partTransforms[0];
        int count = _parent.GetComponentsInChildren<Transform>(true).Length;
        _rig.SetReadout(Vector3.right, 0.35f, 0.8f, 0.8f);
        _rig.Tick(1f, 0.1f, ground);
        Quaternion pose = part.parent.parent.localRotation;
        for (int i = 0; i < 4; i++)
        {
            _recipe.parts[0].colour = Color.magenta;
            Assert.IsTrue(_rig.Recompose(_recipe, _material, _material, RenderTestAssets.LoadMeshes()));
            _rig.Tick(1f, 0f, ground);
            Assert.AreSame(root, _rig.root);
            Assert.AreSame(part, _rig.partTransforms[0]);
            Assert.AreEqual(pose, part.parent.parent.localRotation);
            Assert.AreEqual(count, _parent.GetComponentsInChildren<Transform>(true).Length);
        }

        Assert.Greater(BaseColour(part.GetComponent<Renderer>()).r, 0.1f);
    }

    [Test]
    public void Recompose_TopologyShrinksThenGrows_ReusesAnchorsAndRejectsInvalidRecipe()
    {
        CreaturePart original = _recipe.parts[0];
        _recipe.parts = new[]
        {
            original,
            new CreaturePart
            {
                id = "Tip",
                parent = 0,
                dimensions = Vector3.one * 0.2f,
                role = PartRole.Tip,
                colour = Color.green,
            },
        };
        Assert.IsTrue(_rig.Recompose(_recipe, _material, _material, RenderTestAssets.LoadMeshes()));
        Transform anchor = _rig.budAnchors[0];
        int count = _parent.GetComponentsInChildren<Transform>(true).Length;
        _recipe.parts = new[] { original };
        Assert.IsTrue(_rig.Recompose(_recipe, _material, _material, RenderTestAssets.LoadMeshes()));
        Assert.IsTrue(anchor);
        Assert.IsFalse(anchor.gameObject.activeSelf);
        _recipe.parts = new[]
        {
            original,
            new CreaturePart
            {
                id = "Tip",
                parent = 0,
                dimensions = Vector3.one * 0.2f,
                role = PartRole.Tip,
                colour = Color.red,
            },
        };
        Assert.IsTrue(_rig.Recompose(_recipe, _material, _material, RenderTestAssets.LoadMeshes()));
        Assert.AreSame(anchor, _rig.budAnchors[0]);
        Assert.AreEqual(count, _parent.GetComponentsInChildren<Transform>(true).Length);
        Assert.IsFalse(_rig.Recompose(null, _material, _material, RenderTestAssets.LoadMeshes()));
        Assert.IsTrue(anchor);
    }

    [TestCase(LookSide.Plant)]
    [TestCase(LookSide.Stone)]
    public void Recompose_ArchSurfacesUseTheCallerBodyMaterialWhileAccentsAndRootsKeepShared(LookSide side)
    {
        _rig.Dispose();
        Object.DestroyImmediate(_recipe);
        _recipe = LookComposer.Compose(
            RenderTestAssets.CreateChannels(side, HeadKind.Arch),
            RenderTestAssets.LoadLookVocabulary()
        );
        Material stone = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Stone.mat");
        Material shared = side == LookSide.Stone ? stone : _material;
        Material body =
            side == LookSide.Stone
                ? stone
                : AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Body.mat");
        _rig = new CreatureRig();
        Assert.IsTrue(_rig.Init(_recipe, _parent.transform, shared, body, RenderTestAssets.LoadMeshes(), 1f));
        Transform headTransform = null;
        int heads = 0;
        int stems = 0;
        int tips = 0;
        for (int pass = 0; pass < 2; pass++)
        {
            for (int i = 0; i < _recipe.parts.Length; i++)
            {
                CreaturePart part = _recipe.parts[i];
                Renderer renderer = _rig.partTransforms[i].GetComponent<Renderer>();
                bool surface =
                    part.role == PartRole.Body || part.role == PartRole.Head || part.role == PartRole.Stem;
                Assert.AreSame(surface ? body : shared, renderer.sharedMaterial, part.id);
                if (part.role == PartRole.Head)
                {
                    heads++;
                    if (headTransform == null)
                    {
                        headTransform = renderer.transform;
                    }
                }

                if (part.role == PartRole.Stem)
                {
                    stems++;
                }

                if (part.role == PartRole.Tip)
                {
                    tips++;
                }
            }

            // Recomposition must preserve the material contract on reused geometry as well as new parts.
            Assert.IsTrue(_rig.Recompose(_recipe, shared, body, RenderTestAssets.LoadMeshes()));
            Assert.IsTrue(headTransform);
        }

        Assert.That(heads, Is.GreaterThan(0), "The real Arch recipe must exercise head surfaces");
        Assert.That(tips, Is.GreaterThan(0), "Its semantic tips must retain the shared material");
        if (side == LookSide.Plant)
        {
            Assert.That(stems, Is.GreaterThan(0), "The Arch stalk must exercise the same surface look as its head");
        }

        int rootParts = 0;
        foreach (Renderer renderer in _rig.root.GetComponentsInChildren<Renderer>())
        {
            bool recipePart = false;
            foreach (Transform part in _rig.partTransforms)
            {
                if (renderer.transform == part)
                {
                    recipePart = true;
                }
            }

            if (!recipePart)
            {
                rootParts++;
                Assert.AreSame(shared, renderer.sharedMaterial, "Root material must stay separate");
            }
        }

        if (side == LookSide.Plant)
        {
            Assert.That(rootParts, Is.GreaterThan(0), "The plant must exercise the separate root material");
        }
        else
        {
            Assert.That(rootParts, Is.Zero, "Mineral supports are recipe parts, not plant roots");
        }
    }

    [Test]
    public void Dispose_Twice_ReleasesTheRootOnceAndLeavesSharedMeshes()
    {
        Mesh mesh = _rig.root.GetComponentInChildren<MeshFilter>().sharedMesh;
        _rig.Dispose();
        _rig.Dispose();
        Assert.IsFalse(_rig.root);
        Assert.IsTrue(mesh);
        Assert.IsTrue(AssetDatabase.Contains(mesh));
    }

    [Test]
    public void Init_ScaledParentPart_KeepsChildPivotUnscaled()
    {
        _rig.Dispose();
        CreaturePart child = new CreaturePart
        {
            id = "Child",
            parent = 0,
            localPosition = Vector3.up,
            dimensions = Vector3.one,
            colour = Color.green,
        };
        _recipe.parts = new CreaturePart[] { _recipe.parts[0], child };
        _recipe.parts[0].dimensions = Vector3.one * 3f;
        _rig = RenderTestAssets.CreateRig(_recipe, _parent.transform, _material);
        Transform pivot = _rig.root.Find("Sway/Body/Child");
        Assert.AreEqual(Vector3.up, pivot.localPosition);
        Assert.AreEqual(Vector3.one, pivot.lossyScale);
    }

    [Test]
    public void Init_NonuniformAncestors_LogsAndLeavesViewEmpty()
    {
        _parent.transform.localScale = new Vector3(1f, 2f, 1f);
        CreatureRig rig = new CreatureRig();
        LogAssert.Expect(LogType.Error, "[CreatureRig] Creature rig ancestors must have positive uniform scale.");
        bool isInitialized = rig.Init(_recipe, _parent.transform, _material, RenderTestAssets.LoadMeshes());
        Assert.IsFalse(isInitialized);
        Assert.IsNull(rig.root);
    }

    [Test]
    public void Init_StonePart_BuildsOnItsSeededVariant()
    {
        _rig.Dispose();
        _recipe.parts[0].primitive = Primitive.Stone;
        _recipe.parts[0].variant = 3;
        _rig = RenderTestAssets.CreateRig(_recipe, _parent.transform, _material);
        Mesh mesh = _rig.partTransforms[0].GetComponent<MeshFilter>().sharedMesh;
        Mesh[] variants = RenderTestAssets.LoadMeshes().stoneVariants.meshes;
        Assert.AreEqual(variants[3 % variants.Length], mesh);
    }

    [Test]
    public void Init_HealerRecipe_OnePartTransformPerPartOnBakedMeshes()
    {
        CreatureRecipe healer = AssetDatabase.LoadAssetAtPath<CreatureRecipe>(healerPath);
        _rig.Dispose();
        _rig = RenderTestAssets.CreateRig(healer, _parent.transform, _material);
        Assert.AreEqual(healer, _rig.recipe);
        Assert.AreEqual(healer.parts.Length, _rig.partTransforms.Count);
        for (int i = 0; i < healer.parts.Length; i++)
        {
            Assert.AreEqual(healer.parts[i].id, _rig.partTransforms[i].parent.name);
        }

        // Body parts share the baked meshes, only the arm chains are generated per rig
        foreach (MeshFilter filter in _parent.GetComponentsInChildren<MeshFilter>())
        {
            Assert.AreEqual(filter.name != "LianaArm", AssetDatabase.Contains(filter.sharedMesh), filter.name);
        }

        Assert.IsEmpty(_parent.GetComponentsInChildren<Collider>());
    }
}
}
