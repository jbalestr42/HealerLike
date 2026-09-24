using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Skills
{

public class AreaOfEffectSkillTests
{
    GameObject _entityManagerGo;
    EntityManager _entityManager;
    GameObject _areaOfEffectPrefab;
    GameObject _source;
    AreaOfEffectSkill _skill;

    [SetUp]
    public void SetUp()
    {
        _entityManagerGo = new GameObject("EntityManager");
        _entityManager = _entityManagerGo.AddComponent<EntityManager>();
        TestHelpers.InvokePrivate(_entityManager, "Awake");
        SetEntityManagerInstance(_entityManager);

        _areaOfEffectPrefab = new GameObject("AreaOfEffect");
        _areaOfEffectPrefab.AddComponent<AreaOfEffect>();

        _source = new GameObject("Source");
        _skill = _source.AddComponent<AreaOfEffectSkill>();
        _skill.data = new AreaOfEffectSkillData { areaOfEffectPrefab = _areaOfEffectPrefab };
        // Start() would pull the range from a full AttributeManager and add a TargetRequirement
        TestHelpers.SetPrivateField(_skill, "_range", new Attribute(3f));
    }

    [TearDown]
    public void TearDown()
    {
        SetEntityManagerInstance(null);
        Object.DestroyImmediate(_entityManagerGo);
        Object.DestroyImmediate(_areaOfEffectPrefab);
        Object.DestroyImmediate(_source);
    }

    // Execute reaches the manager through EntityManager.instance. Seeding the singleton with the
    // test's own manager keeps the getter from searching the open scene or creating a
    // DontDestroyOnLoad object.
    static void SetEntityManagerInstance(EntityManager entityManager)
    {
        FieldInfo field = typeof(Singleton<EntityManager>).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
        field.SetValue(null, entityManager);
    }

    [Test]
    public void Execute_SpawnsAreaOfEffectThroughEntityManager()
    {
        GameObject spawnedGo = null;
        _entityManager.OnProjectileSpawned.AddListener(go => spawnedGo = go);

        _skill.Execute(_source);

        Assert.IsNotNull(spawnedGo);
        AreaOfEffect areaOfEffect = spawnedGo.GetComponent<AreaOfEffect>();
        Assert.IsNotNull(areaOfEffect);
        Assert.AreSame(_source, areaOfEffect.source);
        Assert.AreEqual(3f, areaOfEffect.radius);
    }

    [Test]
    public void Execute_ParentsAreaOfEffectUnderProjectilePool()
    {
        GameObject spawnedGo = null;
        _entityManager.OnProjectileSpawned.AddListener(go => spawnedGo = go);

        _skill.Execute(_source);

        Assert.AreSame(_entityManager.transform.Find("Projectiles"), spawnedGo.transform.parent);
    }
}

}
