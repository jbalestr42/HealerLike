using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Skills
{

// The allies come from the EntityManager singleton in the game
public class TestPurifySkill : PurifySkill
{
    public List<GameObject> allies = new List<GameObject>();

    protected override List<GameObject> GetAllies() => allies;
}

// Purifier: removes one debuff from a random debuffed ally, then gives a random buff of its list to a random ally
public class PurifySkillTests
{
    readonly TestUnits _units = new TestUnits();
    readonly List<Object> _scriptableObjects = new List<Object>();
    readonly List<GameObject> _enemies = new List<GameObject>();
    GameplayTag _debuffTag;
    Entity _purifier;
    Entity _ally;
    TestPurifySkill _skill;

    [SetUp]
    public void SetUp()
    {
        _debuffTag = Track(ScriptableObject.CreateInstance<GameplayTag>());
        _debuffTag.name = TagNames.Debuff;
        _purifier = CreateUnit("Purifier", 100f);
        _ally = CreateUnit("Ally", 100f);
        _skill = _purifier.gameObject.AddComponent<TestPurifySkill>();
        _skill.data = new PurifySkillData { debuffTag = _debuffTag, onSkillTriggerFactory = new List<AOnSkillTriggerFactory>() };
        _skill.allies = new List<GameObject> { _purifier.gameObject, _ally.gameObject };
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        foreach (GameObject enemy in _enemies)
        {
            Object.DestroyImmediate(enemy);
        }
        _enemies.Clear();
        foreach (Object scriptableObject in _scriptableObjects)
        {
            Object.DestroyImmediate(scriptableObject);
        }
        _scriptableObjects.Clear();
    }

    T Track<T>(T scriptableObject) where T : Object
    {
        _scriptableObjects.Add(scriptableObject);
        return scriptableObject;
    }

    Entity CreateUnit(string name, float health)
    {
        Entity entity = _units.Create(health, 100f, name);
        entity.GetComponent<BuffManager>().isEnabled = true;
        return entity;
    }

    ABuffHandlerFactory CreateHandler(string name, params GameplayTag[] tags)
    {
        BuffHandlerFactory handler = Track(ScriptableObject.CreateInstance<BuffHandlerFactory>());
        handler.name = name;
        handler.data = new BuffHandlerData
        {
            durationType = DurationType.Infinite,
            buffFactoryList = new List<ABuffFactory>(),
            tags = new List<GameplayTag>(tags),
        };
        return handler;
    }

    // Applied by an enemy, as the game does
    void Apply(Entity entity, ABuffHandlerFactory handler)
    {
        GameObject enemy = new GameObject("Enemy " + handler.name);
        _enemies.Add(enemy);
        BuffManager buffManager = entity.GetComponent<BuffManager>();
        buffManager.AddHandler(handler, enemy, entity.gameObject);
        buffManager.ForceUpdate();
    }

    static List<ABuffHandlerFactory> GetHandlers(Entity entity)
    {
        BuffManager buffManager = entity.GetComponent<BuffManager>();
        buffManager.ForceUpdate();
        return buffManager.GetActiveHandlers().ConvertAll(handler => handler.buffHandlerFactory);
    }

    [Test]
    public void RemoveOneDebuff_RemovesTheDebuffOfTheDebuffedAlly()
    {
        ABuffHandlerFactory poison = CreateHandler("Poison", _debuffTag);
        Apply(_ally, poison);

        Assert.IsTrue(_skill.RemoveOneDebuff(new List<Entity> { _purifier, _ally }));

        CollectionAssert.DoesNotContain(GetHandlers(_ally), poison);
    }

    [Test]
    public void RemoveOneDebuff_OnlyOneOfThem()
    {
        ABuffHandlerFactory poison = CreateHandler("Poison", _debuffTag);
        ABuffHandlerFactory curse = CreateHandler("Curse", _debuffTag);
        Apply(_ally, poison);
        Apply(_ally, curse);

        _skill.RemoveOneDebuff(new List<Entity> { _purifier, _ally });

        Assert.AreEqual(1, GetHandlers(_ally).FindAll(handler => handler == poison || handler == curse).Count);
    }

    // A descendant of the Debuff tag is a debuff too
    [Test]
    public void RemoveOneDebuff_ADescendantTagIsADebuff()
    {
        GameplayTag poisonTag = Track(ScriptableObject.CreateInstance<GameplayTag>());
        TestHelpers.SetPrivateField(poisonTag, "_parent", _debuffTag);
        ABuffHandlerFactory poison = CreateHandler("Poison", poisonTag);
        Apply(_ally, poison);

        Assert.IsTrue(_skill.RemoveOneDebuff(new List<Entity> { _ally }));

        CollectionAssert.DoesNotContain(GetHandlers(_ally), poison);
    }

    [Test]
    public void RemoveOneDebuff_KeepsTheOtherBuffs()
    {
        ABuffHandlerFactory shield = CreateHandler("Shield");
        Apply(_ally, shield);

        Assert.IsFalse(_skill.RemoveOneDebuff(new List<Entity> { _purifier, _ally }));

        CollectionAssert.Contains(GetHandlers(_ally), shield);
    }

    [Test]
    public void GiveRandomBuff_GivesABuffOfTheListToAnAlly_FromThePurifier()
    {
        ABuffHandlerFactory blessing = CreateHandler("Blessing");
        _skill.data.buffHandlerFactories = new List<ABuffHandlerFactory> { blessing };

        Assert.AreSame(blessing, _skill.GiveRandomBuff(new List<Entity> { _ally }));

        GetHandlers(_ally);
        BuffManager.BuffHandlerData given = _ally.GetComponent<BuffManager>().GetActiveHandlers().Find(handler => handler.buffHandlerFactory == blessing);
        Assert.IsNotNull(given);
        Assert.AreSame(_purifier.gameObject, given.source);
    }

    // Attack speed, damage or armor: each one comes up
    [Test]
    public void GiveRandomBuff_EveryBuffOfTheListCanBeGiven()
    {
        List<ABuffHandlerFactory> buffs = new List<ABuffHandlerFactory> { CreateHandler("Speed"), CreateHandler("Damage"), CreateHandler("Armor") };
        _skill.data.buffHandlerFactories = buffs;
        Random.InitState(1);

        HashSet<ABuffHandlerFactory> given = new HashSet<ABuffHandlerFactory>();
        for (int i = 0; i < 60; i++)
        {
            given.Add(_skill.GiveRandomBuff(new List<Entity> { _ally }));
        }

        CollectionAssert.AreEquivalent(buffs, given);
    }

    [Test]
    public void GiveRandomBuff_WithoutBuff_GivesNothing()
    {
        Assert.IsNull(_skill.GiveRandomBuff(new List<Entity> { _ally }));
        CollectionAssert.IsEmpty(GetHandlers(_ally));
    }

    [Test]
    public void Execute_RemovesADebuffAndGivesABuff()
    {
        ABuffHandlerFactory poison = CreateHandler("Poison", _debuffTag);
        ABuffHandlerFactory blessing = CreateHandler("Blessing");
        _skill.data.buffHandlerFactories = new List<ABuffHandlerFactory> { blessing };
        _skill.allies = new List<GameObject> { _ally.gameObject };
        Apply(_ally, poison);

        Assert.IsTrue(_skill.Execute(_purifier.gameObject));

        List<ABuffHandlerFactory> handlers = GetHandlers(_ally);
        CollectionAssert.DoesNotContain(handlers, poison);
        CollectionAssert.Contains(handlers, blessing);
    }

    // A dead ally is neither cleansed nor blessed
    [Test]
    public void Execute_SkipsTheDeadAllies()
    {
        ABuffHandlerFactory blessing = CreateHandler("Blessing");
        _skill.data.buffHandlerFactories = new List<ABuffHandlerFactory> { blessing };
        Entity dead = CreateUnit("Dead", 0f);
        _skill.allies = new List<GameObject> { dead.gameObject, _ally.gameObject };

        for (int i = 0; i < 10; i++)
        {
            _skill.Execute(_purifier.gameObject);
        }

        CollectionAssert.IsEmpty(GetHandlers(dead));
    }

    [Test]
    public void Execute_WithoutLivingAlly_NotUsed()
    {
        _skill.allies = new List<GameObject>();

        Assert.IsFalse(_skill.Execute(_purifier.gameObject));
    }

    [Test]
    public void Cooldown_IsTheRate()
    {
        _skill.data.rate = 5f;

        Assert.AreEqual(5f, _skill.cooldownDuration);
    }
}

}
