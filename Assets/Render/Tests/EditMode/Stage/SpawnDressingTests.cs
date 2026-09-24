using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Deliveries;
using HealerLike.Render.Spells;
using HealerLike.Render.Stones;
using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stage
{

public class SpawnDressingTests
{
    static readonly string projectileFolder = "Assets/Prefabs/Projectiles/";

    readonly List<GameObject> _spawned = new List<GameObject>();
    readonly List<Object> _created = new List<Object>();
    StageSceneFixture _scene;

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
        GameObject projectileGo =
            Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(projectileFolder + prefabName + ".prefab"));
        _spawned.Add(projectileGo);
        return projectileGo.GetComponent<Projectile>();
    }

    // An area as EntityManager.SpawnProjectile hands it out, before its caller sets the source and radius
    AreaOfEffect SpawnArea()
    {
        GameObject areaGo = new GameObject("Area");
        _spawned.Add(areaGo);
        return areaGo.AddComponent<AreaOfEffect>();
    }

    // What the caller then sets: a source entity whose one on hit consumer takes this value, and the radius
    void ConfigureArea(AreaOfEffect area, float consumerValue)
    {
        GameObject sourceGo = new GameObject("Source");
        _spawned.Add(sourceGo);
        Entity source = null;
        TestHelpers.WithLoggingDisabled(() => source = sourceGo.AddComponent<Entity>());
        ConsumerFactory consumer = ScriptableObject.CreateInstance<ConsumerFactory>();
        _created.Add(consumer);
        FlatValue value = new FlatValue();
        value.data = new FlatValueData { value = consumerValue };
        consumer.data = new ConsumerData { value = value };
        source.AddOnHitConsumer(consumer);

        area.source = sourceGo;
        area.radius = 2.5f;
    }

    [SetUp]
    public void SetUp()
    {
        _scene = new StageSceneFixture();
        _scene.Create();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject spawnedGo in _spawned)
        {
            Object.DestroyImmediate(spawnedGo);
        }

        _spawned.Clear();
        foreach (Object created in _created)
        {
            Object.DestroyImmediate(created);
        }

        _created.Clear();
        _scene.Destroy();
    }

    [Test]
    public void OnEntitySpawned_Ally_HidesTheGameModelAndInitsTheView()
    {
        _scene.manager.Init(_scene.entityManager, _scene.player);
        Entity entity = CreateEntity(_scene.gameGo.transform, Entity.EntityType.Player, out Renderer modelRenderer);
        entity.data = AssetDatabase.LoadAssetAtPath<EntityData>("Assets/Data/Entities/NormalEntity/NormalEntity.asset");

        _scene.entityManager.OnEntitySpawned.Invoke(entity);

        Assert.IsFalse(modelRenderer.enabled);
        RangePreview preview = entity.model.GetComponentInChildren<RangePreview>();
        Assert.IsNotNull(preview);
        Assert.AreSame(entity, preview.entity);
        Assert.IsNotNull(entity.model.GetComponentInChildren<CreatureBuilder>().rig);
    }

    [Test]
    public void OnEntitySpawned_ModelWithTheHUD_KeepsTheEffectIconsShowing()
    {
        _scene.manager.Init(_scene.entityManager, _scene.player);
        Entity entity = CreateEntity(_scene.gameGo.transform, Entity.EntityType.Player, out Renderer modelRenderer);
        entity.data = AssetDatabase.LoadAssetAtPath<EntityData>("Assets/Data/Entities/NormalEntity/NormalEntity.asset");
        GameObject hudGo = Object.Instantiate(
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EntityHUD.prefab"), entity.model.transform);
        BuffIconBar bar = hudGo.GetComponentInChildren<BuffIconBar>(true);
        Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/BuffIcon.prefab"),
            bar.transform);

        _scene.entityManager.OnEntitySpawned.Invoke(entity);

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
        _scene.manager.Init(_scene.entityManager, _scene.player);
        Entity entity = CreateEntity(_scene.gameGo.transform, Entity.EntityType.Computer, out Renderer modelRenderer);

        _scene.entityManager.OnEntitySpawned.Invoke(entity);

        Assert.IsFalse(modelRenderer.enabled);
        Assert.IsNotNull(entity.model.GetComponentInChildren<CreatureBuilder>());
        Assert.IsNotNull(entity.model.GetComponentInChildren<BruiseZone>());
        Assert.IsNull(entity.model.GetComponentInChildren<RangePreview>());
    }

    [TestCase("Assets/Data/Entities/SoldierEntity/SoldierEntity.asset")]
    [TestCase("Assets/Data/Entities/HitArmorBufferEntityEntity/HitArmorBufferEntity.asset")]
    public void OnEntitySpawned_StoneEnemy_GetsADerivedStone(string dataPath)
    {
        _scene.manager.Init(_scene.entityManager, _scene.player);
        Entity entity = CreateEntity(_scene.gameGo.transform, Entity.EntityType.Computer, out Renderer modelRenderer);
        entity.data = AssetDatabase.LoadAssetAtPath<EntityData>(dataPath);

        _scene.entityManager.OnEntitySpawned.Invoke(entity);

        Assert.IsNotNull(entity.data);
        CreatureBuilder builder = entity.model.GetComponentInChildren<CreatureBuilder>();
        Assert.IsNotNull(builder, "The derived stone carries a CreatureBuilder.");
        Assert.IsNotNull(builder.rig);
        Assert.AreEqual(Primitive.Stone, builder.recipe.parts[0].primitive);
    }

    [Test]
    public void OnCharacterInit_Character_GetsTheHealerView()
    {
        _scene.manager.Init(_scene.entityManager, _scene.player);
        GameObject characterGo = new GameObject("Character");
        characterGo.transform.SetParent(_scene.gameGo.transform, false);
        Character character = null;
        TestHelpers.WithLoggingDisabled(() => character = characterGo.AddComponent<Character>());

        _scene.player.OnCharacterInit.Invoke(character);

        Assert.IsNotNull(characterGo.GetComponentInChildren<CharacterView>());
        Assert.IsNotNull(characterGo.GetComponentInChildren<HealPulse>());
    }

    [Test]
    public void OnProjectileSpawned_Bullet_AttachesTheDeliveryVisuals()
    {
        _scene.manager.Init(_scene.entityManager, _scene.player);
        Projectile projectile = SpawnProjectile("BulletSpeed");

        _scene.entityManager.OnProjectileSpawned.Invoke(projectile.gameObject);

        ProjectileVisualObserver observer = projectile.GetComponent<ProjectileVisualObserver>();
        Assert.IsNotNull(observer);
        Assert.AreEqual(DeliveryStyle.Direct, observer.deliveryStyle); // Homing at speed 15
        Assert.IsNotNull(projectile.GetComponent<StoneProjectileImpactBridge>());
        Assert.IsNotNull(projectile.GetComponent<LaunchWave>());
    }

    [Test]
    public void OnProjectileSpawned_Chain_KeepsItsContactPathAndHidesItsLine()
    {
        _scene.manager.Init(_scene.entityManager, _scene.player);
        Projectile projectile = SpawnProjectile("ChainLightning");

        _scene.entityManager.OnProjectileSpawned.Invoke(projectile.gameObject);

        Assert.AreEqual(DeliveryStyle.ChainSync, projectile.GetComponent<ProjectileVisualObserver>().deliveryStyle);
        foreach (LineRenderer line in projectile.GetComponentsInChildren<LineRenderer>(true))
        {
            Assert.IsFalse(line.enabled);
        }
    }

    [Test]
    public void OnProjectileSpawned_NotAttached_AddsNothing()
    {
        Projectile projectile = SpawnProjectile("BulletSpeed");

        _scene.entityManager.OnProjectileSpawned.Invoke(projectile.gameObject);

        Assert.IsNull(projectile.GetComponent<ProjectileVisualObserver>());
    }

    [Test]
    public void OnProjectileSpawned_NeitherProjectileNorArea_AddsNothing()
    {
        _scene.manager.Init(_scene.entityManager, _scene.player);
        GameObject spawnedGo = new GameObject("Spawned");
        _spawned.Add(spawnedGo);

        _scene.entityManager.OnProjectileSpawned.Invoke(spawnedGo);

        Assert.IsNull(spawnedGo.GetComponent<AreaPulseOnStart>());
        Assert.IsNull(spawnedGo.GetComponent<LegacyAreaVisualMask>());
        Assert.IsNull(spawnedGo.GetComponent<ProjectileVisualObserver>());
    }

    [TestCase(-5f, ZoneKind.Heal, EffectElement.Ring)]
    [TestCase(5f, ZoneKind.Hostile, EffectElement.Litter)]
    public void OnProjectileSpawned_Area_PulsesItsKindOnceAtStartAndMasksItsOwnVisual(float consumerValue,
        ZoneKind kind, EffectElement element)
    {
        _scene.manager.Init(_scene.entityManager, _scene.player);
        AreaOfEffect area = SpawnArea();

        _scene.entityManager.OnProjectileSpawned.Invoke(area.gameObject);
        ConfigureArea(area, consumerValue);

        Assert.AreEqual(0, _scene.manager.spellSink.GetComponentsInChildren<SpellEffect>().Length);
        TestHelpers.InvokePrivate(area.GetComponent<AreaPulseOnStart>(), "Start");
        _scene.manager.zones.PublishFrame(0f);

        SpellEffect[] effects = _scene.manager.spellSink.GetComponentsInChildren<SpellEffect>();
        Assert.AreEqual(1, effects.Length);
        Assert.AreEqual(element, effects[0].element);
        Assert.AreEqual(1, _scene.manager.zones.count);
        Assert.AreEqual((int)kind, _scene.manager.zones.snapshot[0].kind);
        Assert.AreEqual(2.5f, _scene.manager.zones.snapshot[0].radius);
        Assert.IsNotNull(area.GetComponent<LegacyAreaVisualMask>());
    }
}

}
