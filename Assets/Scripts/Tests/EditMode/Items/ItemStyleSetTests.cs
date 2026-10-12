using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Items
{

// An item gets the style of its tags (or of their parents), the highest priority one when there are several
public class ItemStyleSetTests
{
    class StubItem : AItem
    {
        readonly List<GameplayTag> _tags;

        public StubItem(params GameplayTag[] tags)
        {
            _tags = new List<GameplayTag>(tags);
        }

        public override void Equip(GameObject target) { }
        public override void Unequip(GameObject target) { }
        public override string title => "Stub";
        public override string description => "";
        public override Sprite icon => null;
        public override List<GameplayTag> tags => _tags;
    }

    readonly List<Object> _assets = new List<Object>();
    ItemStyleSet _set;
    GameplayTag _cursedTag;
    GameplayTag _ascensionTag;
    GameplayTag _playerTag;
    ItemStyle _cursedStyle;
    ItemStyle _ascensionStyle;

    [SetUp]
    public void SetUp()
    {
        _cursedTag = CreateTag(TagNames.Cursed);
        _ascensionTag = CreateTag(TagNames.Ascension);
        _playerTag = CreateTag("Player");
        _cursedStyle = CreateStyle(_cursedTag, 0);
        _ascensionStyle = CreateStyle(_ascensionTag, 1);

        _set = ScriptableObject.CreateInstance<ItemStyleSet>();
        _set.styles.Add(_cursedStyle);
        _set.styles.Add(_ascensionStyle);
        _assets.Add(_set);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object asset in _assets)
        {
            Object.DestroyImmediate(asset);
        }
        _assets.Clear();
    }

    ItemStyle CreateStyle(GameplayTag tag, int priority)
    {
        ItemStyle style = ScriptableObject.CreateInstance<ItemStyle>();
        style.tag = tag;
        style.priority = priority;
        _assets.Add(style);
        return style;
    }

    GameplayTag CreateTag(string name, GameplayTag parent = null)
    {
        GameplayTag tag = ScriptableObject.CreateInstance<GameplayTag>();
        tag.name = name;
        TestHelpers.SetPrivateField(tag, "_parent", parent);
        _assets.Add(tag);
        return tag;
    }

    [Test]
    public void GetStyle_StyledTag_TakesItsStyle()
    {
        Assert.AreEqual(_cursedStyle, _set.GetStyle(new StubItem(_playerTag, _cursedTag)));
        Assert.AreEqual(_ascensionStyle, _set.GetStyle(new StubItem(_ascensionTag)));
    }

    [Test]
    public void GetStyle_SeveralStyledTags_TakesTheHighestPriority()
    {
        Assert.AreEqual(_ascensionStyle, _set.GetStyle(new StubItem(_ascensionTag, _cursedTag)));
        Assert.AreEqual(_ascensionStyle, _set.GetStyle(new StubItem(_cursedTag, _ascensionTag)));
    }

    [Test]
    public void GetStyle_ChildOfAStyledTag_TakesTheStyleOfItsParent()
    {
        GameplayTag child = CreateTag("DarkLibrary", _cursedTag);

        Assert.AreEqual(_cursedStyle, _set.GetStyle(new StubItem(child)));
    }

    [Test]
    public void GetStyle_RegularOrMissingItem_IsNull()
    {
        Assert.IsNull(_set.GetStyle(new StubItem(_playerTag)));
        Assert.IsNull(_set.GetStyle((AItem)null));
    }

    [Test]
    public void GetStyle_StyleWithoutTag_StylesNothing()
    {
        _set.styles.Add(CreateStyle(null, 5));

        Assert.IsNull(_set.GetStyle(new StubItem(_playerTag)));
        Assert.AreEqual(_cursedStyle, _set.GetStyle(new StubItem(_cursedTag)));
    }
}

}
