using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Creatures;
using HealerLike.Render.Stones;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Stage
{

public class RenderManagerTests
{
    static readonly string prefabPath = "Assets/Render/Stage/Prefabs/RenderManager.prefab";

    GameObject _managerGo;
    GameObject _gameGo;
    GameObject _cameraGo;
    GameObject _sunGo;
    GameObject _decorationGo;
    RenderManager _manager;
    EntityManager _entityManager;
    PlayerBehaviour _player;
    Renderer _ground;
    RenderPipelineAsset _previousPipeline;
    Light _previousSun;
    AmbientMode _previousAmbient;

    // His scene as the manager sees it: a main camera, a directional light, the grid and its ground, decoration
    static GridManager CreateGrid(Transform parent, out Renderer ground)
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
        return grid;
    }

    [SetUp]
    public void SetUp()
    {
        _previousPipeline = QualitySettings.renderPipeline;
        _previousSun = RenderSettings.sun;
        _previousAmbient = RenderSettings.ambientMode;

        _managerGo = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));
        _manager = _managerGo.GetComponent<RenderManager>();
        _cameraGo = new GameObject("Main Camera");
        _cameraGo.tag = "MainCamera";
        _cameraGo.AddComponent<Camera>();
        _sunGo = new GameObject("Directional Light");
        _sunGo.AddComponent<Light>().type = LightType.Directional;
        _decorationGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _decorationGo.name = "MiddleLine";

        _gameGo = new GameObject("Game");
        _entityManager = _gameGo.AddComponent<EntityManager>();
        _player = _gameGo.AddComponent<PlayerBehaviour>();
        _player.grid = CreateGrid(_gameGo.transform, out _ground);
    }

    [TearDown]
    public void TearDown()
    {
        // Edit mode runs no OnDestroy, so the buffers and the pipeline are given back here
        _manager.zones.Release();
        _manager.grass.Release();
        QualitySettings.renderPipeline = _previousPipeline;
        RenderSettings.sun = _previousSun;
        RenderSettings.ambientMode = _previousAmbient;
        Object.DestroyImmediate(_managerGo);
        Object.DestroyImmediate(_gameGo);
        Object.DestroyImmediate(_cameraGo);
        Object.DestroyImmediate(_sunGo);
        Object.DestroyImmediate(_decorationGo);
    }

    static Entity CreateEntity(Transform parent, Entity.EntityType entityType, out Renderer modelRenderer)
    {
        GameObject entityGo = new GameObject("Entity");
        entityGo.transform.SetParent(parent, false);
        Entity entity = null;
        TestHelpers.WithLoggingDisabled(() => entity = entityGo.AddComponent<Entity>());
        entity.entityType = entityType;
        GameObject modelGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        modelGo.transform.SetParent(entityGo.transform, false);
        TestHelpers.SetPrivateField(entity, "_model", modelGo.AddComponent<EntityModel>());
        modelRenderer = modelGo.GetComponent<Renderer>();
        return entity;
    }

    [Test]
    public void Init_NullEntityManager_StaysDetached()
    {
        TestHelpers.WithLoggingDisabled(() => _manager.Init(null, null));

        Assert.IsNull(_manager.entityManager);
        Assert.IsNull(_manager.player);
    }

    [Test]
    public void Init_GameScene_AdoptsHisCameraAndSwapsThePipeline()
    {
        RenderPipelineAsset stagePipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(
            "Assets/Render/Stage/Settings/StagePipeline.asset");

        _manager.Init(_entityManager, _player);

        Assert.AreSame(_entityManager, _manager.entityManager);
        Assert.AreSame(_cameraGo.GetComponent<Camera>(), _manager.gameCamera);
        Assert.AreEqual(CameraClearFlags.SolidColor, _manager.gameCamera.clearFlags);
        Assert.AreEqual(_manager.overviewPose.position, _cameraGo.transform.position);
        Assert.AreSame(stagePipeline, QualitySettings.renderPipeline);
    }

    [Test]
    public void Init_GameScene_LightsAndDressesTheBoard()
    {
        _manager.Init(_entityManager, _player);

        Assert.IsFalse(_sunGo.GetComponent<Light>().enabled);
        Assert.AreEqual("KeyLight", RenderSettings.sun.name);
        Assert.AreEqual(AmbientMode.Flat, RenderSettings.ambientMode);
        Assert.AreEqual("HLStageGround", _ground.sharedMaterial.name);
        Assert.IsFalse(_decorationGo.GetComponent<Renderer>().enabled);
        Assert.AreEqual(16f, _manager.board.size.x);
    }

    [Test]
    public void Init_GameScene_BuildsTheEnvironmentAndTheBoardGrass()
    {
        _manager.Init(_entityManager, _player);

        Assert.IsNotNull(_manager.gust);
        Assert.IsNotNull(_manager.foreground);
        Assert.IsNotNull(_manager.zones.buffer);
        Assert.IsNotNull(_manager.registry.spellSink);
    }

    [Test]
    public void Init_SameEntityManagerTwice_AttachesOnce()
    {
        _manager.Init(_entityManager, _player);
        Object gust = _manager.gust;

        _manager.Init(_entityManager, _player);

        Assert.AreSame(gust, _manager.gust);
    }

    [Test]
    public void OnEntitySpawned_Ally_HidesHisModelAndInitsTheView()
    {
        _manager.Init(_entityManager, _player);
        Entity entity = CreateEntity(_gameGo.transform, Entity.EntityType.Player, out Renderer modelRenderer);

        _entityManager.OnEntitySpawned.Invoke(entity);

        Assert.IsFalse(modelRenderer.enabled);
        HLRangePreview preview = entity.model.GetComponentInChildren<HLRangePreview>();
        Assert.IsNotNull(preview);
        Assert.AreSame(entity, preview.entity);
        Assert.IsNotNull(entity.model.GetComponentInChildren<HLCreatureBuilder>().rig);
    }

    [Test]
    public void OnEntitySpawned_Enemy_GetsTheStoneView()
    {
        _manager.Init(_entityManager, _player);
        Entity entity = CreateEntity(_gameGo.transform, Entity.EntityType.Computer, out Renderer modelRenderer);

        _entityManager.OnEntitySpawned.Invoke(entity);

        Assert.IsFalse(modelRenderer.enabled);
        Assert.IsNotNull(entity.model.GetComponentInChildren<HLStoneEnemyVisual>());
        Assert.IsNull(entity.model.GetComponentInChildren<HLRangePreview>());
    }

    [Test]
    public void OnCharacterInit_Character_GetsTheHealerView()
    {
        _manager.Init(_entityManager, _player);
        GameObject characterGo = new GameObject("Character");
        characterGo.transform.SetParent(_gameGo.transform, false);
        Character character = null;
        TestHelpers.WithLoggingDisabled(() => character = characterGo.AddComponent<Character>());

        _player.OnCharacterInit.Invoke(character);

        Assert.IsNotNull(characterGo.GetComponentInChildren<HLCharacterView>());
        Assert.IsNotNull(characterGo.GetComponentInChildren<HLHealPulse>());
    }

    [Test]
    public void LateUpdate_Attached_PublishesTheFrameZones()
    {
        _manager.Init(_entityManager, _player);
        _manager.zones.Add(HLZoneKind.Heal, Vector3.zero, 1f, 1f);

        TestHelpers.InvokePrivate(_manager, "LateUpdate");

        Assert.AreEqual(1, _manager.zones.count);
    }

    [Test]
    public void NextDeliveryToken_Twice_CountsUpFromOne()
    {
        Assert.AreEqual(1, _manager.NextDeliveryToken());
        Assert.AreEqual(2, _manager.NextDeliveryToken());
    }
}
}
