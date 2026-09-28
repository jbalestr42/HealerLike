using System.Collections.Generic;
using System.Reflection;
using HealerLike.Render.Grass;
using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

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
            // A backdrop's host is a scene root of its own, beside the manager
            manager.backdrop.Clear();
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

    // The menu has no game: the manager dresses it as a calm establishing shot of the same meadow
    [Test]
    public void AttachBackdrop_SceneWithoutAGame_BuildsTheMeadowAroundTheRestPose()
    {
        RenderPipelineAsset stagePipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(
            "Assets/Render/Stage/Settings/StagePipeline.asset");
        Scene scene = SceneManager.GetActiveScene();

        Assert.IsTrue(_scene.manager.AttachBackdrop(scene));

        Camera camera = _scene.cameraGo.GetComponent<Camera>();
        Pose rest = StageBackdrop.RestPose(StageBackdrop.Board);
        Assert.IsTrue(_scene.manager.backdrop.isAttached);
        Assert.IsNull(_scene.manager.entityManager, "The backdrop is not a game.");
        Assert.AreSame(camera, _scene.manager.gameCamera);
        Assert.AreEqual(CameraClearFlags.SolidColor, camera.clearFlags, "No default skybox behind the meadow.");
        Assert.AreEqual(0f, Vector3.Distance(rest.position, camera.transform.position), 0.001f);
        Assert.IsNotNull(_scene.manager.environment);
        Assert.IsNotNull(_scene.manager.gust);
        Assert.IsNotNull(_scene.manager.zones.buffer);
        Assert.AreSame(stagePipeline, QualitySettings.renderPipeline);
        Vector2 fog = StageCalibration.BackgroundFog(rest.position, StageBackdrop.Board, StageCalibration.PortraitYaw);
        Assert.AreEqual(fog.x, _scene.manager.look.settings.fogStart, 0.01f);
    }

    [Test]
    public void AttachBackdrop_AddsNoCameraListenerOrEventSystem()
    {
        int cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include).Length;
        int listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include).Length;
        int eventSystems = Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include)
            .Length;

        _scene.manager.AttachBackdrop(SceneManager.GetActiveScene());

        Assert.AreEqual(cameras, Object.FindObjectsByType<Camera>(FindObjectsInactive.Include).Length);
        Assert.AreEqual(listeners, Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include).Length);
        Assert.AreEqual(eventSystems,
            Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include).Length);
    }

    // A capture or the stage asking for an orientation must not throw the backdrop onto an unframed board
    [Test]
    public void SetLandscape_Backdrop_KeepsTheEstablishingShotAndItsFog()
    {
        _scene.manager.AttachBackdrop(SceneManager.GetActiveScene());
        Vector3 position = _scene.cameraGo.transform.position;
        float fogStart = _scene.manager.look.settings.fogStart;

        _scene.manager.SetLandscape(false);
        _scene.manager.FrameViewport(new Rect(0f, 0f, 1f, 1f), 9f / 16f);

        Assert.AreEqual(position, _scene.cameraGo.transform.position);
        Assert.AreEqual(fogStart, _scene.manager.look.settings.fogStart);
    }

    [Test]
    public void LateUpdate_Backdrop_KeepsTheMeadowMoving()
    {
        _scene.manager.AttachBackdrop(SceneManager.GetActiveScene());
        _scene.manager.zones.Add(ZoneKind.Heal, Vector3.zero, 1f, 1f);

        TestHelpers.InvokePrivate(_scene.manager, "LateUpdate");

        Assert.AreEqual(1, _scene.manager.zones.count);
    }

    // Leaving the menu unloads its scene: the backdrop and the meadow built for it go with it
    [Test]
    public void SceneUnloaded_BackdropScene_ReleasesTheBackdropAndItsMeadow()
    {
        Scene scene = SceneManager.GetActiveScene();
        _scene.manager.AttachBackdrop(scene);
        GameObject host = _scene.manager.backdrop.host;

        typeof(RenderManager).GetMethod("OnSceneUnloaded", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(_scene.manager, new object[] { scene });

        Assert.IsFalse(_scene.manager.backdrop.isAttached);
        Assert.IsFalse(host, "The backdrop host is destroyed.");
        Assert.IsNull(_scene.manager.environment);
    }

    // Start loads Main in place of the menu: whichever of load and unload comes first, the game replaces the backdrop
    [Test]
    public void Init_GameAfterBackdrop_ReplacesTheBackdrop()
    {
        _scene.manager.AttachBackdrop(SceneManager.GetActiveScene());
        GameObject host = _scene.manager.backdrop.host;

        _scene.manager.Init(_scene.entityManager, _scene.player);

        Assert.IsFalse(_scene.manager.backdrop.isAttached);
        Assert.IsFalse(host);
        Assert.AreSame(_scene.entityManager, _scene.manager.entityManager);
        Assert.AreEqual(_scene.manager.overviewPose.position, _scene.cameraGo.transform.position);
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
