using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{

// An item counting toward an effect shows its count on its icon (e.g. the Rosary: 4, the next spell is free),
// following the skills the character uses
public class PlayerItemIconCounterTests
{
    readonly List<Object> _scriptableObjects = new List<Object>();
    GameObject _characterGo;
    Character _character;
    BuffManager _buffManager;
    BuffHandlerFactory _handler;
    Item _rosary;
    GameObject _iconGo;
    PlayerItemIcon _icon;
    TMPro.TextMeshProUGUI _counter;
    readonly CharacterSkillData _skill = new CharacterSkillData { name = "Heal" };

    [SetUp]
    public void SetUp()
    {
        _characterGo = new GameObject("Character");
        // Adding Character triggers its editor-only Reset(), which NREs without Init(): only its event is used
        TestHelpers.WithLoggingDisabled(() => _character = _characterGo.AddComponent<Character>());
        _buffManager = _characterGo.GetComponent<BuffManager>() ?? _characterGo.AddComponent<BuffManager>();
        _buffManager.isEnabled = true;

        BuffEveryNthSkillBuffFactory counter = Track(ScriptableObject.CreateInstance<BuffEveryNthSkillBuffFactory>());
        // The free spell, given before the fifth one and taken back by it
        BuffHandlerFactory freeSpell = Track(ScriptableObject.CreateInstance<BuffHandlerFactory>());
        freeSpell.data = new BuffHandlerData { durationType = DurationType.Infinite, maxStacks = 1, buffFactoryList = new List<ABuffFactory>(), tags = new List<GameplayTag>() };
        counter.data = new BuffEveryNthSkillBuffData { count = 5, buffHandlerFactory = freeSpell };
        _handler = Track(ScriptableObject.CreateInstance<BuffHandlerFactory>());
        _handler.data = new BuffHandlerData { durationType = DurationType.Infinite, buffFactoryList = new List<ABuffFactory> { counter }, tags = new List<GameplayTag>() };
        ItemFactory factory = Track(ScriptableObject.CreateInstance<ItemFactory>());
        factory.data = new ItemData { name = "Rosary", buffs = new List<ABuffHandlerFactory> { _handler } };
        _rosary = (Item)factory.GetItem();

        _iconGo = new GameObject("Icon");
        _icon = _iconGo.AddComponent<PlayerItemIcon>();
        TestHelpers.SetPrivateField(_icon, "_icon", _iconGo.AddComponent<Image>());
        GameObject counterGo = new GameObject("Counter");
        counterGo.transform.SetParent(_iconGo.transform);
        _counter = counterGo.AddComponent<TMPro.TextMeshProUGUI>();
        TestHelpers.SetPrivateField(_icon, "_counter", _counter);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_iconGo);
        Object.DestroyImmediate(_characterGo);
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

    // As Item.Equip does, the buff manager applying them on its update
    void EquipTheRosary()
    {
        _buffManager.AddHandler(_handler, _characterGo, _characterGo);
        _buffManager.ForceUpdate();
    }

    void UseSkills(int count)
    {
        for (int i = 0; i < count; i++)
        {
            _character.OnSkillUsed.Invoke(_skill);
            _buffManager.ForceUpdate();
        }
    }

    [Test]
    public void ItemBuffsApplied_ShowsTheCount()
    {
        EquipTheRosary();
        _icon.Init(_rosary, _buffManager);

        Assert.IsTrue(_counter.gameObject.activeSelf);
        Assert.AreEqual("0", _counter.text);
    }

    // The icon is made as the item is equipped, its buffs applied on the next update
    [Test]
    public void ItemBuffsAppliedAfterTheIcon_ShowsTheCountThen()
    {
        _icon.Init(_rosary, _buffManager);
        Assert.IsFalse(_counter.gameObject.activeSelf);

        EquipTheRosary();

        Assert.IsTrue(_counter.gameObject.activeSelf);
        Assert.AreEqual("0", _counter.text);
    }

    [Test]
    public void SkillsUsed_TheCountFollows_BackToZeroAfterTheFreeOne()
    {
        EquipTheRosary();
        _icon.Init(_rosary, _buffManager);

        UseSkills(4);
        Assert.AreEqual("4", _counter.text);

        UseSkills(1);
        Assert.AreEqual("0", _counter.text);
    }

    [Test]
    public void ItemBuffsRemoved_HidesTheCount()
    {
        EquipTheRosary();
        _icon.Init(_rosary, _buffManager);

        _buffManager.RemoveHandler(_handler, _characterGo, _characterGo);
        _buffManager.ForceUpdate();

        Assert.IsFalse(_counter.gameObject.activeSelf);
    }

    [Test]
    public void WithoutHolder_NoCount()
    {
        _icon.Init(_rosary);

        Assert.IsFalse(_counter.gameObject.activeSelf);
    }

    // An item counting nothing doesn't follow the buffs of its holder
    [Test]
    public void ItemWithoutCounter_NoCount()
    {
        ItemFactory factory = Track(ScriptableObject.CreateInstance<ItemFactory>());
        factory.data = new ItemData { name = "Plain", buffs = new List<ABuffHandlerFactory>() };
        _icon.Init(factory.GetItem(), _buffManager);

        EquipTheRosary();

        Assert.IsNull(_icon.counterBuff);
        Assert.IsFalse(_counter.gameObject.activeSelf);
    }
}

}
