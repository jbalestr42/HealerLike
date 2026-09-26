using System.Collections.Generic;
using NUnit.Framework;
using HealerLike.Render.Creatures;
using UnityEngine;

namespace HealerLike.Render.Zones
{

public class TrampleRigTestHost : ARigHost
{
    public bool Build(CreatureRecipe recipe, Material material)
    {
        return BuildRig(recipe, transform, material, material, RenderTestAssets.LoadMeshes(), null, 1f);
    }
    public void Clear() { ReleaseRig(); }
    public override void OnHealthResolved(GameObject target, float value, bool critical) { }
}

public class TrampleZoneTests
{
    GameObject _root;
    GameObject _obstacle;
    ZoneRegistry _registry;
    TrampleZone _zone;
    CreatureRecipe _recipe;
    TrampleRigTestHost _host;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("zones");
        _obstacle = new GameObject("obstacle");
        _registry = _root.AddComponent<ZoneRegistry>();
        _registry.Init(new ZoneFakeUpload());
        _zone = _obstacle.AddComponent<TrampleZone>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_host != null) _host.Clear();
        if (_recipe != null) Object.DestroyImmediate(_recipe);
        Object.DestroyImmediate(_obstacle);
        Object.DestroyImmediate(_root);
    }

    [Test]
    public void Refresh_ObstacleMovesOrTurnsInvalid_UpdatesOneHandleAndRemovesIt()
    {
        _zone.Init(_registry);
        _zone.Refresh();
        _registry.PublishFrame(1f);
        Assert.AreEqual((int)ZoneKind.Trample, _registry.snapshot[0].kind);

        _obstacle.transform.position = Vector3.forward;
        _zone.radius = 2f;
        for (int i = 0; i < 50; i++)
        {
            _zone.Refresh();
        }

        _registry.PublishFrame(1f);
        Assert.AreEqual(1, _registry.count);
        Assert.AreEqual(Vector3.forward, _registry.snapshot[0].position);
        Assert.AreEqual(2, _registry.snapshot[0].radius);

        _zone.radius = float.NaN;
        _zone.Refresh();
        Assert.AreEqual(0, _registry.liveCount);

        _zone.radius = 1f;
        _zone.Refresh();
        Assert.AreEqual(1, _registry.liveCount);

        _zone.enabled = false;
        TestHelpers.InvokePrivate(_zone, "OnDisable");
        Assert.AreEqual(0, _registry.liveCount);

        _zone.enabled = true;
        _zone.Refresh();
        Assert.AreEqual(1, _registry.liveCount);

        TestHelpers.InvokePrivate(_zone, "OnDestroy");
        Assert.AreEqual(0, _registry.liveCount);
    }

    [Test]
    public void Refresh_Steady_AllocatesNothing()
    {
        _zone.Init(_registry);
        for (int i = 0; i < 100; i++)
        {
            _zone.Refresh();
        }

        long before = System.GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
        {
            _zone.Refresh();
        }
        long allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(0, allocated);
    }

    [Test]
    public void Init_RecreatedRegistry_MovesTheFootprintThere()
    {
        _zone.Init(_registry);
        _zone.Refresh();
        Object.DestroyImmediate(_root);
        _zone.Refresh();
        _root = new GameObject("replacement");
        _registry = _root.AddComponent<ZoneRegistry>();
        _registry.Init(new ZoneFakeUpload());

        _zone.Init(_registry);
        _zone.Refresh();

        Assert.AreEqual(1, _registry.liveCount);
    }

    [Test]
    public void CreatureFootprint_ScaledRoot_IsHalfTheLargerHorizontalScale()
    {
        _obstacle.transform.localScale = new Vector3(0.9f, 3f, -1.2f);

        float footprint = TrampleZone.CreatureFootprint(_obstacle.transform);

        Assert.AreEqual(0.6f, footprint, 0.0001f); // 1 cell * 0.5 * |-1.2|
        Assert.AreEqual(0.75f, TrampleZone.TrampleRadius(footprint), 0.0001f); // + 0.15 margin
        Assert.AreEqual(TrampleZone.Margin, TrampleZone.TrampleRadius(-1f), 0.0001f);
    }

    void BuildRig()
    {
        _host = _obstacle.AddComponent<TrampleRigTestHost>();
        Assert.IsTrue(_host.Build(_recipe, RenderTestAssets.LoadLookMaterial()));
        _host.rig.Tick(0f, 0f, new FootFrame(_obstacle.transform.position, Vector3.up, 1f));
        _zone.InitFootprint(_registry);
    }

    [Test]
    public void Refresh_RecomposedRootReach_ResizesOneFootprintAndSteadyRefreshAllocatesNothing()
    {
        _recipe = RenderTestAssets.CreateRecipe();
        _recipe.roots.footRadius = 0.8f;
        _recipe.roots.thickness = 0.08f;
        _recipe.idle = default;
        BuildRig();
        Assert.That(_zone.radius, Is.EqualTo(0.8f + 0.08f + TrampleZone.Margin).Within(0.0001f));
        _zone.Refresh();
        _recipe.roots.footRadius = 1.8f;
        Assert.IsTrue(_host.rig.Recompose(_recipe, RenderTestAssets.LoadLookMaterial(),
            RenderTestAssets.LoadLookMaterial(), RenderTestAssets.LoadMeshes()));
        _host.rig.Tick(0f, 0f, new FootFrame(Vector3.zero, Vector3.up, 1f));
        _zone.Refresh();
        Assert.That(_zone.radius, Is.EqualTo(1.8f + 0.08f + TrampleZone.Margin).Within(0.0001f));
        Assert.AreEqual(0, Count(ZoneKind.Trample), "A creature presses as a body, not as a disc.");
        Assert.AreEqual(1, _registry.bodyCount);
        for (int i = 0; i < 100; i++) _zone.Refresh();
        long before = System.GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++) _zone.Refresh();
        Assert.AreEqual(0, System.GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Test]
    public void CreatureFootprint_StoneBasalMassAndFeet_ExcludeElevatedHeadAndArms()
    {
        _recipe = RenderTestAssets.CreateRecipe();
        _recipe.roots.count = 0;
        _recipe.idle = default;
        _recipe.parts = new[]
        {
            new CreaturePart { id = "Body", parent = -1, shape = ShapeProfile.Block(),
                dimensions = Vector3.one * 0.4f, colour = Color.white, role = PartRole.Body },
            new CreaturePart { id = "ExtraBase", parent = 0, shape = ShapeProfile.Block(),
                localPosition = new Vector3(0.8f, 0f, 0f), dimensions = new Vector3(0.6f, 0.3f, 0.4f),
                colour = Color.white, role = PartRole.Body },
            new CreaturePart { id = "Foot", parent = 0, shape = ShapeProfile.Block(),
                localPosition = new Vector3(-1.2f, 0f, 0f), dimensions = Vector3.one * 0.4f,
                colour = Color.white, role = PartRole.Limb },
            new CreaturePart { id = "WideHead", parent = 0, shape = ShapeProfile.Block(),
                localPosition = new Vector3(0f, 4f, 0f), dimensions = new Vector3(12f, 1f, 12f),
                colour = Color.white, role = PartRole.Head }
        };
        BuildRig();
        float footprint = TrampleZone.CreatureFootprint(_obstacle.transform, _host.rig);
        Assert.That(footprint, Is.EqualTo(Mathf.Sqrt(1.4f * 1.4f + 0.2f * 0.2f)).Within(0.001f));
        Assert.That(_zone.radius, Is.LessThan(1.6f), "The wide head and gesture arms must not clear a large disc.");
        _recipe.parts[2].localPosition = new Vector3(-0.3f, 0f, 0f);
        Assert.IsTrue(_host.rig.Recompose(_recipe, RenderTestAssets.LoadLookMaterial(),
            RenderTestAssets.LoadLookMaterial(), RenderTestAssets.LoadMeshes()));
        _zone.Refresh();
        Assert.That(_zone.radius, Is.EqualTo(Mathf.Sqrt(1.1f * 1.1f + 0.2f * 0.2f) + TrampleZone.Margin)
            .Within(0.001f), "The extra basal piece remains part of the footprint despite parenting to Body.");
    }

    [Test]
    public void InitFootprint_Rig_RegistersOneBodyAndNoDisc()
    {
        _recipe = RenderTestAssets.CreateRecipe();
        _recipe.idle = default;
        BuildRig();
        _zone.Refresh();
        _zone.InitFootprint(_registry);

        Assert.IsTrue(_zone.isBody);
        Assert.AreEqual(1, _registry.bodyCount);
        Assert.AreEqual(0, Count(ZoneKind.Trample));

        TestHelpers.InvokePrivate(_zone, "OnDisable");
        Assert.AreEqual(0, _registry.bodyCount, "A disabled body stops pressing.");
        TestHelpers.InvokePrivate(_zone, "OnEnable");
        Assert.AreEqual(1, _registry.bodyCount);
        TestHelpers.InvokePrivate(_zone, "OnDestroy");
        Assert.AreEqual(0, _registry.bodyCount);
    }

    [Test]
    public void AppendCapsules_Rig_SendsTheLowMeshesAndSkipsTheRaisedOnes()
    {
        _recipe = RenderTestAssets.CreateRecipe();
        _recipe.roots.count = 0;
        _recipe.idle = default;
        _recipe.parts = new[]
        {
            new CreaturePart { id = "Body", parent = -1, shape = ShapeProfile.Block(),
                dimensions = Vector3.one * 0.4f, colour = Color.white, role = PartRole.Body },
            new CreaturePart { id = "Head", parent = 0, shape = ShapeProfile.Block(),
                localPosition = new Vector3(0f, 4f, 0f), dimensions = Vector3.one * 0.5f,
                colour = Color.white, role = PartRole.Head }
        };
        _recipe.arms = new ArmDefinition[0];
        BuildRig();
        BodyCapsule[] capsules = new BodyCapsule[8];

        int count = _zone.AppendCapsules(capsules, 1);

        Assert.AreEqual(1, count, "The head floats past the grass's reach.");
        Assert.Less(capsules[1].bottom, TrampleZone.BodyReach);
        Assert.Greater(capsules[1].radius, 0f);
        Assert.AreEqual(0, _zone.AppendCapsules(capsules, capsules.Length), "No room, nothing written.");
        Assert.AreEqual(0, _zone.AppendCapsules(null, 0));
    }

    [Test]
    public void AppendCapsules_Steady_AllocatesNothing()
    {
        _recipe = RenderTestAssets.CreateRecipe();
        _recipe.idle = default;
        BuildRig();
        BodyCapsule[] capsules = new BodyCapsule[TrampleZone.MaxCapsules];
        _zone.AppendCapsules(capsules, 0);

        long before = System.GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 200; i++)
        {
            _zone.AppendCapsules(capsules, 0);
        }

        Assert.AreEqual(0, System.GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Test]
    public void AppendCapsules_Obstacle_IsNoBody()
    {
        _zone.Init(_registry);

        Assert.IsFalse(_zone.isBody);
        Assert.AreEqual(0, _zone.AppendCapsules(new BodyCapsule[4], 0));
        Assert.AreEqual(0, _registry.bodyCount);
    }

    // A root-legged creature standing at the origin, its landing on its first refresh already counted
    void BuildRootedCreature()
    {
        _recipe = RenderTestAssets.CreateRecipe();
        _recipe.idle = default;
        _recipe.arms = new ArmDefinition[0];
        _recipe.roots.count = 6;
        _recipe.roots.footRadius = 0.8f;
        _recipe.roots.thickness = 0.08f;
        BuildRig();
    }

    int CountShocks()
    {
        return Count(ZoneKind.Shock);
    }

    // Zones of one kind in the published snapshot; landings add shock pulses beside the footprints
    int Count(ZoneKind kind)
    {
        _registry.PublishFrame(0f);
        int count = 0;
        foreach (Zone zone in _registry.snapshot)
        {
            count += zone.kind == (int)kind ? 1 : 0;
        }

        return count;
    }

    [Test]
    public void Refresh_FirstStand_ThrowsARingOutOfEveryFootAndOneRoundTheBody()
    {
        BuildRootedCreature();

        _zone.Refresh();

        Assert.AreEqual(1, _zone.landings);
        Assert.AreEqual(6 + 1, CountShocks());
        _zone.Refresh();
        Assert.AreEqual(1, _zone.landings, "Standing still lands once.");
    }

    [Test]
    public void Refresh_Held_BrushesTheGrassAndLandsWhenLetGo()
    {
        BuildRootedCreature();
        GameObject grip = new GameObject("collider");
        grip.transform.SetParent(_obstacle.transform, false);
        Collider collider = grip.AddComponent<BoxCollider>();
        TestHelpers.SetPrivateField(_zone, "_hold", collider);
        _zone.Refresh();

        grip.layer = Layers.IgnoreRaycast;
        _obstacle.transform.position = new Vector3(3f, 0f, 0f);
        _zone.Refresh();

        Assert.Greater(_zone.AppendCapsules(new BodyCapsule[TrampleZone.MaxCapsules], 0), 0,
            "Dragged over the grass, it leaves a trail.");
        Assert.AreEqual(1, _zone.landings, "Hopping from cell to cell while held lands nowhere.");

        grip.layer = 0;
        _zone.Refresh();

        Assert.AreEqual(2, _zone.landings);
        Assert.Greater(_zone.AppendCapsules(new BodyCapsule[TrampleZone.MaxCapsules], 0), 0);
    }

    [Test]
    public void Refresh_JumpIntoPlace_LandsButAStepDoesNot()
    {
        BuildRootedCreature();
        _zone.Refresh();

        _obstacle.transform.position += new Vector3(0.1f, 0f, 0f);
        _zone.Refresh();
        Assert.AreEqual(1, _zone.landings, "A walking step is no landing.");

        _obstacle.transform.position += new Vector3(1f, 0f, 0f);
        _zone.Refresh();
        Assert.AreEqual(2, _zone.landings, "A swap moves it a whole cell at once.");
    }

    [Test]
    public void CollectBodyMeshes_Rig_TakesThePartsAndRootsButNotTheArms()
    {
        _recipe = RenderTestAssets.CreateRecipe();
        _recipe.idle = default;
        _recipe.roots.count = 4;
        _recipe.roots.footRadius = 0.8f;
        _recipe.roots.thickness = 0.08f;
        BuildRig();
        List<MeshFilter> meshes = new List<MeshFilter>();

        _host.rig.CollectBodyMeshes(meshes);

        int roots = _recipe.roots.count * _recipe.roots.segments + _recipe.roots.count * (_recipe.roots.segments - 1);
        Assert.AreEqual(_recipe.parts.Length + roots, meshes.Count);
        Assert.AreEqual(_host.rig.partTransforms[0], meshes[meshes.Count - 1].transform,
            "The feet come first, so a many-part body never crowds them out.");
        foreach (MeshFilter filter in meshes)
        {
            Assert.AreNotEqual("LianaChain", filter.sharedMesh.name, "An arm is one mesh along its whole chain.");
        }

        bool hasArm = false;
        foreach (MeshFilter filter in _host.rig.root.GetComponentsInChildren<MeshFilter>())
        {
            hasArm |= filter.sharedMesh != null && filter.sharedMesh.name == "LianaChain";
        }

        Assert.IsTrue(hasArm, "The rig does draw its arm, the body only leaves it out.");
    }

    [Test]
    public void Feet_Roots_RestAtTheFootRadiusAroundTheRoot()
    {
        GameObject root = new GameObject("root");
        try
        {
            root.transform.position = new Vector3(1f, 0f, 2f);
            RootDefinition roots = new RootDefinition { count = 4, footRadius = 0.5f };
            List<Vector3> feet = new List<Vector3>();

            TrampleZone.Feet(root.transform, roots, 2f, feet);

            Assert.AreEqual(4, feet.Count);
            Assert.That(Vector3.Distance(new Vector3(2f, 0f, 2f), feet[0]), Is.LessThan(1e-5f));
            Assert.That(Vector3.Distance(new Vector3(1f, 0f, 3f), feet[1]), Is.LessThan(1e-5f));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void Refresh_InitWithZones_AddsOneFootprintThere()
    {
        _zone.Init(_registry);
        _zone.Refresh();
        _zone.Refresh();

        Assert.AreEqual(1, Count(ZoneKind.Trample));
        Assert.AreEqual(1, Count(ZoneKind.Shock), "An obstacle lands once with a ring round itself.");
    }

    [Test]
    public void Refresh_InitWithoutZones_AddsNothing()
    {
        _zone.Init(null);
        _zone.Refresh();

        Assert.AreEqual(0, _registry.liveCount);
    }
}

}
