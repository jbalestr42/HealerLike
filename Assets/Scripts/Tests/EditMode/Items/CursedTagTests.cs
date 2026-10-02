using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Items
{

// An item is cursed when it has the Cursed tag, or a child of it
public class CursedTagTests
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

    readonly List<Object> _created = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (Object created in _created)
        {
            Object.DestroyImmediate(created);
        }
        _created.Clear();
    }

    GameplayTag CreateTag(string name, GameplayTag parent = null)
    {
        GameplayTag tag = ScriptableObject.CreateInstance<GameplayTag>();
        tag.name = name;
        TestHelpers.SetPrivateField(tag, "_parent", parent);
        _created.Add(tag);
        return tag;
    }

    [Test]
    public void IsCursed_WithTheCursedTagOrAChildOfIt()
    {
        GameplayTag cursedTag = CreateTag(CursedTag.Name);
        GameplayTag childTag = CreateTag("CursedWeapon", cursedTag);

        Assert.IsTrue(CursedTag.IsCursed(new StubItem(cursedTag)));
        Assert.IsTrue(CursedTag.IsCursed(new StubItem(CreateTag("Player"), childTag)));
    }

    [Test]
    public void IsCursed_CursedTagUnderAnotherOne_StillCursed()
    {
        // The Cursed tag is itself a child of Permanent
        GameplayTag cursedTag = CreateTag(CursedTag.Name, CreateTag("Permanent"));

        Assert.IsTrue(CursedTag.IsCursed(new StubItem(cursedTag)));
    }

    [Test]
    public void IsCursed_OtherTagsOrNoItem_IsFalse()
    {
        Assert.IsFalse(CursedTag.IsCursed(new StubItem(CreateTag("Player"))));
        Assert.IsFalse(CursedTag.IsCursed(new StubItem()));
        Assert.IsFalse(CursedTag.IsCursed(new StubItem((GameplayTag)null)));
        Assert.IsFalse(CursedTag.IsCursed((AItem)null));
        Assert.IsFalse(CursedTag.IsCursed((AItemFactory)null));
    }

    [Test]
    public void IsCursed_Factory_FromItsTags()
    {
        ItemFactory cursed = ScriptableObject.CreateInstance<ItemFactory>();
        ItemFactory regular = ScriptableObject.CreateInstance<ItemFactory>();
        _created.Add(cursed);
        _created.Add(regular);
        cursed.data = new ItemData { tags = new List<GameplayTag> { CreateTag(CursedTag.Name) } };
        regular.data = new ItemData { tags = new List<GameplayTag> { CreateTag("Player") } };

        Assert.IsTrue(CursedTag.IsCursed(cursed));
        Assert.IsFalse(CursedTag.IsCursed(regular));
    }
}

}
