using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

// Gives an item to the holder while the buff lasts (e.g. the Archon getting the Blight of a dead Blight Acolyte)
public class EquipItemBuffTests
{
    readonly TestUnits _units = new TestUnits();
    readonly List<Object> _objects = new List<Object>();
    BuffHandlerFactory _onHitEffect;
    BuffHandlerFactory _projectileBehaviour;
    ItemFactory _item;
    Entity _holder;

    [SetUp]
    public void SetUp()
    {
        _onHitEffect = Track(ScriptableObject.CreateInstance<BuffHandlerFactory>());
        _projectileBehaviour = Track(ScriptableObject.CreateInstance<BuffHandlerFactory>());
        _item = Track(ScriptableObject.CreateInstance<ItemFactory>());
        _item.data = new ItemData
        {
            name = "Venom",
            onHitEffects = new List<ABuffHandlerFactory> { _onHitEffect },
            projectileBehaviours = new List<ABuffHandlerFactory> { _projectileBehaviour },
        };
        _holder = _units.Create(100f, 100f, "Archon");
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        foreach (Object obj in _objects)
        {
            Object.DestroyImmediate(obj);
        }
        _objects.Clear();
    }

    T Track<T>(T obj) where T : Object
    {
        _objects.Add(obj);
        return obj;
    }

    EquipItemBuff CreateBuff(AItemFactory item) => new EquipItemBuff { data = new EquipItemBuffData { item = item } };

    [Test]
    public void Add_EquipsTheItem()
    {
        CreateBuff(_item).Add(_holder.gameObject, _holder.gameObject);

        CollectionAssert.AreEqual(new[] { _onHitEffect }, _holder.GetOnHitEffects());
        CollectionAssert.AreEqual(new[] { _projectileBehaviour }, _holder.projectileBehaviours);
    }

    [Test]
    public void Remove_UnequipsTheItem()
    {
        EquipItemBuff buff = CreateBuff(_item);
        buff.Add(_holder.gameObject, _holder.gameObject);

        buff.Remove(_holder.gameObject, _holder.gameObject);

        CollectionAssert.IsEmpty(_holder.GetOnHitEffects());
        CollectionAssert.IsEmpty(_holder.projectileBehaviours);
    }

    [Test]
    public void AddedTwice_EquipsTheItemOnce()
    {
        EquipItemBuff buff = CreateBuff(_item);
        buff.Add(_holder.gameObject, _holder.gameObject);

        buff.Add(_holder.gameObject, _holder.gameObject);

        Assert.AreEqual(1, _holder.GetOnHitEffects().Count);
    }

    [Test]
    public void NoItem_DoesNothing()
    {
        EquipItemBuff buff = CreateBuff(null);

        Assert.DoesNotThrow(() =>
        {
            buff.Add(_holder.gameObject, _holder.gameObject);
            buff.Remove(_holder.gameObject, _holder.gameObject);
        });
        CollectionAssert.IsEmpty(_holder.GetOnHitEffects());
    }
}

}
