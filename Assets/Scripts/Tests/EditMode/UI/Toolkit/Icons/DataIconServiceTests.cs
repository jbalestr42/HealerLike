using NUnit.Framework;
using UnityEngine;

namespace UI.Toolkit.Icons
{

public class DataIconServiceTests
{
    class FakeIconData
    {
        public Sprite icon;
    }

    class FakeIconOwner
    {
        public FakeIconData data;
    }

    Texture2D _texture;
    Sprite _sprite;

    [SetUp]
    public void SetUp()
    {
        _texture = new Texture2D(16, 16);
        _sprite = Sprite.Create(_texture, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f));
    }

    [TearDown]
    public void TearDown()
    {
        DataIconService.Clear();
        Object.DestroyImmediate(_sprite);
        Object.DestroyImmediate(_texture);
    }

    [Test]
    public void GetIcon_NullData_ReturnsFallbackTexture()
    {
        Texture2D icon = DataIconService.GetIcon(null);

        Assert.IsNotNull(icon);
    }

    [Test]
    public void GetIcon_SameData_ReturnsCachedTexture()
    {
        Texture2D first = DataIconService.GetIcon(null);

        Texture2D second = DataIconService.GetIcon(null);

        Assert.AreSame(first, second);
    }

    [Test]
    public void TryGetAuthoredSprite_NestedDataIcon_ReturnsSprite()
    {
        FakeIconOwner owner = new FakeIconOwner { data = new FakeIconData { icon = _sprite } };

        Sprite sprite = DataIconService.TryGetAuthoredSprite(owner);

        Assert.AreSame(_sprite, sprite);
    }

    [Test]
    public void TryGetAuthoredSprite_NoIconField_ReturnsNull()
    {
        Sprite sprite = DataIconService.TryGetAuthoredSprite(new object());

        Assert.IsNull(sprite);
    }
}
}
