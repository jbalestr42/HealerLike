using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{

public class CreatureRigTests
{
    static readonly FootFrame ground = new FootFrame(Vector3.zero, Vector3.up, 1f);
    static readonly string healerPath = "Assets/Render/Creatures/Data/Healer.asset";

    GameObject _parent;
    Material _material;
    CreatureRecipe _recipe;
    CreatureRig _rig;

    [SetUp]
    public void SetUp()
    {
        _parent = new GameObject("TestRig");
        _material = new Material(RenderTestAssets.LoadLookMaterial());
        _recipe = RenderTestAssets.CreateRecipe();
        _recipe.idle = default;
        _rig = RenderTestAssets.CreateRig(_recipe, _parent.transform, _material);
    }

    [TearDown]
    public void TearDown()
    {
        _rig.Dispose();
        Object.DestroyImmediate(_parent);
        Object.DestroyImmediate(_material);
        Object.DestroyImmediate(_recipe);
    }

    void CreateDerivedRig(LookSide side)
    {
        _rig.Dispose();
        Object.DestroyImmediate(_recipe);
        _recipe = LookComposer.Compose(RenderTestAssets.CreateChannels(side, HeadKind.Bud),
            RenderTestAssets.LoadLookVocabulary());
        _rig = RenderTestAssets.CreateRig(_recipe, _parent.transform, _material);
    }

    static Color BaseColour(Renderer renderer)
    {
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        return block.GetColor("_BaseColor");
    }

    Color TipColour(int tip)
    {
        return BaseColour(_rig.partTransforms[tip].GetComponent<Renderer>());
    }

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
        _recipe.parts = new[] { original, new CreaturePart { id = "Tip", parent = 0,
            dimensions = Vector3.one * 0.2f, role = PartRole.Tip, colour = Color.green } };
        Assert.IsTrue(_rig.Recompose(_recipe, _material, _material, RenderTestAssets.LoadMeshes()));
        Transform anchor = _rig.budAnchors[0];
        int count = _parent.GetComponentsInChildren<Transform>(true).Length;
        _recipe.parts = new[] { original };
        Assert.IsTrue(_rig.Recompose(_recipe, _material, _material, RenderTestAssets.LoadMeshes()));
        Assert.IsTrue(anchor);
        Assert.IsFalse(anchor.gameObject.activeSelf);
        _recipe.parts = new[] { original, new CreaturePart { id = "Tip", parent = 0,
            dimensions = Vector3.one * 0.2f, role = PartRole.Tip, colour = Color.red } };
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
        _recipe = LookComposer.Compose(RenderTestAssets.CreateChannels(side, HeadKind.Arch),
            RenderTestAssets.LoadLookVocabulary());
        Material stone = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Stone.mat");
        Material shared = side == LookSide.Stone ? stone : _material;
        Material body = side == LookSide.Stone ? stone
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
                bool surface = part.role == PartRole.Body || part.role == PartRole.Head || part.role == PartRole.Stem;
                Assert.AreSame(surface ? body : shared, renderer.sharedMaterial, part.id);
                if (part.role == PartRole.Head)
                {
                    heads++;
                    if (headTransform == null) headTransform = renderer.transform;
                }
                if (part.role == PartRole.Stem) stems++;
                if (part.role == PartRole.Tip) tips++;
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
                if (renderer.transform == part) recipePart = true;
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
    public void Init_GlowingPart_BrightensBaseColourAndKeepsAlpha()
    {
        _rig.Dispose();
        _recipe.parts[0].glow = 2f;
        _recipe.parts[0].colour = new Color(0.2f, 0.4f, 0.1f, 0.7f);
        _rig = RenderTestAssets.CreateRig(_recipe, _parent.transform, _material);
        Renderer renderer = _rig.root.GetComponentInChildren<Renderer>();
        // 0.4 lit by glow 2 is 1.2, give or take the 8% colour jitter
        Assert.That(BaseColour(renderer).g, Is.InRange(1.104f, 1.296f));
        Assert.That(BaseColour(renderer).a, Is.EqualTo(0.7f).Within(0.0001f));
        _rig.SetReadout(null, 1f, 0f, 1f);
        _rig.Tick(0f, 0f, ground);
        Assert.Greater(BaseColour(renderer).g, 1);
        _rig.SetReadout(null, 1f, 0f, 0f);
        _rig.Tick(0f, 0f, ground);
        Assert.Less(BaseColour(renderer).g, 1);
    }

    [Test]
    public void SetReadout_ChargeAndWilt_SwellBudAndSuppressGlow()
    {
        _rig.Dispose();
        _recipe.parts[0].glow = 1f;
        _recipe.parts[0].role = PartRole.Tip;
        _rig = RenderTestAssets.CreateRig(_recipe, _parent.transform, _material);
        Renderer renderer = _rig.root.GetComponentInChildren<Renderer>();
        _rig.SetReadout(null, 1f, 0f, 0f);
        _rig.Tick(0f, 0f, ground);
        Vector3 scale = renderer.transform.localScale;
        Color colour = BaseColour(renderer);

        _rig.SetReadout(null, 1f, 1f, 0f);
        _rig.Tick(0f, 0f, ground);

        Assert.Greater(renderer.transform.localScale.x, scale.x * 1.2f);
        Assert.Greater(BaseColour(renderer).g, colour.g);
        _rig.SetReadout(null, 0f, 1f, 1f);
        _rig.Tick(0f, 0f, ground);
        Assert.Less(BaseColour(renderer).g, 0.5f);
    }

    [Test]
    public void Tick_FullCharge_PlantBodySphereDoesNotSwell()
    {
        CreateDerivedRig(LookSide.Plant);
        _rig.SetReadout(null, 1f, 0f, 0f);
        _rig.Tick(0f, 0f, ground);
        Vector3 rest = _rig.partTransforms[0].localScale;

        _rig.SetReadout(null, 1f, 1f, 0f);
        _rig.Tick(0f, 0f, ground);

        Assert.AreEqual(Primitive.Sphere, _recipe.parts[0].primitive);
        Assert.AreEqual(rest.x, _rig.partTransforms[0].localScale.x, 0.0001f);
    }

    [Test]
    public void Tick_ChargeThenWilt_TipKeepsItsHueAndBrightensThenDarkens()
    {
        CreateDerivedRig(LookSide.Plant);
        int tip = System.Array.FindIndex(_recipe.parts, part => part.role == PartRole.Tip);
        _rig.SetReadout(null, 1f, 0f, 0f);
        _rig.Tick(0f, 0f, ground);
        Color rest = TipColour(tip);

        _rig.SetReadout(null, 1f, 1f, 0f);
        _rig.Tick(0f, 0f, ground);
        Color charged = TipColour(tip);

        Color.RGBToHSV(rest, out float restHue, out _, out float restValue);
        Color.RGBToHSV(charged, out float chargedHue, out _, out float chargedValue);
        Assert.AreEqual(restHue, chargedHue, 0.01f);
        Assert.Greater(chargedValue, restValue);
        Assert.That(Vector4.Distance(_recipe.parts[tip].colour, rest), Is.LessThan(0.000001f)); // palette accent after colour-space round trip
        _rig.SetReadout(null, 0f, 0f, 0f);
        _rig.Tick(0f, 0f, ground);
        Color.RGBToHSV(TipColour(tip), out float wiltedHue, out _, out float wiltedValue);
        Assert.AreEqual(restHue, wiltedHue, 0.01f);
        Assert.Less(wiltedValue, restValue);
    }

    [Test]
    public void Tick_Crown_SpinsByRoleNotById()
    {
        _rig.Dispose();
        _recipe.parts[0].role = PartRole.Crown;
        _recipe.parts[0].id = "Ring";
        _rig = RenderTestAssets.CreateRig(_recipe, _parent.transform, _material);
        _rig.Tick(0f, 0f, ground);
        Quaternion before = _rig.partTransforms[0].parent.localRotation;

        _rig.Tick(5f, 0f, ground);

        float spin = CreatureRig.CrownSpinDegrees * 5f; // five seconds between the two ticks
        Quaternion after = _rig.partTransforms[0].parent.localRotation;
        Assert.That(Quaternion.Angle(before, after), Is.EqualTo(spin).Within(0.01f));
    }

    [Test]
    public void Tick_WarmRig_AllocatesNothing()
    {
        for (int i = 0; i < 20; i++)
        {
            _rig.Tick(i * 0.016f, 0.016f, ground);
        }

        long before = System.GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++)
        {
            _rig.Tick(i * 0.016f, 0.016f, ground);
        }

        Assert.AreEqual(0, System.GC.GetAllocatedBytesForCurrentThread() - before);
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
    public void Tick_TranslatedFootFrame_ReplantsRootsWithoutMovingParent()
    {
        _parent.transform.position = new Vector3(3f, 2f, 4f);

        _rig.Tick(2f, 0.016f, new FootFrame(new Vector3(3f, 0.5f, 4f), Vector3.up, 1f));

        Assert.AreEqual(new Vector3(3f, 2f, 4f), _parent.transform.position);
        Assert.AreEqual(new Vector3(3f, 0.5f, 4f), _rig.root.position);
        Assert.NotNull(_rig.root.Find("Root"));
        Assert.IsEmpty(_parent.GetComponentsInChildren<Collider>());
    }

    [Test]
    public void Init_ScaledParentPart_KeepsChildPivotUnscaled()
    {
        _rig.Dispose();
        CreaturePart child = new CreaturePart { id = "Child", parent = 0, localPosition = Vector3.up,
            dimensions = Vector3.one, colour = Color.green };
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
    public void SetReadout_AimAndHealth_DampAndRecoverWithoutMovingRoot()
    {
        _rig.SetReadout(Vector3.right * 4f, 0.2f, 0.8f, 0.8f);
        _rig.Tick(1f, 0.1f, ground);
        Transform sway = _rig.root.Find("Sway");
        Assert.Greater(sway.localEulerAngles.y, 0); // turning toward the target on the right, damped
        Assert.Less(sway.localEulerAngles.y, 90);
        Assert.Less(sway.localPosition.y, 0);
        _rig.SetReadout(Vector3.right * 4f, 1f, 0f, 0f);
        _rig.Tick(2f, 1f, ground);
        Assert.AreEqual(0, sway.localPosition.y);
        Assert.AreEqual(Vector3.zero, _rig.root.position);
    }

    [TestCase(LookSide.Plant)]
    [TestCase(LookSide.Stone)]
    public void PresentationFacing_ForkRetainsItsAuthoredWidthAcrossCamerasTargetsAndRecompose(LookSide side)
    {
        _rig.Dispose();
        Object.DestroyImmediate(_recipe);
        _recipe = LookComposer.Compose(RenderTestAssets.CreateChannels(side, HeadKind.Fork),
            RenderTestAssets.LoadLookVocabulary());
        _recipe.idle = default;
        _rig = RenderTestAssets.CreateRig(_recipe, _parent.transform, _material);
        _rig.Tick(0f, 0f, ground);
        float authoredWidth = ProjectedHeadWidth(Vector3.right);
        float oldPortraitWidth = ProjectedHeadWidth(Vector3.forward);
        Transform root = _rig.root;
        Transform firstPart = _rig.partTransforms[0];
        _rig.SetPresentationForward(Vector3.left);
        _rig.Tick(0f, 0f, ground);
        Assert.That(ProjectedHeadWidth(Vector3.forward), Is.GreaterThan(oldPortraitWidth * 1.4f),
            "The actual Fork mesh must expose its spread in the first portrait frame");
        foreach (float yaw in new[] { 90f, 0f, -55f, 170f })
        {
            Quaternion camera = Quaternion.Euler(52f, yaw, 0f);
            _rig.SetPresentationForward(-(camera * Vector3.forward));
            foreach (Vector3 target in new[] { Vector3.right, Vector3.forward, Vector3.back, Vector3.zero })
            {
                _rig.SetReadout(target * 4f, 1f, 0f, 0f);
                _rig.Tick(1f, 1f, ground);
                Assert.That(ProjectedHeadWidth(camera * Vector3.right), Is.GreaterThan(authoredWidth * 0.88f),
                    "The target reaction must preserve visible mesh width at camera yaw " + yaw);
                Assert.AreEqual(Vector3.zero, _rig.root.position);
            }
            Assert.IsTrue(_rig.Recompose(_recipe, _material, _material, RenderTestAssets.LoadMeshes()));
            _rig.Tick(1f, 0f, ground);
            Assert.AreSame(root, _rig.root);
            Assert.AreSame(firstPart, _rig.partTransforms[0]);
            Assert.That(ProjectedHeadWidth(camera * Vector3.right), Is.GreaterThan(authoredWidth * 0.88f));
            Assert.IsTrue(_rig.TryGetAnchors(out EffectAnchors anchors));
            Assert.That(Vector3.Distance(anchors.foot, _rig.root.position), Is.LessThan(0.00001f));
            Assert.That(anchors.headRadius, Is.GreaterThan(0f));
        }
    }

    // Measure actual generated vertices, excluding roots and body; transformed axis-aligned bounds would
    // overstate how wide an edge-on articulated head really appears.
    float ProjectedHeadWidth(Vector3 screenRight)
    {
        float minimum = float.PositiveInfinity;
        float maximum = float.NegativeInfinity;
        for (int i = 0; i < _recipe.parts.Length; i++)
        {
            if (_recipe.parts[i].role != PartRole.Head) continue;
            Transform part = _rig.partTransforms[i];
            foreach (Vector3 vertex in part.GetComponent<MeshFilter>().sharedMesh.vertices)
            {
                float x = Vector3.Dot(part.TransformPoint(vertex), screenRight);
                minimum = Mathf.Min(minimum, x);
                maximum = Mathf.Max(maximum, x);
            }
        }
        return maximum - minimum;
    }

    [Test]
    public void PresentationFacing_DegenerateViewAndTargetsKeepFinitePoseAndGroundedFeet()
    {
        _rig.SetPresentationForward(Vector3.left);
        _rig.SetReadout(Vector3.zero, 1f, 0f, 0f);
        _rig.Tick(0f, 0f, ground);
        Transform sway = _rig.root.Find("Sway");
        Quaternion valid = sway.rotation;
        Vector3[] feet = RootFeet();
        foreach (Vector3 invalid in new[] { Vector3.zero, Vector3.up, new Vector3(float.NaN, 0f, 0f),
            new Vector3(float.PositiveInfinity, 0f, 0f) })
        {
            _rig.SetPresentationForward(invalid);
            _rig.SetReadout(invalid, 1f, 0f, 0f);
            _rig.Tick(0f, 0.1f, ground);
            Assert.That(Quaternion.Angle(valid, sway.rotation), Is.LessThan(0.001f));
        }
        _rig.SetPresentationForward(Vector3.back);
        _rig.SetReadout(Vector3.right * 4f, 1f, 0f, 0f);
        _rig.Tick(0f, 1f, ground);
        Vector3[] turnedFeet = RootFeet();
        Assert.That(feet.Length, Is.GreaterThan(0));
        for (int i = 0; i < feet.Length; i++)
        {
            Assert.That(Vector3.Distance(feet[i], turnedFeet[i]), Is.LessThan(0.0001f));
        }
        Assert.AreEqual(Vector3.zero, _parent.transform.position);
        Assert.AreEqual(Quaternion.identity, _parent.transform.rotation);
    }

    Vector3[] RootFeet()
    {
        System.Collections.Generic.List<Vector3> feet = new System.Collections.Generic.List<Vector3>();
        int segment = 0;
        foreach (Transform child in _rig.root)
        {
            if (child.name != "Root") continue;
            if (++segment % _recipe.roots.segments == 0)
            {
                Vector3 top = _recipe.roots.segmentShape.isProcedural
                    ? ProceduralShapeMeshes.Anchor(_recipe.roots.segmentShape, ShapeAnchor.Top) : Vector3.up * 0.5f;
                feet.Add(child.TransformPoint(top));
            }
        }
        return feet.ToArray();
    }

    [Test]
    public void Hit_ThenHealthRestored_ShakeDecaysAndTintRecovers()
    {
        _rig.SetReadout(Vector3.forward, 0.1f, 0f, 0f);
        _rig.Hit();
        _rig.Tick(0f, 0.01f, ground);
        Transform sway = _rig.root.Find("Sway");
        Assert.Greater(Mathf.Abs(sway.localRotation.z), 0.001f);
        Renderer renderer = sway.GetComponentInChildren<Renderer>();
        Color hurt = BaseColour(renderer);
        _rig.SetReadout(Vector3.forward, 1f, 0f, 0f);
        _rig.Tick(1f, 1f, ground);
        Assert.AreNotEqual(hurt, BaseColour(renderer));
        Assert.That(Quaternion.Angle(sway.localRotation, Quaternion.identity), Is.LessThan(0.001f));
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
