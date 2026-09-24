using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{

public class CreatureRigTests
{
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

    [Test]
    public void Init_GlowingPart_BrightensBaseColourAndKeepsAlpha()
    {
        _rig.Dispose();
        _recipe.parts[0].glow = 2f;
        _recipe.parts[0].colour = new Color(0.2f, 0.4f, 0.1f, 0.7f);
        _rig = RenderTestAssets.CreateRig(_recipe, _parent.transform, _material);
        Renderer renderer = _rig.root.GetComponentInChildren<Renderer>();
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        // 0.4 lit by glow 2 is 1.2, give or take the 8% colour jitter
        Assert.That(block.GetColor("_BaseColor").g, Is.InRange(1.104f, 1.296f));
        Assert.That(block.GetColor("_BaseColor").a, Is.EqualTo(0.7f).Within(0.0001f));
        _rig.SetReadout(null, 1f, 0f, 1f);
        _rig.Tick(0f, 0f, new FootFrame(Vector3.zero, Vector3.up, 1f));
        renderer.GetPropertyBlock(block);
        Assert.Greater(block.GetColor("_BaseColor").g, 1);
        _rig.SetReadout(null, 1f, 0f, 0f);
        _rig.Tick(0f, 0f, new FootFrame(Vector3.zero, Vector3.up, 1f));
        renderer.GetPropertyBlock(block);
        Assert.Less(block.GetColor("_BaseColor").g, 1);
    }

    [Test]
    public void SetReadout_ChargeAndWilt_SwellBudAndSuppressGlow()
    {
        _rig.Dispose();
        _recipe.parts[0].glow = 1f;
        _recipe.parts[0].role = PartRole.Tip;
        _rig = RenderTestAssets.CreateRig(_recipe, _parent.transform, _material);
        Renderer renderer = _rig.root.GetComponentInChildren<Renderer>();
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        FootFrame frame = new FootFrame(Vector3.zero, Vector3.up, 1f);
        _rig.SetReadout(null, 1f, 0f, 0f);
        _rig.Tick(0f, 0f, frame);
        Vector3 scale = renderer.transform.localScale;
        renderer.GetPropertyBlock(block);
        Color colour = block.GetColor("_BaseColor");

        _rig.SetReadout(null, 1f, 1f, 0f);
        _rig.Tick(0f, 0f, frame);

        Assert.Greater(renderer.transform.localScale.x, scale.x * 1.2f);
        renderer.GetPropertyBlock(block);
        Assert.Greater(block.GetColor("_BaseColor").g, colour.g);
        _rig.SetReadout(null, 0f, 1f, 1f);
        _rig.Tick(0f, 0f, frame);
        renderer.GetPropertyBlock(block);
        Assert.Less(block.GetColor("_BaseColor").g, 0.5f);
    }

    CreatureRig CreateDerivedRig(LookSide side)
    {
        _rig.Dispose();
        Object.DestroyImmediate(_recipe);
        _recipe = LookComposer.Compose(RenderTestAssets.CreateChannels(side, HeadKind.Bud), RenderTestAssets.LoadLookVocabulary());
        _rig = RenderTestAssets.CreateRig(_recipe, _parent.transform, _material);
        return _rig;
    }

    Color TipColour(int tip)
    {
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        _rig.partTransforms[tip].GetComponent<Renderer>().GetPropertyBlock(block);
        return block.GetColor("_BaseColor");
    }

    [Test]
    public void Tick_FullCharge_PlantBodySphereDoesNotSwell()
    {
        CreateDerivedRig(LookSide.Plant);
        FootFrame frame = new FootFrame(Vector3.zero, Vector3.up, 1f);
        _rig.SetReadout(null, 1f, 0f, 0f);
        _rig.Tick(0f, 0f, frame);
        Vector3 rest = _rig.partTransforms[0].localScale;

        _rig.SetReadout(null, 1f, 1f, 0f);
        _rig.Tick(0f, 0f, frame);

        Assert.AreEqual(Primitive.Sphere, _recipe.parts[0].primitive);
        Assert.AreEqual(rest.x, _rig.partTransforms[0].localScale.x, 0.0001f);
    }

    [Test]
    public void Tick_Charge_TipKeepsItsHueAndBrightens()
    {
        CreateDerivedRig(LookSide.Plant);
        int tip = System.Array.FindIndex(_recipe.parts, part => part.role == PartRole.Tip);
        FootFrame frame = new FootFrame(Vector3.zero, Vector3.up, 1f);
        _rig.SetReadout(null, 1f, 0f, 0f);
        _rig.Tick(0f, 0f, frame);
        Color rest = TipColour(tip);

        _rig.SetReadout(null, 1f, 1f, 0f);
        _rig.Tick(0f, 0f, frame);
        Color charged = TipColour(tip);

        Color.RGBToHSV(rest, out float restHue, out _, out float restValue);
        Color.RGBToHSV(charged, out float chargedHue, out _, out float chargedValue);
        Assert.AreEqual(restHue, chargedHue, 0.01f);
        Assert.Greater(chargedValue, restValue);
        Assert.AreEqual(_recipe.parts[tip].colour, rest); // the palette accent at full value at rest
    }

    [Test]
    public void Tick_Wilt_TipDarkensWithoutMovingItsHue()
    {
        CreateDerivedRig(LookSide.Plant);
        int tip = System.Array.FindIndex(_recipe.parts, part => part.role == PartRole.Tip);
        FootFrame frame = new FootFrame(Vector3.zero, Vector3.up, 1f);

        _rig.SetReadout(null, 0f, 0f, 0f);
        _rig.Tick(0f, 0f, frame);

        Color.RGBToHSV(_recipe.parts[tip].colour, out float hue, out _, out float value);
        Color.RGBToHSV(TipColour(tip), out float wiltedHue, out _, out float wiltedValue);
        Assert.AreEqual(hue, wiltedHue, 0.01f);
        Assert.Less(wiltedValue, value);
    }

    [Test]
    public void Tick_Crown_SpinsByRoleNotById()
    {
        _rig.Dispose();
        _recipe.parts[0].role = PartRole.Crown;
        _recipe.parts[0].id = "Ring";
        _rig = RenderTestAssets.CreateRig(_recipe, _parent.transform, _material);
        FootFrame frame = new FootFrame(Vector3.zero, Vector3.up, 1f);
        _rig.Tick(0f, 0f, frame);
        Quaternion before = _rig.partTransforms[0].parent.localRotation;

        _rig.Tick(5f, 0f, frame);

        Assert.Greater(Quaternion.Angle(before, _rig.partTransforms[0].parent.localRotation), 1f);
    }

    [Test]
    public void Tick_WarmRig_AllocatesNothing()
    {
        FootFrame frame = new FootFrame(Vector3.zero, Vector3.up, 1f);
        for (int i = 0; i < 20; i++)
        {
            _rig.Tick(i * 0.016f, 0.016f, frame);
        }

        long before = System.GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++)
        {
            _rig.Tick(i * 0.016f, 0.016f, frame);
        }

        Assert.AreEqual(0, System.GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Test]
    public void Dispose_Twice_ReleasesTheRootOnce()
    {
        _rig.Dispose();
        _rig.Dispose();

        Assert.IsFalse(_rig.root);
    }

    [Test]
    public void Tick_TranslatedFootFrame_ReplantsRootsWithoutMovingParent()
    {
        _parent.transform.position = new Vector3(3f, 2f, 4f);
        Vector3 before = _parent.transform.position;

        _rig.Tick(2f, 0.016f, new FootFrame(new Vector3(3f, 0.5f, 4f), Vector3.up, 1f));

        Assert.AreEqual(before, _parent.transform.position);
        Assert.AreEqual(new Vector3(3f, 0.5f, 4f), _rig.root.position);
        Transform root = _rig.root.Find("Root");
        Assert.NotNull(root);
        Assert.IsEmpty(_parent.GetComponentsInChildren<Collider>());
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
            colour = Color.green
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
    public void Init_Recipe_OnePartTransformPerPart()
    {
        _rig.Dispose();
        CreatureRecipe data = AssetDatabase.LoadAssetAtPath<CreatureRecipe>("Assets/Render/Creatures/Data/Healer.asset");

        _rig = RenderTestAssets.CreateRig(data, _parent.transform, _material);

        Assert.AreEqual(data, _rig.recipe);
        Assert.AreEqual(data.parts.Length, _rig.partTransforms.Count);
        for (int i = 0; i < data.parts.Length; i++)
        {
            Assert.AreEqual(data.parts[i].id, _rig.partTransforms[i].parent.name);
            Assert.NotNull(_rig.partTransforms[i].GetComponent<MeshFilter>());
        }
    }

    [Test]
    public void SetReadout_AimAndHealth_DampAndRecoverWithoutMovingRoot()
    {
        _rig.SetReadout(Vector3.right * 4f, 0.2f, 0.8f, 0.8f);
        _rig.Tick(1f, 0.1f, new FootFrame(Vector3.zero, Vector3.up, 1f));
        Transform sway = _rig.root.Find("Sway");
        Assert.Greater(sway.localEulerAngles.y, 0); // turning toward the target on the right, damped
        Assert.Less(sway.localEulerAngles.y, 90);
        Assert.Less(sway.localPosition.y, 0);
        _rig.SetReadout(Vector3.right * 4f, 1f, 0f, 0f);
        _rig.Tick(2f, 1f, new FootFrame(Vector3.zero, Vector3.up, 1f));
        Assert.AreEqual(0, sway.localPosition.y);
        Assert.AreEqual(Vector3.zero, _rig.root.position);
    }

    [Test]
    public void Hit_ThenHealthRestored_ShakeDecaysAndTintRecovers()
    {
        FootFrame frame = new FootFrame(Vector3.zero, Vector3.up, 1f);
        _rig.SetReadout(Vector3.forward, 0.1f, 0f, 0f);
        _rig.Hit();
        _rig.Tick(0f, 0.01f, frame);
        Transform sway = _rig.root.Find("Sway");
        Assert.Greater(Mathf.Abs(sway.localRotation.z), 0.001f);
        Renderer renderer = sway.GetComponentInChildren<Renderer>();
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        Color hurt = block.GetColor("_BaseColor");
        _rig.SetReadout(Vector3.forward, 1f, 0f, 0f);
        _rig.Tick(1f, 1f, frame);
        renderer.GetPropertyBlock(block);
        Assert.AreNotEqual(hurt, block.GetColor("_BaseColor"));
        Assert.That(Quaternion.Angle(sway.localRotation, Quaternion.identity), Is.LessThan(0.001f));
    }

    [Test]
    public void Dispose_LiveRig_LeavesSharedMeshesAlive()
    {
        Mesh mesh = _rig.root.GetComponentInChildren<MeshFilter>().sharedMesh;

        _rig.Dispose();

        Assert.IsTrue(mesh);
        Assert.IsTrue(AssetDatabase.Contains(mesh));
    }

    [Test]
    public void Init_HealerRecipe_BuildsOnBakedMeshesAndSpinsItsCrown()
    {
        string healerPath = "Assets/Render/Creatures/Data/Healer.asset";
        CreatureRecipe healer = AssetDatabase.LoadAssetAtPath<CreatureRecipe>(healerPath);
        FootFrame frame = new FootFrame(Vector3.zero, Vector3.up, 1f);
        _rig.Dispose();
        _rig = RenderTestAssets.CreateRig(healer, _parent.transform, _material);
        _rig.Tick(1f, 0.016f, frame);
        Transform crown = _rig.root.Find("Sway/Stem/Crown");
        Quaternion before = crown.localRotation;

        _rig.Tick(11f, 0.016f, frame);

        Assert.IsTrue(CreatureValidator.TryValidate(healer, out string error), error);
        Assert.That(healer.roots.count, Is.InRange(8, 14));
        Assert.AreEqual(3, healer.roots.segments);
        Assert.AreEqual(Primitive.Cone, System.Array.Find(healer.parts, part => part.id == "Bulb").primitive);
        Assert.AreEqual(Primitive.Torus, System.Array.Find(healer.parts, part => part.id == "Crown").primitive);
        float spin = CreatureRig.CrownSpinDegrees * 10f; // ten seconds between the two ticks
        Assert.That(Quaternion.Angle(before, crown.localRotation), Is.EqualTo(spin).Within(0.01f));
        // Body parts share the baked meshes, only the arm chains are generated per rig
        foreach (MeshFilter filter in _parent.GetComponentsInChildren<MeshFilter>())
        {
            bool isChain = filter.name == "LianaArm";
            Assert.AreEqual(!isChain, AssetDatabase.Contains(filter.sharedMesh), filter.name);
        }
        Assert.IsEmpty(_parent.GetComponentsInChildren<Collider>());
    }
}

}
