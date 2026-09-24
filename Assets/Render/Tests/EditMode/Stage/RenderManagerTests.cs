using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Deliveries;
using HealerLike.Render.Spells;
using HealerLike.Render.Stones;
using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Stage
{

public class RenderManagerTests
{
    static readonly string prefabPath = "Assets/Render/Stage/Prefabs/RenderManager.prefab";
    static readonly string projectileFolder = "Assets/Prefabs/Projectiles/";

    readonly List<GameObject> _spawned = new List<GameObject>();

    GameObject _managerGo;
    GameObject _gameGo;
    GameObject _cameraGo;
    GameObject _createdCameraGo;
    GameObject _sunGo;
    GameObject _decorationGo;
    GameObject _otherDecorationGo;
    GameObject _farGroundGo;
    RenderManager _manager;
    EntityManager _entityManager;
    PlayerBehaviour _player;
    Renderer _ground;
    RenderPipelineAsset _previousPipeline;
    Light _previousSun;
    AmbientMode _previousAmbient;

    // The game scene as the manager sees it: a main camera, a directional light, the grid and its ground, decoration
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
        TestHelpers.SetPrivateField(grid, "_ground", groundGo);
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
        // The test scene may already hold a main camera, the manager adopts whichever Camera.main returns
        _createdCameraGo = new GameObject("Main Camera");
        _createdCameraGo.tag = "MainCamera";
        _createdCameraGo.AddComponent<Camera>();
        _cameraGo = Camera.main.gameObject;
        _sunGo = new GameObject("Directional Light");
        _sunGo.AddComponent<Light>().type = LightType.Directional;
        _decorationGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _decorationGo.name = "MiddleLine";
        _otherDecorationGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _otherDecorationGo.name = "Sphere";
        _farGroundGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
        _farGroundGo.name = "Ground";

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
        foreach (GameObject spawnedGo in _spawned)
        {
            Object.DestroyImmediate(spawnedGo);
        }
        _spawned.Clear();
        Object.DestroyImmediate(_managerGo);
        Object.DestroyImmediate(_gameGo);
        Object.DestroyImmediate(_createdCameraGo);
        Object.DestroyImmediate(_otherDecorationGo);
        Object.DestroyImmediate(_farGroundGo);
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

    // A projectile as EntityManager.SpawnProjectile hands it out, before Projectile.Init
    Projectile SpawnProjectile(string prefabName)
    {
        GameObject projectileGo = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(projectileFolder + prefabName + ".prefab"));
        _spawned.Add(projectileGo);
        return projectileGo.GetComponent<Projectile>();
    }

    [Test]
    public void Init_NullEntityManager_StaysDetached()
    {
        TestHelpers.WithLoggingDisabled(() => _manager.Init(null, null));

        Assert.IsNull(_manager.entityManager);
        Assert.IsNull(_manager.player);
    }

    [Test]
    public void Init_GameScene_AdoptsTheGameCameraAndSwapsThePipeline()
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
        Assert.AreEqual("StageGround", _ground.sharedMaterial.name);
        Assert.IsFalse(_decorationGo.GetComponent<Renderer>().enabled);
        Assert.IsFalse(_farGroundGo.GetComponent<Renderer>().enabled);
        Assert.IsTrue(_ground.enabled); // the board ground shares the name and stays
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
    public void OnEntitySpawned_Ally_HidesTheGameModelAndInitsTheView()
    {
        _manager.Init(_entityManager, _player);
        Entity entity = CreateEntity(_gameGo.transform, Entity.EntityType.Player, out Renderer modelRenderer);
        entity.data = AssetDatabase.LoadAssetAtPath<EntityData>("Assets/Data/Entities/NormalEntity/NormalEntity.asset");

        _entityManager.OnEntitySpawned.Invoke(entity);

        Assert.IsFalse(modelRenderer.enabled);
        RangePreview preview = entity.model.GetComponentInChildren<RangePreview>();
        Assert.IsNotNull(preview);
        Assert.AreSame(entity, preview.entity);
        Assert.IsNotNull(entity.model.GetComponentInChildren<CreatureBuilder>().rig);
    }

    [Test]
    public void OnEntitySpawned_ModelWithTheHUD_KeepsTheEffectIconsShowing()
    {
        _manager.Init(_entityManager, _player);
        Entity entity = CreateEntity(_gameGo.transform, Entity.EntityType.Player, out Renderer modelRenderer);
        entity.data = AssetDatabase.LoadAssetAtPath<EntityData>("Assets/Data/Entities/NormalEntity/NormalEntity.asset");
        GameObject hudGo = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EntityHUD.prefab"),
                                              entity.model.transform);
        BuffIconBar bar = hudGo.GetComponentInChildren<BuffIconBar>(true);
        Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/BuffIcon.prefab"), bar.transform);

        _entityManager.OnEntitySpawned.Invoke(entity);

        Assert.IsFalse(modelRenderer.enabled);
        Assert.IsTrue(hudGo.GetComponentInChildren<Canvas>(true).enabled);
        Assert.AreEqual(0, bar.GetComponentsInChildren<Renderer>(true).Length); // the icons draw through the canvas
        foreach (Behaviour behaviour in bar.GetComponentsInChildren<Behaviour>(true))
        {
            Assert.IsTrue(behaviour.enabled, behaviour.GetType().Name);
        }
    }

    [Test]
    public void OnEntitySpawned_Enemy_GetsTheStoneHost()
    {
        _manager.Init(_entityManager, _player);
        Entity entity = CreateEntity(_gameGo.transform, Entity.EntityType.Computer, out Renderer modelRenderer);

        _entityManager.OnEntitySpawned.Invoke(entity);

        Assert.IsFalse(modelRenderer.enabled);
        Assert.IsNotNull(entity.model.GetComponentInChildren<CreatureBuilder>());
        Assert.IsNotNull(entity.model.GetComponentInChildren<BruiseZone>());
        Assert.IsNull(entity.model.GetComponentInChildren<RangePreview>());
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

        Assert.IsNotNull(characterGo.GetComponentInChildren<CharacterView>());
        Assert.IsNotNull(characterGo.GetComponentInChildren<HealPulse>());
    }

    [Test]
    public void LateUpdate_Attached_PublishesTheFrameZones()
    {
        _manager.Init(_entityManager, _player);
        _manager.zones.Add(ZoneKind.Heal, Vector3.zero, 1f, 1f);

        TestHelpers.InvokePrivate(_manager, "LateUpdate");

        Assert.AreEqual(1, _manager.zones.count);
    }

    [Test]
    public void NextDeliveryToken_Twice_CountsUpFromOne()
    {
        Assert.AreEqual(1, _manager.NextDeliveryToken());
        Assert.AreEqual(2, _manager.NextDeliveryToken());
    }

    [TestCase("Assets/Data/Entities/SoldierEntity/SoldierEntity.asset")]
    [TestCase("Assets/Data/Entities/HitArmorBufferEntityEntity/HitArmorBufferEntity.asset")]
    public void OnEntitySpawned_StoneEnemy_GetsADerivedStone(string dataPath)
    {
        _manager.Init(_entityManager, _player);
        Entity entity = CreateEntity(_gameGo.transform, Entity.EntityType.Computer, out Renderer modelRenderer);
        entity.data = AssetDatabase.LoadAssetAtPath<EntityData>(dataPath);

        _entityManager.OnEntitySpawned.Invoke(entity);

        Assert.IsNotNull(entity.data);
        CreatureBuilder builder = entity.model.GetComponentInChildren<CreatureBuilder>();
        Assert.IsNotNull(builder, "The derived stone carries a CreatureBuilder.");
        Assert.IsNotNull(builder.rig);
        Assert.AreEqual(Primitive.Stone, builder.recipe.parts[0].primitive);
    }

    [Test]
    public void OnProjectileSpawned_Bullet_AttachesTheDeliveryVisuals()
    {
        _manager.Init(_entityManager, _player);
        Projectile projectile = SpawnProjectile("BulletSpeed");

        _entityManager.OnProjectileSpawned.Invoke(projectile);

        ProjectileVisualObserver observer = projectile.GetComponent<ProjectileVisualObserver>();
        Assert.IsNotNull(observer);
        Assert.AreEqual(DeliveryStyle.Direct, observer.deliveryStyle); // Homing at speed 15
        Assert.IsNotNull(projectile.GetComponent<StoneProjectileImpactBridge>());
        Assert.IsNotNull(projectile.GetComponent<LaunchWave>());
        Assert.IsNotNull(projectile.GetComponent<StageLaunchGust>());
        Assert.IsNull(projectile.GetComponent<ChainContactVisual>());
    }

    [Test]
    public void OnProjectileSpawned_Chain_AddsTheContactVisualAndHidesItsLine()
    {
        _manager.Init(_entityManager, _player);
        Projectile projectile = SpawnProjectile("ChainLightning");

        _entityManager.OnProjectileSpawned.Invoke(projectile);

        Assert.AreEqual(DeliveryStyle.ChainSync, projectile.GetComponent<ProjectileVisualObserver>().deliveryStyle);
        Assert.IsNotNull(projectile.GetComponent<ChainContactVisual>());
        foreach (LineRenderer line in projectile.GetComponentsInChildren<LineRenderer>(true))
        {
            Assert.IsFalse(line.enabled);
        }
    }

    [Test]
    public void OnProjectileSpawned_NotAttached_AddsNothing()
    {
        Projectile projectile = SpawnProjectile("BulletSpeed");

        _entityManager.OnProjectileSpawned.Invoke(projectile);

        Assert.IsNull(projectile.GetComponent<ProjectileVisualObserver>());
    }

    [Test]
    public void OnAreaOfEffectStarted_Area_AttachesThePulseAndMasksItsOwnVisual()
    {
        _manager.Init(_entityManager, _player);
        GameObject areaGo = new GameObject("Area");
        _spawned.Add(areaGo);
        AreaOfEffect area = areaGo.AddComponent<AreaOfEffect>();

        _entityManager.OnAreaOfEffectStarted.Invoke(area);

        Assert.IsNotNull(areaGo.GetComponent<AreaPulse>());
        Assert.IsNotNull(areaGo.GetComponent<LegacyAreaVisualMask>());
    }
}

}
