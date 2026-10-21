using NUnit.Framework;
using UnityEngine;

namespace UI.Toolkit.Icons
{

    public class DataIconServiceTests
    {
        class FakeIconData : IDataIconMetadata
        {
            public Sprite icon { get; set; }
            public string label { get { return null; } }
        }

        class FakeIconOwner : IGameDataSource
        {
            public FakeIconData data;
            public object sourceData { get { return data; } }
        }

        Texture2D _texture;
        Sprite _sprite;
        DataIconService _icons;

        [SetUp]
        public void SetUp()
        {
            _texture = new Texture2D(16, 16);
            _sprite = Sprite.Create(_texture, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f));
            _icons = new DataIconService();
        }

        [TearDown]
        public void TearDown()
        {
            _icons.Clear();
            Object.DestroyImmediate(_sprite);
            Object.DestroyImmediate(_texture);
        }

        [Test]
        public void GetIcon_AuthoredSprite_ReturnsItsTextureBeforeTheGeneratedOne()
        {
            Texture2D icon = _icons.GetIcon(new CharacterSkillData { icon = _sprite });

            Assert.AreSame(_texture, icon);
        }

        [Test]
        public void GetIcon_AuthoredSpriteOnNestedData_ReturnsItsTexture()
        {
            FakeIconOwner owner = new FakeIconOwner { data = new FakeIconData { icon = _sprite } };

            Assert.AreSame(_texture, _icons.GetIcon(owner));
        }

        [Test]
        public void GetIcon_NoAuthoredSprite_GeneratesAnIcon()
        {
            Texture2D icon = _icons.GetIcon(new CharacterSkillData());

            Assert.IsNotNull(icon);
            Assert.AreNotSame(_texture, icon);
        }

        [Test]
        public void GetIcon_CroppedAuthoredSprite_IsNotShownWholeAndFallsThrough()
        {
            Sprite cropped = Sprite.Create(_texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f));

            Texture2D icon = _icons.GetIcon(new CharacterSkillData { icon = cropped });

            Object.DestroyImmediate(cropped);
            Assert.IsNotNull(icon);
            Assert.AreNotSame(_texture, icon);
        }

        [Test]
        public void Clear_AuthoredIcon_IsNotDestroyed()
        {
            _icons.GetIcon(new CharacterSkillData { icon = _sprite });

            _icons.Clear();

            Assert.IsTrue(_texture);
        }

        [Test]
        public void GetIcon_NullData_ReturnsFallbackTexture()
        {
            Texture2D icon = _icons.GetIcon(null);

            Assert.IsNotNull(icon);
        }

        [Test]
        public void GetIcon_SameData_ReturnsCachedTexture()
        {
            Texture2D first = _icons.GetIcon(null);

            Texture2D second = _icons.GetIcon(null);

            Assert.AreSame(first, second);
        }

        [Test]
        public void Clear_GeneratedIcon_DestroysIt()
        {
            Texture2D icon = _icons.GetIcon(null);

            _icons.Clear();

            Assert.IsFalse(icon);
        }

        [Test]
        public void GetIcon_TwoServices_EachOwnsItsTexture()
        {
            DataIconService other = new DataIconService();
            Texture2D mine = _icons.GetIcon(null);

            Texture2D theirs = other.GetIcon(null);

            other.Clear();
            Assert.AreNotSame(mine, theirs);
            Assert.IsTrue(mine);
        }

        [Test]
        public void TryGetAuthoredSprite_NestedDataIcon_ReturnsSprite()
        {
            FakeIconOwner owner = new FakeIconOwner { data = new FakeIconData { icon = _sprite } };

            Sprite sprite = DataIconService.TryGetAuthoredSprite(owner);

            Assert.AreSame(_sprite, sprite);
        }

        [Test]
        public void TryGetAuthoredSprite_KnownDataFamilies_ReturnsOriginalSprites()
        {
            Assert.AreSame(_sprite, DataIconService.TryGetAuthoredSprite(new BaseItemData { icon = _sprite }));
            Assert.AreSame(_sprite, DataIconService.TryGetAuthoredSprite(new CharacterSkillData { icon = _sprite }));
            Assert.AreSame(_sprite, DataIconService.TryGetAuthoredSprite(new BuffHandlerBaseData { icon = _sprite }));
        }

        [Test]
        public void TryGetAuthoredSprite_NoIconField_ReturnsNull()
        {
            Sprite sprite = DataIconService.TryGetAuthoredSprite(new object());

            Assert.IsNull(sprite);
        }
    }
}
