using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{

// A cursed item stands out among the items of the player
public class PlayerItemIconTests
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

    GameObject _go;
    PlayerItemIcon _icon;
    Image _image;
    GameplayTag _cursedTag;
    GameplayTag _playerTag;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Icon");
        _icon = _go.AddComponent<PlayerItemIcon>();
        _image = _go.AddComponent<Image>();
        TestHelpers.SetPrivateField(_icon, "_icon", _image);

        _cursedTag = ScriptableObject.CreateInstance<GameplayTag>();
        _cursedTag.name = TagNames.Cursed;
        _playerTag = ScriptableObject.CreateInstance<GameplayTag>();
        _playerTag.name = "Player";
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
        Object.DestroyImmediate(_cursedTag);
        Object.DestroyImmediate(_playerTag);
    }

    bool HasOutline()
    {
        UnityEngine.UI.Outline outline = _image.GetComponent<UnityEngine.UI.Outline>();
        return outline != null && outline.enabled;
    }

    [Test]
    public void Init_CursedItem_HasAnOutline()
    {
        _icon.Init(new StubItem(_playerTag, _cursedTag));

        Assert.IsTrue(HasOutline());
    }

    [Test]
    public void Init_RegularItem_NoOutline()
    {
        _icon.Init(new StubItem(_playerTag));

        Assert.IsFalse(HasOutline());
    }

    [Test]
    public void Init_RegularAfterCursed_RemovesTheOutline()
    {
        _icon.Init(new StubItem(_cursedTag));

        _icon.Init(new StubItem(_playerTag));

        Assert.IsFalse(HasOutline());
    }
}

}
