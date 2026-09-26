using System.Collections.Generic;
using HealerLike.Render.Grass;
using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Stage
{

// The game scene as the manager sees it: a main camera, a directional light, the grid and its ground, decoration,
// and the shipped RenderManager beside it
public class StageSceneFixture
{
    public static readonly string PrefabPath = "Assets/Render/Stage/Prefabs/RenderManager.prefab";

    public RenderManager manager;
    public EntityManager entityManager;
    public PlayerBehaviour player;
    public GameObject gameGo;
    public GameObject cameraGo;
    public GameObject sunGo;
    public GameObject decorationGo;
    public GameObject farGroundGo;
    public Renderer ground;

    readonly List<GameObject> _created = new List<GameObject>();
    RenderPipelineAsset _previousPipeline;
    Light _previousSun;
    AmbientMode _previousAmbient;

    public void Create()
    {
        _previousPipeline = QualitySettings.renderPipeline;
        _previousSun = RenderSettings.sun;
        _previousAmbient = RenderSettings.ambientMode;

        GameObject managerGo = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
        _created.Add(managerGo);
        manager = managerGo.GetComponent<RenderManager>();
        // The test scene may already hold a main camera, the manager adopts whichever Camera.main returns
        GameObject createdCameraGo = new GameObject("Main Camera");
        _created.Add(createdCameraGo);
        createdCameraGo.tag = "MainCamera";
        createdCameraGo.AddComponent<Camera>();
        cameraGo = Camera.main.gameObject;
        sunGo = new GameObject("Directional Light");
        _created.Add(sunGo);
        sunGo.AddComponent<Light>().type = LightType.Directional;
        decorationGo = CreatePrimitive(PrimitiveType.Sphere, "MiddleLine");
        CreatePrimitive(PrimitiveType.Sphere, "Sphere");
        farGroundGo = CreatePrimitive(PrimitiveType.Plane, "Ground");

        gameGo = new GameObject("Game");
        _created.Add(gameGo);
        entityManager = gameGo.AddComponent<EntityManager>();
        player = gameGo.AddComponent<PlayerBehaviour>();
        player.grid = CreateGrid(gameGo.transform);
    }

    // Edit mode runs no OnDestroy, so the buffers and the pipeline are given back here
    public void Destroy()
    {
        if (manager != null)
        {
            // Environment strips also own draw subscriptions; runtime OnDisable does not run in this fixture.
            foreach (GrassField field in manager.GetComponentsInChildren<GrassField>(true))
            {
                field.Release();
            }

            manager.zones.Release();
        }

        QualitySettings.renderPipeline = _previousPipeline;
        RenderSettings.sun = _previousSun;
        RenderSettings.ambientMode = _previousAmbient;
        foreach (GameObject createdGo in _created)
        {
            Object.DestroyImmediate(createdGo);
        }

        _created.Clear();
    }

    GameObject CreatePrimitive(PrimitiveType type, string name)
    {
        GameObject primitiveGo = GameObject.CreatePrimitive(type);
        primitiveGo.name = name;
        _created.Add(primitiveGo);
        return primitiveGo;
    }

    GridManager CreateGrid(Transform parent)
    {
        GameObject gridGo = new GameObject("Grid");
        gridGo.transform.SetParent(parent, false);
        GridManager grid = gridGo.AddComponent<GridManager>();
        grid.width = 16;
        grid.height = 16;
        grid.size = 1f;
        GameObject groundGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        groundGo.name = "Ground";
        groundGo.transform.SetParent(gridGo.transform, false);
        groundGo.transform.localScale = new Vector3(16f, 1f, 16f);
        ground = groundGo.GetComponent<Renderer>();
        TestHelpers.SetPrivateField(grid, "_ground", groundGo);
        return grid;
    }
}

public class RenderManagerTests
{
    StageSceneFixture _scene;

    [SetUp]
    public void SetUp()
    {
        _scene = new StageSceneFixture();
        _scene.Create();
    }

    [TearDown]
    public void TearDown()
    {
        _scene.Destroy();
    }

    [Test]
    public void Init_NullEntityManager_StaysDetached()
    {
        TestHelpers.WithLoggingDisabled(() => _scene.manager.Init(null, null));

        Assert.IsNull(_scene.manager.entityManager);
        Assert.IsNull(_scene.manager.player);
    }

    [Test]
    public void Init_GameScene_AdoptsTheGameCameraAndSwapsThePipeline()
    {
        RenderPipelineAsset stagePipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(
            "Assets/Render/Stage/Settings/StagePipeline.asset");

        _scene.manager.Init(_scene.entityManager, _scene.player);

        Assert.AreSame(_scene.entityManager, _scene.manager.entityManager);
        Assert.AreSame(_scene.cameraGo.GetComponent<Camera>(), _scene.manager.gameCamera);
        Assert.AreEqual(CameraClearFlags.SolidColor, _scene.manager.gameCamera.clearFlags);
        Assert.AreEqual(_scene.manager.overviewPose.position, _scene.cameraGo.transform.position);
        Assert.AreSame(stagePipeline, QualitySettings.renderPipeline);
    }

    [Test]
    public void Init_GameScene_BuildsTheEnvironmentAndTheBoardGrass()
    {
        _scene.manager.Init(_scene.entityManager, _scene.player);

        Assert.IsNotNull(_scene.manager.gust);
        Assert.IsNotNull(_scene.manager.foreground);
        Assert.IsNotNull(_scene.manager.zones.buffer);
        Assert.IsNotNull(_scene.manager.spellSink);
    }

    [Test]
    public void Init_SameEntityManagerTwice_AttachesOnce()
    {
        _scene.manager.Init(_scene.entityManager, _scene.player);
        Object gust = _scene.manager.gust;

        _scene.manager.Init(_scene.entityManager, _scene.player);

        Assert.AreSame(gust, _scene.manager.gust);
    }

    [Test]
    public void LateUpdate_Attached_PublishesTheFrameZones()
    {
        _scene.manager.Init(_scene.entityManager, _scene.player);
        _scene.manager.zones.Add(ZoneKind.Heal, Vector3.zero, 1f, 1f);

        TestHelpers.InvokePrivate(_scene.manager, "LateUpdate");

        Assert.AreEqual(1, _scene.manager.zones.count);
    }

    [Test]
    public void Destroy_FixtureWithBuiltEnvironment_ReleasesEveryOwnedDraw()
    {
        _scene.manager.Init(_scene.entityManager, _scene.player);
        GrassField[] fields = _scene.manager.GetComponentsInChildren<GrassField>(true);
        foreach (GrassField field in fields)
        {
            field.tuftBudget = 65;
        }

        TestHelpers.InvokePrivate(_scene.manager, "LateUpdate");
        List<GrassDraw> draws = new List<GrassDraw>();
        List<GraphicsBuffer> arguments = new List<GraphicsBuffer>();
        foreach (GrassField field in fields)
        {
            if (!field.isReady)
            {
                continue;
            }

            Assert.IsNotNull(field.tuftDraw, field.name);
            Assert.IsNotNull(field.socleDraw, field.name);
            draws.Add(field.tuftDraw);
            draws.Add(field.socleDraw);
            arguments.Add(field.tuftDraw.arguments);
            arguments.Add(field.socleDraw.arguments);
        }

        Assert.Greater(draws.Count, 2, "Both the board and its environment must have built draws.");
        foreach (GraphicsBuffer buffer in arguments)
        {
            Assert.IsTrue(buffer.IsValid());
        }

        _scene.Destroy();

        foreach (GrassDraw draw in draws)
        {
            Assert.IsNull(draw.arguments, "Every owned draw must release its argument buffer.");
        }

        foreach (GraphicsBuffer buffer in arguments)
        {
            Assert.IsFalse(buffer.IsValid());
        }
    }

    [Test]
    public void NextDeliveryToken_Twice_CountsUpFromOne()
    {
        Assert.AreEqual(1, _scene.manager.NextDeliveryToken());
        Assert.AreEqual(2, _scene.manager.NextDeliveryToken());
    }
}

}
