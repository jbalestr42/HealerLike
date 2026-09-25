using System.Linq;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Environment
{

public class EnvironmentScatterTests
{
    GameObject _go;
    GameObject _otherGo;
    PrimitiveMeshes _meshes;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("ScatterTest");
        _otherGo = new GameObject("ScatterOther");
        _meshes = AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>("Assets/Render/Creatures/Data/PrimitiveMeshes.asset");
    }

    [TearDown]
    public void TearDown()
    {
        DestroyWithGeneratedMeshes(_go);
        DestroyWithGeneratedMeshes(_otherGo);
    }

    // Stones are generated per seed; the baked primitive meshes are assets and stay
    static void DestroyWithGeneratedMeshes(GameObject go)
    {
        foreach (MeshFilter filter in go.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh != null && !EditorUtility.IsPersistent(filter.sharedMesh))
            {
                Object.DestroyImmediate(filter.sharedMesh);
            }
        }

        Object.DestroyImmediate(go);
    }

    EnvironmentScatter Make(int seed)
    {
        return Make(_go, seed);
    }

    EnvironmentScatter Make(GameObject go, int seed)
    {
        EnvironmentScatter scatter = go.AddComponent<EnvironmentScatter>();
        TestHelpers.SetPrivateField(scatter, "_palette",
            AssetDatabase.LoadAssetAtPath<LookPalette>("Assets/Render/Grammar/Data/LookPalette.asset"));
        EnvironmentSettings settings = EnvironmentSettings.Default;
        settings.seed = seed;
        settings.counts = new EnvironmentCounts
        {
            boulders = 6,
            cairns = 3,
            monoliths = 2,
            mushroomTrees = 4,
            spiralFerns = 4,
            bladeRosettes = 4,
            sphereClusters = 4
        };
        scatter.settings = settings;
        return scatter;
    }

    // What the environment root hands the scatter, without a camera or a gust
    void Init(EnvironmentScatter scatter, Rect grid)
    {
        scatter.Init(grid, 1f, 0.5f, null, null, 60f, _meshes);
    }

    Transform FirstPivot(EnvironmentScatter scatter, string name)
    {
        return Enumerable.Range(0, scatter.root.childCount)
            .Select(i => scatter.root.GetChild(i))
            .First(t => t.name == name);
    }

    Vector3[] PivotPositions(EnvironmentScatter scatter)
    {
        return Enumerable.Range(0, scatter.root.childCount)
            .Select(i => scatter.root.GetChild(i).position)
            .ToArray();
    }

    [Test]
    public void Frame_PortraitCamera_FitsCrownsAndPreservesOutsideBoardGroundPositions()
    {
        EnvironmentScatter scatter = Make(5);
        Camera camera = _otherGo.AddComponent<Camera>();
        camera.fieldOfView = 40f;
        camera.aspect = 9f / 16f;
        // The native first battle's focused view, with the hidden healer excluded from its bounds.
        camera.transform.SetPositionAndRotation(new Vector3(-5.52f, 14.73f, -1f), Quaternion.Euler(52f, 90f, 0f));
        Rect board = new Rect(-8f, -8f, 16f, 16f);
        scatter.Init(board, 1f, 0.5f, camera, null, 50f, _meshes);
        Vector3[] positions = PivotPositions(scatter);
        scatter.Frame(camera);
        CollectionAssert.AreEqual(positions, PivotPositions(scatter));
        foreach (EnvironmentItem item in scatter.items)
            Assert.IsFalse(EnvironmentLayout.InsideMargin(board, 1f, new Vector2(item.position.x, item.position.z)));
        foreach (string name in new[] { "Monolith", "MushroomTree" })
        {
            Transform landmark = FirstPivot(scatter, name);
            Assert.IsTrue(landmark.gameObject.activeSelf, name);
            Renderer[] renderers = landmark.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            Vector3 centre = camera.WorldToViewportPoint(bounds.center);
            Assert.That(centre.x, Is.InRange(0.01f, 0.99f), name);
            Assert.That(centre.y, Is.InRange(0.5f, EnvironmentFraming.CrownCeiling), name);
        }
        for (int i = 0; i < scatter.items.Count; i++)
        {
            EnvironmentKind kind = scatter.items[i].kind;
            if (kind != EnvironmentKind.MushroomTree && kind != EnvironmentKind.Monolith) continue;
            Transform pivot = scatter.root.GetChild(i);
            if (!pivot.gameObject.activeSelf) continue;
            Renderer[] renderers = pivot.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            Vector3 centre = camera.WorldToViewportPoint(bounds.center);
            if (centre.z <= 0f || centre.x < 0f || centre.x > 1f) continue;
            for (int corner = 0; corner < 8; corner++)
                Assert.That(camera.WorldToViewportPoint(RenderMath.Corner(bounds, corner)).y,
                    Is.LessThanOrEqualTo(EnvironmentFraming.CrownCeiling + 0.001f));
        }
    }

    [Test]
    public void Init_SameSeed_SpawnsSamePivotsOutsideGrid()
    {
        EnvironmentScatter scatter = Make(5);
        Rect grid = new Rect(-8f, -8f, 16f, 16f);

        Init(scatter, grid);

        Assert.That(scatter.items.Count, Is.GreaterThan(0));
        Assert.AreEqual(scatter.items.Count, scatter.root.childCount);
        Vector3[] first = PivotPositions(scatter);
        foreach (Vector3 point in first)
        {
            Assert.IsFalse(EnvironmentLayout.InsideMargin(grid, 1f, new Vector2(point.x, point.z)));
        }
        foreach (MeshRenderer meshRenderer in scatter.root.GetComponentsInChildren<MeshRenderer>())
        {
            // No part may reach into the grid itself
            Bounds b = meshRenderer.bounds;
            Assert.IsFalse(b.min.x > grid.xMin && b.max.x < grid.xMax && b.min.z > grid.yMin
                && b.max.z < grid.yMax, meshRenderer.name);
        }

        EnvironmentScatter other = Make(_otherGo, 5);
        Init(other, grid);

        Assert.AreEqual(first, PivotPositions(other));
    }

    [Test]
    public void Init_Meshes_BuildsFromThem()
    {
        EnvironmentScatter scatter = Make(3);

        scatter.Init(new Rect(-8f, -8f, 16f, 16f), 1f, 0.5f, null, null, 60f, _meshes);

        Assert.That(scatter.items.Count, Is.GreaterThan(0));
        Assert.AreEqual(scatter.items.Count, scatter.root.childCount);
        Assert.IsTrue(scatter.root.GetComponentsInChildren<MeshFilter>().Any(f => f.sharedMesh == _meshes.capsule));
        Assert.IsTrue(scatter.items.All(i => i.position.y == 0.5f));
    }

    [Test]
    public void Init_MixedKinds_SwaysOnlyThePlants()
    {
        EnvironmentScatter scatter = Make(9);
        Init(scatter, new Rect(-8f, -8f, 16f, 16f));
        EnvironmentSway sway = scatter.root.GetComponent<EnvironmentSway>();
        Quaternion[] rest = Enumerable.Range(0, scatter.root.childCount)
            .Select(i => scatter.root.GetChild(i).localRotation)
            .ToArray();

        sway.Animate(10);

        for (int i = 0; i < scatter.items.Count; i++)
        {
            EnvironmentKind kind = scatter.items[i].kind;
            bool isStone = kind == EnvironmentKind.Boulder || kind == EnvironmentKind.Cairn
                || kind == EnvironmentKind.Monolith;
            // Exact components, since Quaternion.Angle reads a turn under a sixth of a degree as none
            bool isMoved = !rest[i].Equals(scatter.root.GetChild(i).localRotation);
            Assert.AreEqual(!isStone, isMoved, kind.ToString());
        }
    }

    [Test]
    public void Init_Gust_OpensFernJointsAndNodsCaps()
    {
        EnvironmentScatter scatter = Make(5);
        EnvironmentGust gust = _go.AddComponent<EnvironmentGust>();
        scatter.Init(new Rect(-8f, -8f, 16f, 16f), 1f, 0.5f, null, gust, 100f, _meshes);
        EnvironmentSway sway = scatter.root.GetComponent<EnvironmentSway>();
        Transform fern = FirstPivot(scatter, "SpiralFern");
        Transform joint = fern.GetChild(0).GetChild(0).GetChild(1);
        double peak = Time.timeAsDouble + 1;
        sway.Animate(peak);
        float resting = joint.localEulerAngles.z;

        gust.Gust(Vector3.forward, 1f, 2f);
        sway.Animate(peak);

        Assert.That(Mathf.DeltaAngle(resting, joint.localEulerAngles.z), Is.GreaterThan(2f));
        Transform mushroom = FirstPivot(scatter, "MushroomTree");
        Transform cap = mushroom.Find("NoddingCap");
        Assert.IsNotNull(cap);
        Assert.AreEqual(2, cap.childCount);

        Quaternion before = cap.localRotation;
        sway.Animate(peak + 1);

        Assert.That(Quaternion.Angle(before, cap.localRotation), Is.GreaterThan(0.001f));
    }
}

}
