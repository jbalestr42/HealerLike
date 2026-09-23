using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Items
{

public class ItemTests
{
    GameObject _go;
    Entity _entity;
    BuffManager _buffManager;
    readonly List<Object> _scriptableObjects = new List<Object>();

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Entity");
        // Adding Entity triggers Entity.Reset() (NREs without a full Init())
        TestHelpers.WithLoggingDisabled(() =>
        {
            _entity = _go.AddComponent<Entity>();
        });
        _buffManager = _go.GetComponent<BuffManager>();
        TestHelpers.SetPrivateField(_entity, "_buffManager", _buffManager);
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

    ABuffHandlerFactory CreateHandlerFactory()
    {
        BuffHandlerFactory handlerFactory = CreateTracked<BuffHandlerFactory>();
        handlerFactory.data = new BuffHandlerData
        {
            durationType = DurationType.Infinite,
            buffFactoryList = new List<ABuffFactory>(),
            tags = new List<GameplayTag>(),
        };
        return handlerFactory;
    }

    AItem CreateItem(List<ABuffHandlerFactory> buffs = null, List<ABuffHandlerFactory> onHitEffects = null)
    {
        ItemFactory itemFactory = CreateTracked<ItemFactory>();
        itemFactory.data = new ItemData
        {
            name = "TestItem",
            buffs = buffs ?? new List<ABuffHandlerFactory>(),
            onHitEffects = onHitEffects ?? new List<ABuffHandlerFactory>(),
            onHitConsumers = new List<AConsumerFactory>(),
            projectileBehaviours = new List<ABuffHandlerFactory>(),
            skills = new List<ASkillFactory>(),
        };
        return itemFactory.GetItem();
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
    public void Equip_ItemWithBuff_AddsBuffHandlerOnTheTargetItself()
    {
        ABuffHandlerFactory passive = CreateHandlerFactory();
        AItem item = CreateItem(buffs: new List<ABuffHandlerFactory> { passive });

        item.Equip(_go);

        List<BuffManager.BuffHandlerData> handlers = GetHandlers();
        Assert.AreEqual(1, handlers.Count);
        Assert.AreSame(passive, handlers[0].buffHandlerFactory);
        Assert.AreSame(_go, handlers[0].target);
    }

    [Test]
    public void Equip_ItemWithOnHitEffect_RegistersOnHitEffect()
    {
        ABuffHandlerFactory onHitEffect = CreateHandlerFactory();
        AItem item = CreateItem(onHitEffects: new List<ABuffHandlerFactory> { onHitEffect });

        item.Equip(_go);

        CollectionAssert.AreEqual(new[] { onHitEffect }, _entity.GetOnHitEffects());
    }

    [Test]
    public void Unequip_ItemWithOnHitEffect_UnregistersOnHitEffect()
    {
        ABuffHandlerFactory onHitEffect = CreateHandlerFactory();
        AItem item = CreateItem(onHitEffects: new List<ABuffHandlerFactory> { onHitEffect });
        item.Equip(_go);

        item.Unequip(_go);

        Assert.IsEmpty(_entity.GetOnHitEffects());
    }
}

}
