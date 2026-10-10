using NUnit.Framework;
using UnityEngine;

namespace Oisif.Editor.Tests
{
    public class PreviewFieldTests
    {
        GameObject _gameObject;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("PreviewFieldTests");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void GetDropped_ObjectOfTheType_TheObject()
        {
            Assert.AreSame(_gameObject, PreviewField.GetDropped(new Object[] { _gameObject }, typeof(GameObject)));
        }

        [Test]
        public void GetDropped_GameObjectForAComponent_ItsComponent()
        {
            BoxCollider collider = _gameObject.AddComponent<BoxCollider>();

            Assert.AreSame(collider, PreviewField.GetDropped(new Object[] { _gameObject }, typeof(BoxCollider)));
        }

        [Test]
        public void GetDropped_GameObjectWithoutTheComponent_Null()
        {
            Assert.IsNull(PreviewField.GetDropped(new Object[] { _gameObject }, typeof(BoxCollider)));
        }

        [Test]
        public void GetDropped_OtherType_Null()
        {
            Assert.IsNull(PreviewField.GetDropped(new Object[] { _gameObject }, typeof(Sprite)));
        }

        [Test]
        public void GetDropped_SkipsTheObjectsItCannotHold()
        {
            Assert.AreSame(_gameObject, PreviewField.GetDropped(new Object[] { null, _gameObject.transform, _gameObject }, typeof(GameObject)));
        }
    }
}
