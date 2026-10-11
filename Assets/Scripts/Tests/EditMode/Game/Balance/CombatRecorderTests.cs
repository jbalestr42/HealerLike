using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Game.Balance
{

public class CombatRecorderTests
{
    class FixedConsumer : AConsumer
    {
        readonly float _value;

        public FixedConsumer(float value)
        {
            _value = value;
        }

        public override float GetValue() => _value;
        public override bool ignoreDamageReduction => true;
        public override bool ignoreConsumerPrevention => true;
    }

    readonly List<GameObject> _created = new List<GameObject>();
    readonly List<Object> _createdData = new List<Object>();
    GameObject _characterGo;
    ResourceAttribute _mana;
    GameObject _enemyGo;
    float _time;

    [SetUp]
    public void SetUp()
    {
        _time = 0f;
        _characterGo = Create();
        _mana = TestHelpers.CreateResourceAttribute(_characterGo, AttributeType.ManaMax, 100f);
        _enemyGo = Create();
        TestHelpers.CreateAttributeManager(_enemyGo);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in _created)
        {
            Object.DestroyImmediate(go);
        }
        _created.Clear();
        foreach (Object data in _createdData)
        {
            Object.DestroyImmediate(data);
        }
        _createdData.Clear();
    }

    GameObject Create()
    {
        GameObject go = new GameObject();
        _created.Add(go);
        return go;
    }

    ResourceAttribute CreateHealth(float max)
    {
        return TestHelpers.CreateResourceAttribute(Create(), AttributeType.HealthMax, max);
    }

    CombatRecorder CreateRecorder()
    {
        return new CombatRecorder(new CombatStats { wave = "Wave_Test" }, () => _time, _mana);
    }

    // Applies the value to the resource as if it came from source, like a skill or an attack would
    static void Apply(ResourceAttribute resource, GameObject source, float value)
    {
        ResourceModifier modifier = new ResourceModifier { source = source };
        modifier.consumers.Add(new FixedConsumer(value));
        resource.AddResourceModifier(modifier);
        TestHelpers.InvokePrivate(resource, "Update");
    }

    [Test]
    public void Constructor_KeepsTheContextAndTheStartingMana()
    {
        _time = 5f;
        Apply(_mana, _characterGo, -30f);

        CombatStats stats = CreateRecorder().stats;

        Assert.AreEqual("Wave_Test", stats.wave);
        Assert.AreEqual(70f, stats.manaStart, 0.001f);
        Assert.AreEqual(100f, stats.manaMax, 0.001f);
    }

    [Test]
    public void ManaSpentAndGained_DuringTheFight_AreRecorded()
    {
        CombatRecorder recorder = CreateRecorder();

        Apply(_mana, _characterGo, -40f);
        Apply(_mana, _characterGo, 10f);
        Apply(_mana, _characterGo, -25f);
        CombatStats stats = recorder.Stop(true);

        Assert.AreEqual(65f, stats.manaSpent, 0.001f);
        Assert.AreEqual(10f, stats.manaGained, 0.001f);
        Assert.AreEqual(45f, stats.manaEnd, 0.001f);
    }

    [Test]
    public void AllyHealth_DamageAndHeals_SplitByWhoHealed()
    {
        CombatRecorder recorder = CreateRecorder();
        ResourceAttribute knight = CreateHealth(100f);
        GameObject shaman = Create();
        TestHelpers.CreateAttributeManager(shaman);
        recorder.AddAlly("Knight", knight);

        Apply(knight, _enemyGo, -60f);
        // 70 healed by the character, 10 of them above the max
        Apply(knight, _characterGo, 20f);
        Apply(knight, _characterGo, 50f);
        // Already full: all lost
        Apply(knight, shaman, 5f);
        CombatStats stats = recorder.Stop(true);

        Assert.AreEqual(60f, stats.allyDamageTaken, 0.001f);
        Assert.AreEqual(60f, stats.characterHeal, 0.001f);
        Assert.AreEqual(10f, stats.characterOverheal, 0.001f);
        Assert.AreEqual(0f, stats.otherHeal, 0.001f);
        Assert.AreEqual(5f, stats.otherOverheal, 0.001f);
    }

    [Test]
    public void AllyHealth_KeptFromTheStartToTheEnd()
    {
        ResourceAttribute knight = CreateHealth(100f);
        ResourceAttribute archer = CreateHealth(50f);
        // Wounded in a previous fight
        Apply(knight, _enemyGo, -30f);
        CombatRecorder recorder = CreateRecorder();
        recorder.AddAlly("Knight", knight);
        recorder.AddAlly("Archer", archer);

        Apply(archer, _enemyGo, -20f);
        CombatStats stats = recorder.Stop(true);

        Assert.AreEqual(150f, stats.allyHealthMax, 0.001f);
        Assert.AreEqual(120f, stats.allyHealthStart, 0.001f);
        Assert.AreEqual(100f, stats.allyHealthEnd, 0.001f);
    }

    [Test]
    public void AllyDestroyedDuringTheFight_CountsForNoHealthAtTheEnd()
    {
        ResourceAttribute knight = CreateHealth(100f);
        GameObject archerGo = Create();
        ResourceAttribute archer = TestHelpers.CreateResourceAttribute(archerGo, AttributeType.HealthMax, 50f);
        CombatRecorder recorder = CreateRecorder();
        recorder.AddAlly("Knight", knight);
        recorder.AddAlly("Archer", archer);

        Object.DestroyImmediate(archerGo);
        recorder.RecordAllyDeath("Archer");
        CombatStats stats = recorder.Stop(false);

        Assert.AreEqual(100f, stats.allyHealthEnd, 0.001f);
        CollectionAssert.AreEqual(new[] { "Archer" }, stats.deadAllies);
        Assert.IsFalse(stats.won);
    }

    [Test]
    public void AddAlly_SameUnitTwice_CountedOnce()
    {
        CombatRecorder recorder = CreateRecorder();
        ResourceAttribute knight = CreateHealth(100f);
        recorder.AddAlly("Knight", knight);
        recorder.AddAlly("Knight", knight);

        Apply(knight, _enemyGo, -10f);
        CombatStats stats = recorder.Stop(true);

        Assert.AreEqual(1, stats.allies.Count);
        Assert.AreEqual(10f, stats.allyDamageTaken, 0.001f);
    }

    [Test]
    public void Enemies_TheirHealthAndTheDamageTheyTook()
    {
        CombatRecorder recorder = CreateRecorder();
        ResourceAttribute skeleton = CreateHealth(80f);
        recorder.AddEnemy(skeleton);

        Apply(skeleton, _characterGo, -30f);
        CombatStats stats = recorder.Stop(true);

        Assert.AreEqual(80f, stats.enemyHealthMax, 0.001f);
        Assert.AreEqual(30f, stats.enemyDamageTaken, 0.001f);
        Assert.AreEqual(0f, stats.allyDamageTaken, 0.001f);
    }

    [Test]
    public void Duration_AndPeakDamage_FollowTheClock()
    {
        _time = 10f;
        CombatRecorder recorder = CreateRecorder();
        ResourceAttribute knight = CreateHealth(200f);
        recorder.AddAlly("Knight", knight);

        _time = 11f;
        Apply(knight, _enemyGo, -20f);
        _time = 12f;
        Apply(knight, _enemyGo, -30f);
        _time = 20f;
        Apply(knight, _enemyGo, -10f);
        _time = 25f;
        CombatStats stats = recorder.Stop(true);

        Assert.AreEqual(15f, stats.duration, 0.001f);
        Assert.AreEqual(50f, stats.allyPeakDamage, 0.001f);
    }

    [Test]
    public void Stop_NothingRecordedAfterwards()
    {
        CombatRecorder recorder = CreateRecorder();
        ResourceAttribute knight = CreateHealth(100f);
        recorder.AddAlly("Knight", knight);
        CombatStats stats = recorder.Stop(true);

        Apply(knight, _enemyGo, -10f);
        Apply(_mana, _characterGo, -10f);

        Assert.AreEqual(0f, stats.allyDamageTaken, 0.001f);
        Assert.AreEqual(0f, stats.manaSpent, 0.001f);
    }

    // A unit of the side, holding the health, with or without its data
    Entity CreateEntity(Entity.EntityType side, ResourceAttribute health, string title = null)
    {
        GameObject go = Create();
        go.name = "Unit (Entity)";
        Entity entity = null;
        TestHelpers.WithLoggingDisabled(() => entity = go.AddComponent<Entity>());
        TestHelpers.SetPrivateField(entity, "_health", health);
        entity.entityType = side;
        if (title != null)
        {
            EntityData data = ScriptableObject.CreateInstance<EntityData>();
            data.title = title;
            _createdData.Add(data);
            entity.data = data;
        }
        return entity;
    }

    [Test]
    public void AddUnit_ByItsSide()
    {
        CombatRecorder recorder = CreateRecorder();
        ResourceAttribute knightHealth = CreateHealth(100f);
        ResourceAttribute skeletonHealth = CreateHealth(80f);

        recorder.AddUnit(CreateEntity(Entity.EntityType.Player, knightHealth, "Knight"));
        recorder.AddUnit(CreateEntity(Entity.EntityType.Computer, skeletonHealth, "Skeleton"));
        Apply(knightHealth, _enemyGo, -10f);
        Apply(skeletonHealth, _characterGo, -20f);
        CombatStats stats = recorder.Stop(true);

        CollectionAssert.AreEqual(new[] { "Knight" }, stats.allies);
        Assert.AreEqual(10f, stats.allyDamageTaken, 0.001f);
        Assert.AreEqual(80f, stats.enemyHealthMax, 0.001f);
        Assert.AreEqual(20f, stats.enemyDamageTaken, 0.001f);
    }

    [Test]
    public void AddUnit_NoUnitOrNoHealth_Ignored()
    {
        CombatRecorder recorder = CreateRecorder();

        recorder.AddUnit(null);
        recorder.AddUnit(CreateEntity(Entity.EntityType.Player, null, "Ghost"));
        CombatStats stats = recorder.Stop(true);

        Assert.IsEmpty(stats.allies);
    }

    [Test]
    public void RecordDeath_OnlyTheAllies()
    {
        CombatRecorder recorder = CreateRecorder();

        recorder.RecordDeath(CreateEntity(Entity.EntityType.Player, CreateHealth(100f), "Knight"));
        recorder.RecordDeath(CreateEntity(Entity.EntityType.Computer, CreateHealth(100f), "Skeleton"));
        recorder.RecordDeath(null);
        CombatStats stats = recorder.Stop(false);

        CollectionAssert.AreEqual(new[] { "Knight" }, stats.deadAllies);
    }

    [Test]
    public void GetUnitName_TheTitleOfItsData_OrItsName()
    {
        Assert.AreEqual("Knight", CombatRecorder.GetUnitName(CreateEntity(Entity.EntityType.Player, null, "Knight")));
        Assert.AreEqual("Unit (Entity)", CombatRecorder.GetUnitName(CreateEntity(Entity.EntityType.Player, null)));
    }
}

}
