using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Players
{

// Character.Init() equips the items of its data, like the entities do
public class CharacterTests
{
    GameObject _go;
    Character _character;
    BuffManager _buffManager;
    readonly List<Object> _scriptableObjects = new List<Object>();

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Character");
        TestHelpers.CreateAttributeManager(_go);
        _buffManager = _go.AddComponent<BuffManager>();
        // Adding Character triggers Character.Reset() (an editor-only message), which NREs before Init()
        TestHelpers.WithLoggingDisabled(() => _character = _go.AddComponent<Character>());
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
        foreach (Object scriptableObject in _scriptableObjects)
        {
            Object.DestroyImmediate(scriptableObject);
        }
        _scriptableObjects.Clear();
    }

    T CreateTracked<T>() where T : ScriptableObject
    {
        T instance = ScriptableObject.CreateInstance<T>();
        _scriptableObjects.Add(instance);
        return instance;
    }

    ItemFactory CreateItem(ABuffFactory buff)
    {
        BuffHandlerFactory handlerFactory = CreateTracked<BuffHandlerFactory>();
        handlerFactory.data = new BuffHandlerData
        {
            durationType = DurationType.Infinite,
            buffFactoryList = new List<ABuffFactory> { buff },
            tags = new List<GameplayTag>(),
        };

        ItemFactory itemFactory = CreateTracked<ItemFactory>();
        itemFactory.data = new ItemData
        {
            name = "TestItem",
            buffs = new List<ABuffHandlerFactory> { handlerFactory },
            onHitEffects = new List<ABuffHandlerFactory>(),
            onHitConsumers = new List<AConsumerFactory>(),
            projectileBehaviours = new List<ABuffHandlerFactory>(),
            skills = new List<ASkillFactory>(),
        };
        return itemFactory;
    }

    FlatModifierFactory CreateHealPowerModifier(float value)
    {
        FlatModifierFactory modifier = CreateTracked<FlatModifierFactory>();
        modifier.data = new FlatModifierData { type = AttributeType.HealPower, modifierType = AttributeModifierType.Add, value = value };
        return modifier;
    }

    void Init(params AItemFactory[] items)
    {
        CharacterData data = CreateTracked<CharacterData>();
        data.attributes = new Dictionary<AttributeType, float> { { AttributeType.ManaMax, 100f }, { AttributeType.HealPower, 25f } };
        data.items = new List<AItemFactory>(items);
        // No skill: their buttons need the game UI
        data.skills = new List<ACharacterSkillFactory>();
        data.entities = new List<EntityData>();
        _character.data = data;
        _character.Init();
    }

    // RemoveBuff with an always-false predicate is used only to enumerate the registered handlers
    List<BuffManager.BuffHandlerData> GetHandlers()
    {
        List<BuffManager.BuffHandlerData> handlers = new List<BuffManager.BuffHandlerData>();
        _buffManager.RemoveBuff(buffHandlerData =>
        {
            handlers.Add(buffHandlerData);
            return false;
        });
        return handlers;
    }

    [Test]
    public void Init_WithoutItems_HasNoItemAndNoBuff()
    {
        Init();

        Assert.IsEmpty(_character.items);
        Assert.IsEmpty(GetHandlers());
    }

    [Test]
    public void Init_EquipsEveryItemOfTheData()
    {
        ItemFactory first = CreateItem(CreateHealPowerModifier(10f));
        ItemFactory second = CreateItem(CreateHealPowerModifier(5f));

        Init(first, second);

        Assert.AreEqual(2, _character.items.Count);
        List<BuffManager.BuffHandlerData> handlers = GetHandlers();
        Assert.AreEqual(2, handlers.Count);
        CollectionAssert.AreEquivalent(new[] { first.data.buffs[0], second.data.buffs[0] }, handlers.ConvertAll(handler => handler.buffHandlerFactory));
        Assert.IsTrue(handlers.TrueForAll(handler => handler.target == _go && handler.source == _go));
    }

    [Test]
    public void Init_ItemBuffIsAppliedToTheCharacterAttributes()
    {
        Init(CreateItem(CreateHealPowerModifier(10f)));

        TestHelpers.InvokePrivate(_buffManager, "Update");
        Attribute healPower = _character.attributeManager.Get(AttributeType.HealPower);
        healPower.Update();

        Assert.AreEqual(35f, healPower.Value, 0.0001f);
    }
}

}
