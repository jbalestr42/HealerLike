using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Items
{

// The count of an item counting toward an effect (e.g. the Rosary, every fifth spell free), found among the buffs
// its holder got from it, for its icon
public class ItemCounterTests
{
    class StubItem : AItem
    {
        public override void Equip(GameObject target) { }
        public override void Unequip(GameObject target) { }
        public override string title => "Stub";
        public override string description => "";
        public override Sprite icon => null;
        public override List<GameplayTag> tags => new List<GameplayTag>();
    }

    readonly List<Object> _scriptableObjects = new List<Object>();
    GameObject _holder;
    BuffManager _buffManager;
    BuffHandlerFactory _handler;
    BuffEveryNthSkillBuffFactory _counter;
    Item _item;

    [SetUp]
    public void SetUp()
    {
        _holder = new GameObject("Holder");
        _buffManager = _holder.AddComponent<BuffManager>();
        _buffManager.isEnabled = true;
        // For the damage buff given next to the counter
        TestHelpers.CreateAttributeManager(_holder, AttributeType.Damage, 10f);

        // Next to a buff counting nothing
        FlatModifierFactory damage = Track(ScriptableObject.CreateInstance<FlatModifierFactory>());
        damage.data = new FlatModifierData { type = AttributeType.Damage, modifierType = AttributeModifierType.Add, value = 1f };
        _counter = Track(ScriptableObject.CreateInstance<BuffEveryNthSkillBuffFactory>());
        _counter.data = new BuffEveryNthSkillBuffData { count = 5 };
        _handler = Track(ScriptableObject.CreateInstance<BuffHandlerFactory>());
        _handler.data = new BuffHandlerData { durationType = DurationType.Infinite, buffFactoryList = new List<ABuffFactory> { damage, _counter }, tags = new List<GameplayTag>() };
        ItemFactory factory = Track(ScriptableObject.CreateInstance<ItemFactory>());
        factory.data = new ItemData { name = "Rosary", buffs = new List<ABuffHandlerFactory> { _handler } };
        _item = (Item)factory.GetItem();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_holder);
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

    void GiveTheItemBuffs()
    {
        _buffManager.AddHandler(_handler, _holder, _holder);
        _buffManager.ForceUpdate();
    }

    [Test]
    public void Find_OnceTheItemBuffsAreApplied_TheCounter()
    {
        GiveTheItemBuffs();

        ICounterBuff counterBuff = ItemCounter.Find(_item, _buffManager);

        Assert.IsInstanceOf<BuffEveryNthSkillBuff>(counterBuff);
        Assert.AreEqual(5, counterBuff.counterMax);
    }

    // The buff manager applies them on its next update
    [Test]
    public void Find_BeforeTheItemBuffsAreApplied_None()
    {
        _buffManager.AddHandler(_handler, _holder, _holder);

        Assert.IsNull(ItemCounter.Find(_item, _buffManager));
    }

    [Test]
    public void Find_ItemWithoutCounter_None()
    {
        GiveTheItemBuffs();

        Assert.IsNull(ItemCounter.Find(new StubItem(), _buffManager));
    }

    [Test]
    public void Find_WithoutBuffManager_None()
    {
        Assert.IsNull(ItemCounter.Find(_item, null));
    }

    // The current count alone, not the max
    [Test]
    public void GetText_TheCurrentCount()
    {
        GiveTheItemBuffs();

        Assert.AreEqual("0", ItemCounter.GetText(ItemCounter.Find(_item, _buffManager)));
    }
}

}
