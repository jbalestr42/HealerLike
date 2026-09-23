using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using HealerLike.Render.Deliveries;
using HealerLike.Render.Grammar;
using HealerLike.Render.Stones;

namespace HealerLike.Render.Creatures
{

public class CreatureRigTests
{
    GameObject _parent;
    Material _material;
    CreatureRecipe _recipe;
    CreatureRig _rig;

    public static CreatureRig CreateRig(CreatureRecipe recipe, Transform parent, Material material)
    {
        CreatureRig rig = new CreatureRig();
        rig.Init(recipe, parent, material, PrimitiveMeshesTests.Meshes());
        return rig;
    }

    [SetUp]
    public void SetUp()
    {
        _parent = new GameObject("TestRig");
        _material = new Material(AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader"));
        _recipe = CreatureValidatorTests.Recipe();
        _recipe.idle = default;
        _rig = CreateRig(_recipe, _parent.transform, _material);
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
    public void ContactDelivery_ShortDirectReach_DoesNotCoilUnusedLength()
    {
        _rig.Dispose();
        CreatureRecipe data = AssetDatabase.LoadAssetAtPath<CreatureRecipe>("Assets/Render/Creatures/Data/SpiralFern.asset");
        _rig = CreateRig(data, _parent.transform, _material);
        _rig.Tick(0f, 0f, new FootFrame(Vector3.zero, Vector3.up, 1f));
        Vector3 target = new Vector3(2f, 1.3f, 0f);

        Assert.IsTrue(_rig.BeginDelivery(500, DeliveryStyle.Direct, null, target));
        _rig.ContactDelivery(500, target, null);
        _rig.Tick(0.02f, 0.02f, new FootFrame(Vector3.zero, Vector3.up, 1f));

        MeshFilter arm = _rig.root.Find("LianaArm").GetComponent<MeshFilter>();
        Assert.Less(arm.sharedMesh.bounds.size.magnitude, 4f,
            "A two-cell real delivery must not loop the unused 24-cell reach around the actor.");
    }

    [Test]
    public void Tick_LiveProjectile_ArmFollowsWithoutObserverPush()
    {
        GameObject projectile = new GameObject("Projectile");
        projectile.transform.position = new Vector3(1f, 1f, 0f);
        FootFrame frame = new FootFrame(Vector3.zero, Vector3.up, 1f);
        Assert.IsTrue(_rig.BeginDelivery(7, DeliveryStyle.Direct, projectile.transform, Vector3.one));
        _rig.Tick(0f, 0.2f, frame);

        projectile.transform.position = new Vector3(2f, 1f, 0f);
        _rig.Tick(0.2f, 0.016f, frame);

        FieldInfo field = typeof(CreatureRig).GetField("_arms", BindingFlags.Instance | BindingFlags.NonPublic);
        LianaArm[] arms = (LianaArm[])field.GetValue(_rig);
        Assert.That(Vector3.Distance(arms[0].goal, projectile.transform.position), Is.LessThan(0.00001f));
        Object.DestroyImmediate(projectile);
    }

    [Test]
    public void Init_GlowingPart_BrightensBaseColourAndKeepsAlpha()
    {
        _rig.Dispose();
        _recipe.parts[0].glow = 2f;
        _recipe.parts[0].colour = new Color(0.2f, 0.4f, 0.1f, 0.7f);
        _rig = CreateRig(_recipe, _parent.transform, _material);
        Renderer renderer = _rig.root.GetComponentInChildren<Renderer>();
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
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
        _rig = CreateRig(_recipe, _parent.transform, _material);
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
        _recipe = LookComposer.Compose(LookComposerTests.CreateChannels(side, HeadKind.Bud), LookVocabularyTests.Vocabulary());
        _rig = CreateRig(_recipe, _parent.transform, _material);
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
    public void Init_Tip_DrawsAWiderOutline()
    {
        CreateDerivedRig(LookSide.Plant);
        int tip = System.Array.FindIndex(_recipe.parts, part => part.role == PartRole.Tip);
        MaterialPropertyBlock block = new MaterialPropertyBlock();

        _rig.partTransforms[tip].GetComponent<Renderer>().GetPropertyBlock(block);

        Assert.AreEqual(CreatureRig.TipOutlineWidth, block.GetFloat("_HLOutlineWidthMultiplier"));
    }

    [Test]
    public void Tick_Crown_SpinsByRoleNotById()
    {
        _rig.Dispose();
        _recipe.parts[0].role = PartRole.Crown;
        _recipe.parts[0].id = "Ring";
        _rig = CreateRig(_recipe, _parent.transform, _material);
        FootFrame frame = new FootFrame(Vector3.zero, Vector3.up, 1f);
        _rig.Tick(0f, 0f, frame);
        Quaternion before = _rig.partTransforms[0].parent.localRotation;

        _rig.Tick(5f, 0f, frame);

        Assert.Greater(Quaternion.Angle(before, _rig.partTransforms[0].parent.localRotation), 1f);
    }

    [Test]
    public void Init_TwoFacedStone_DrawsOchreOnTheSecondSubmesh()
    {
        _rig.Dispose();
        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[] { Vector3.zero, Vector3.up, Vector3.right, Vector3.forward };
        mesh.subMeshCount = 2;
        mesh.SetTriangles(new int[] { 0, 1, 2 }, 0);
        mesh.SetTriangles(new int[] { 0, 2, 3 }, 1);
        StoneVariants variants = ScriptableObject.CreateInstance<StoneVariants>();
        variants.meshes = new Mesh[] { mesh };
        PrimitiveMeshes meshes = Object.Instantiate(PrimitiveMeshesTests.Meshes());
        meshes.stoneVariants = variants;
        _recipe.parts[0].primitive = Primitive.Stone;
        _rig = new CreatureRig();
        _rig.Init(_recipe, _parent.transform, _material, meshes);
        Renderer renderer = _rig.partTransforms[0].GetComponent<Renderer>();
        MaterialPropertyBlock block = new MaterialPropertyBlock();

        renderer.GetPropertyBlock(block, 1);

        Assert.AreEqual(2, renderer.sharedMaterials.Length);
        Assert.Less(((Vector4)_recipe.stoneOchre - (Vector4)block.GetColor("_BaseColor")).magnitude, 0.001f);
        Object.DestroyImmediate(meshes);
        Object.DestroyImmediate(variants);
        Object.DestroyImmediate(mesh);
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
    public void Begin_PoolSaturated_RefusesWithoutStealingAndDisposeIsIdempotent()
    {
        for (int i = 0; i < 8; i++)
        {
            Assert.AreNotEqual(0, _rig.Begin(GestureKind.Attack, Vector3.one));
        }

        Assert.AreEqual(0, _rig.Begin(GestureKind.Attack, Vector3.one));
        Assert.AreEqual(8, _rig.activeArmCount);
        _rig.Contact(0, Vector3.one);
        _rig.End(0);
        Assert.AreEqual(8, _rig.activeArmCount);
        _rig.CancelAll();
        _rig.Tick(0.79f, 0.05f, new FootFrame(Vector3.zero, Vector3.up, 1f));
        _rig.Tick(1f, 0.21f, new FootFrame(Vector3.zero, Vector3.up, 1f));
        Assert.AreEqual(0, _rig.activeArmCount);
        _rig.Dispose();
        _rig.Dispose();
        Assert.IsFalse(_rig.root);
    }

    [Test]
    public void Tick_Roots_AreJointedCylinderChainsDownToTheFoot()
    {
        _rig.Tick(0f, 0.016f, new FootFrame(Vector3.zero, Vector3.up, 1f));

        int segments = 0;
        int joints = 0;
        float lowest = float.MaxValue;
        foreach (Transform child in _rig.root)
        {
            if (child.name == "Root")
            {
                segments++;
                Assert.AreSame(PrimitiveMeshesTests.Meshes().cylinder, child.GetComponent<MeshFilter>().sharedMesh);
                lowest = Mathf.Min(lowest, child.GetComponent<Renderer>().bounds.min.y);
            }
            else if (child.name == "RootJoint")
            {
                joints++;
            }
        }

        Assert.AreEqual(_recipe.roots.count * _recipe.roots.segments, segments);
        Assert.AreEqual(_recipe.roots.count * (_recipe.roots.segments - 1), joints);
        Assert.That(lowest, Is.LessThan(_recipe.roots.thickness)); // the last segment lies on the ground
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

        _rig = CreateRig(_recipe, _parent.transform, _material);

        Transform pivot = _rig.root.Find("Sway/Body/Child");
        Assert.AreEqual(Vector3.up, pivot.localPosition);
        Assert.AreEqual(Vector3.one, pivot.lossyScale);
    }

    [Test]
    public void Contact_CancelledSaturatedLease_StaysCancelled()
    {
        for (int i = 0; i < 8; i++)
        {
            int token = _rig.Begin(GestureKind.Attack, Vector3.one);
            _rig.Contact(token, Vector3.one);
        }

        _rig.CancelAll();
        _rig.Tick(0.05f, 0.05f, new FootFrame(Vector3.zero, Vector3.up, 1f));
        _rig.Contact(0, Vector3.one);
        _rig.Tick(0.26f, 0.21f, new FootFrame(Vector3.zero, Vector3.up, 1f));

        Assert.AreEqual(0, _rig.activeArmCount);
    }

    [Test]
    public void Init_NonuniformAncestors_LogsAndLeavesViewEmpty()
    {
        _parent.transform.localScale = new Vector3(1f, 2f, 1f);
        CreatureRig rig = new CreatureRig();
        LogAssert.Expect(LogType.Error, "[CreatureRig] Creature rig ancestors must have positive uniform scale.");

        bool isInitialized = rig.Init(_recipe, _parent.transform, _material, PrimitiveMeshesTests.Meshes());

        Assert.IsFalse(isInitialized);
        Assert.IsNull(rig.root);
    }

    [Test]
    public void BeginDelivery_SwarmCapAndStaleOrUnsupportedTokens_AreRefused()
    {
        Assert.IsFalse(_rig.BeginDelivery(1, DeliveryStyle.Thrown, null, Vector3.one));
        for (int i = 1; i <= 4; i++)
        {
            Assert.IsTrue(_rig.BeginDelivery(i, DeliveryStyle.Swarm, null, Vector3.one));
        }

        Assert.IsFalse(_rig.BeginDelivery(5, DeliveryStyle.Swarm, null, Vector3.one));
        Assert.IsFalse(_rig.BeginDelivery(1, DeliveryStyle.Direct, null, Vector3.one));
        _rig.EndDelivery(1);
        _rig.EndDelivery(1);
        Assert.IsTrue(_rig.BeginDelivery(5, DeliveryStyle.Swarm, null, Vector3.one));
    }

    [Test]
    public void ContactDelivery_ChainSync_CollectsContactsAndRetractsTogether()
    {
        Assert.IsTrue(_rig.BeginDelivery(123, DeliveryStyle.ChainSync, null, Vector3.one));
        _rig.ContactDelivery(123, Vector3.one, null);
        _rig.ContactDelivery(123, Vector3.right * 2f, null);
        Assert.AreEqual(2, _rig.activeArmCount);
        _rig.Tick(0.1f, 0.1f, new FootFrame(Vector3.zero, Vector3.up, 1f));
        Assert.AreEqual(2, _rig.activeArmCount);
        _rig.EndDelivery(123);
        _rig.Tick(0.4f, 0.3f, new FootFrame(Vector3.zero, Vector3.up, 1f));
        Assert.AreEqual(0, _rig.activeArmCount);
    }

    [Test]
    public void Init_StonePartWithoutVariants_BuildsOnTheBoulder()
    {
        _rig.Dispose();
        _recipe.parts[0].primitive = Primitive.Stone;
        _recipe.parts[0].variant = 3;

        _rig = CreateRig(_recipe, _parent.transform, _material);

        Mesh mesh = _rig.partTransforms[0].GetComponent<MeshFilter>().sharedMesh;
        Assert.AreEqual(PrimitiveMeshesTests.Meshes().boulder, mesh);
    }

    [Test]
    public void Init_Recipe_OnePartTransformPerPart()
    {
        _rig.Dispose();
        CreatureRecipe data = AssetDatabase.LoadAssetAtPath<CreatureRecipe>("Assets/Render/Creatures/Data/SpiralFern.asset");

        _rig = CreateRig(data, _parent.transform, _material);

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
        Assert.Greater(_rig.aim.eulerAngles.y, 0);
        Assert.Less(_rig.aim.eulerAngles.y, 90);
        Transform sway = _rig.root.Find("Sway");
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
}

}
