using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Zones
{
    public class HLRangePreviewTests
    {
        GameObject _ownerGo, _entityGo;
        HLZoneRegistry _owner;
        Entity _entity;
        HLRangePreview _preview;
        [SetUp] public void SetUp()
        {
            _ownerGo = new GameObject("zones");
            _owner = _ownerGo.AddComponent<HLZoneRegistry>();
            _owner.Initialize(new HLZoneFakeUpload());
            _entityGo = new GameObject("entity");
            var attributes = TestHelpers.CreateAttributeManager(_entityGo, AttributeType.Range, 3);
            TestHelpers.WithLoggingDisabled(() => _entity = _entityGo.AddComponent<Entity>());
            _entity.attributeManager = attributes;
            _preview = _entityGo.AddComponent<HLRangePreview>();
            _preview.ObservePointer = false;
            _preview.Init(_entity);
        }
        [TearDown] public void TearDown()
        {
            Object.DestroyImmediate(_entityGo); Object.DestroyImmediate(_ownerGo);
        }
        [TestCase(true, false)] [TestCase(false, true)] [TestCase(true, true)]
        public void SelectionOrDragShowsOneCosmeticZoneAtCurrentEntityPosition(bool selected, bool dragging)
        {
            _entityGo.transform.position = Vector3.one * 4;
            _preview.SetPreviewState(selected, dragging);
            _preview.Refresh();
            _owner.PublishFrame(0);
            Assert.AreEqual(1, _owner.Count);
            Assert.AreEqual(3, _owner.Snapshot[0].radius);
            Assert.AreEqual((int)HLZoneKind.Heal, _owner.Snapshot[0].kind);
            Assert.AreEqual(0.35f, _owner.Snapshot[0].strength);
            Assert.AreEqual(_entityGo.transform.position, _owner.Snapshot[0].position);
            _entityGo.transform.position = Vector3.right;
            _entity.attributeManager.Get(AttributeType.Range).BaseValue = 5;
            _entity.attributeManager.Get(AttributeType.Range).Update();
            _preview.Refresh();
            _owner.PublishFrame(0.1f);
            Assert.AreEqual(Vector3.right, _owner.Snapshot[0].position);
            Assert.AreEqual(5, _owner.Snapshot[0].radius);
            Assert.AreEqual(1, _owner.LiveCount);
        }
        [Test] public void DeselectionDisableAndInvalidRangeRemovePreview()
        {
            _preview.SetPreviewState(true, false);
            _preview.SetPreviewState(false, false);
            Assert.AreEqual(0, _owner.LiveCount);
            _preview.SetPreviewState(true, false);
            _entity.attributeManager.Get(AttributeType.Range).BaseValue = 0;
            _entity.attributeManager.Get(AttributeType.Range).Update();
            _preview.Refresh();
            Assert.AreEqual(0, _owner.LiveCount);
            _entity.attributeManager.Get(AttributeType.Range).BaseValue = 2;
            _entity.attributeManager.Get(AttributeType.Range).Update();
            _preview.Refresh();
            Assert.AreEqual(1, _owner.LiveCount);
            TestHelpers.InvokePrivate(_preview, "OnDisable");
            Assert.AreEqual(0, _owner.LiveCount);
            _preview.Refresh();
            Assert.AreEqual(0, _owner.LiveCount);
        }
        [Test] public void RegistryRestartRecreatesPreviewWithoutStaleHandleUse()
        {
            _preview.SetPreviewState(true, false);
            _owner.Release();
            _owner.Initialize(new HLZoneFakeUpload());
            _preview.Refresh();
            _owner.PublishFrame(0);
            Assert.AreEqual(1, _owner.Count);
        }
        [Test] public void MissingRangeAndDestroyedEntityAreSafe()
        {
            _entity.attributeManager = null;
            _preview.SetPreviewState(true, false);
            Assert.AreEqual(0, _owner.LiveCount);
            _preview.Init(null);
            _preview.SetPreviewState(true, true);
            Assert.AreEqual(0, _owner.LiveCount);
        }
    }
}
