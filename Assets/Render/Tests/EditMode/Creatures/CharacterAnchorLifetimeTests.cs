using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public class CharacterAnchorLifetimeTests : CreatureMeshOwnershipFixture
    {
        CharacterView _view;
        GameObject _anchor;

        void CreateView(bool childAnchor)
        {
            Character character = null;
            TestHelpers.WithLoggingDisabled(() => character = _owner.AddComponent<Character>());
            _anchor = new GameObject("Separate visual anchor");
            if (childAnchor)
            {
                _anchor.transform.SetParent(_owner.transform, false);
            }
            _view = _owner.AddComponent<CharacterView>();
            TestHelpers.SetPrivateField(_view, "_character", character);
            TestHelpers.SetPrivateField(_view, "_recipe", _recipe);
            TestHelpers.SetPrivateField(_view, "_meshes", _meshes);
            TestHelpers.SetPrivateField(_view, "_material", _material);
            TestHelpers.SetPrivateField(_view, "_visualAnchor", _anchor.transform);
            TestHelpers.InvokePrivate(_view, "BuildAndRegister");
        }

        [TearDown]
        public void ReleaseView()
        {
            if (_view)
            {
                TestHelpers.InvokePrivate(_view, "OnDestroy");
            }
            Object.DestroyImmediate(_anchor);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AnchorToggle_RetainsRigAndNeverOverridesHiddenOwner(bool childAnchor)
        {
            CreateView(childAnchor);
            CreatureRig rig = _view.rig;
            _anchor.SetActive(false);
            Assert.IsTrue(_view.isActiveAndEnabled);
            TestHelpers.InvokePrivate(_view, "LateUpdate");
            Assert.IsFalse(_view.presentation.gameObject.activeInHierarchy);
            _anchor.SetActive(true);
            TestHelpers.InvokePrivate(_view, "LateUpdate");
            Assert.IsTrue(_view.presentation.gameObject.activeInHierarchy);
            Assert.AreSame(rig, _view.rig);

            _view.enabled = false;
            TestHelpers.InvokePrivate(_view, "OnDisable");
            _anchor.SetActive(false);
            _view.SyncGeometry();
            _anchor.SetActive(true);
            _view.SyncGeometry();
            Assert.IsFalse(_view.presentation.gameObject.activeInHierarchy);
            _view.enabled = true;
            TestHelpers.InvokePrivate(_view, "OnEnable");
            Assert.IsTrue(_view.presentation.gameObject.activeInHierarchy);
            Assert.AreSame(rig, _view.rig);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AnchorDestroyed_ReleasesOwnedGeometryAndCanInitializeANewAnchor(bool childAnchor)
        {
            _recipe.parts[0].shape = ShapeProfile.Bulb();
            CreateView(childAnchor);
            CreatureRig previous = _view.rig;
            Transform presentation = _view.presentation;
            Mesh shape = previous.partTransforms[0].GetComponent<MeshFilter>().sharedMesh;
            Assert.AreNotSame(_source, shape);
            Object.DestroyImmediate(_anchor);
            TestHelpers.InvokePrivate(_view, "LateUpdate");
            Assert.IsFalse(presentation);
            Assert.IsFalse(shape);
            Assert.IsNull(_view.rig);
            Assert.IsTrue(_source);

            _anchor = new GameObject("Replacement visual anchor");
            _anchor.transform.position = new Vector3(7f, 3f, -4f);
            TestHelpers.SetPrivateField(_view, "_visualAnchor", _anchor.transform);
            TestHelpers.InvokePrivate(_view, "BuildAndRegister");
            TestHelpers.InvokePrivate(_view, "LateUpdate");
            Assert.IsNotNull(_view.rig);
            Assert.AreNotSame(previous, _view.rig);
            Assert.AreEqual(_anchor.transform.position, _view.rig.root.position);
            Assert.IsTrue(_view.presentation.gameObject.activeInHierarchy);
        }
    }
}
