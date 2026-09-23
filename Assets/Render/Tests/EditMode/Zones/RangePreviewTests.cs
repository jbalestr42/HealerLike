using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Zones
{
    public class RangePreviewTests
    {
        GameObject _ownerGo;
        GameObject _entityGo;
        ZoneRegistry _owner;
        Entity _entity;
        RangePreview _preview;

        [SetUp]
        public void SetUp()
        {
            _ownerGo = new GameObject("zones");
            _owner = _ownerGo.AddComponent<ZoneRegistry>();
            _owner.Init(new ZoneFakeUpload());
            _entityGo = new GameObject("entity");
            AttributeManager attributes = TestHelpers.CreateAttributeManager(_entityGo, AttributeType.Range, 3);
            TestHelpers.WithLoggingDisabled(() => _entity = _entityGo.AddComponent<Entity>());
            _entity.attributeManager = attributes;
            _entity.entityType = Entity.EntityType.Player;
            _preview = _entityGo.AddComponent<RangePreview>();
            _preview.Init(_entity, _owner);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_entityGo);
            Object.DestroyImmediate(_ownerGo);
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void SelectionOrDragShowsOneCosmeticZoneAtCurrentEntityPosition(bool selected, bool dragging)
        {
            _entityGo.transform.position = Vector3.one * 4f;

            _preview.SetPreviewState(selected, dragging);
            _preview.Refresh();
            _owner.PublishFrame(0f);

            Assert.AreEqual(1, _owner.count);
            Assert.AreEqual(3, _owner.snapshot[0].radius);
            Assert.AreEqual((int)ZoneKind.Range, _owner.snapshot[0].kind);
            Assert.AreEqual(0.35f, _owner.snapshot[0].strength);
            Assert.AreEqual(_entityGo.transform.position, _owner.snapshot[0].position);

            _entityGo.transform.position = Vector3.right;
            _entity.attributeManager.Get(AttributeType.Range).BaseValue = 5;
            _entity.attributeManager.Get(AttributeType.Range).Update();
            _preview.Refresh();
            _owner.PublishFrame(0.1f);

            Assert.AreEqual(Vector3.right, _owner.snapshot[0].position);
            Assert.AreEqual(5, _owner.snapshot[0].radius);
            Assert.AreEqual(1, _owner.liveCount);
        }

        [Test]
        public void StateChangesPublishOnlyOnUpdate()
        {
            _preview.SetPreviewState(true, false);
            Assert.AreEqual(0, _owner.liveCount);

            TestHelpers.InvokePrivate(_preview, "Update");
            Assert.AreEqual(1, _owner.liveCount);

            _preview.SetPreviewState(false, false);
            Assert.AreEqual(1, _owner.liveCount);

            TestHelpers.InvokePrivate(_preview, "Update");
            Assert.AreEqual(0, _owner.liveCount);
        }

        [Test]
        public void DeselectionDisableAndInvalidRangeRemovePreview()
        {
            _preview.SetPreviewState(true, false);
            _preview.Refresh();
            Assert.AreEqual(1, _owner.liveCount);

            _preview.SetPreviewState(false, false);
            _preview.Refresh();
            Assert.AreEqual(0, _owner.liveCount);

            _preview.SetPreviewState(true, false);
            _entity.attributeManager.Get(AttributeType.Range).BaseValue = 0;
            _entity.attributeManager.Get(AttributeType.Range).Update();
            _preview.Refresh();
            Assert.AreEqual(0, _owner.liveCount);

            _entity.attributeManager.Get(AttributeType.Range).BaseValue = 2;
            _entity.attributeManager.Get(AttributeType.Range).Update();
            _preview.Refresh();
            Assert.AreEqual(1, _owner.liveCount);

            TestHelpers.InvokePrivate(_preview, "OnDisable");
            Assert.AreEqual(0, _owner.liveCount);

            _preview.Refresh();
            Assert.AreEqual(0, _owner.liveCount);
        }

        [Test]
        public void RegistryRestartRecreatesPreviewWithoutStaleHandleUse()
        {
            _preview.SetPreviewState(true, false);
            _preview.Refresh();
            _owner.Release();
            _owner.Init(new ZoneFakeUpload());

            _preview.Refresh();
            _owner.PublishFrame(0f);

            Assert.AreEqual(1, _owner.count);
        }

        [Test]
        public void ShowAllUsesLowStrengthAndExcludesEnemies()
        {
            _preview.Show(false, true);
            _preview.Refresh();
            _owner.PublishFrame(0f);
            Assert.AreEqual(0.15f, _owner.snapshot[0].strength);

            _preview.SetPreviewState(true, false);
            _owner.PublishFrame(0f);
            Assert.AreEqual(0.15f, _owner.snapshot[0].strength);

            _entity.entityType = Entity.EntityType.Computer;
            _preview.Refresh();
            Assert.AreEqual(0, _owner.liveCount);
        }

        [Test]
        public void HoverShowsRangeWithoutOverwritingExplicitSelection()
        {
            _preview.Show(true, false);
            _preview.Refresh();
            _owner.PublishFrame(0f);
            Assert.AreEqual(3, _owner.snapshot[0].kind);

            _preview.SetPreviewState(true, false);
            _preview.Show(false, false);
            _preview.Refresh();
            Assert.AreEqual(1, _owner.liveCount);

            _preview.SetPreviewState(false, false);
            _preview.Refresh();
            Assert.AreEqual(0, _owner.liveCount);
        }

        [Test]
        public void MissingRangeAndDestroyedEntityAreSafe()
        {
            _entity.attributeManager = null;
            _preview.SetPreviewState(true, false);
            Assert.AreEqual(0, _owner.liveCount);

            _preview.Init(null, _owner);
            _preview.SetPreviewState(true, true);
            Assert.AreEqual(0, _owner.liveCount);
        }


        [Test]
        public void Show_Hovered_ShowsTheRangeAtFullPreviewStrength()
        {
            _preview.Show(true, false);
            _preview.Refresh();
            _owner.PublishFrame(0f);

            Assert.AreEqual(1, _owner.count);
            Assert.AreEqual(0.35f, _owner.snapshot[0].strength);
        }

        [Test]
        public void Show_ShowAll_UsesTheLowStrength()
        {
            _preview.Show(false, true);
            _preview.Refresh();
            _owner.PublishFrame(0f);

            Assert.AreEqual(0.15f, _owner.snapshot[0].strength);
        }

        [Test]
        public void Show_NeitherHoveredNorShowAll_HidesTheRange()
        {
            _preview.Show(false, false);
            _preview.Refresh();

            Assert.AreEqual(0, _owner.liveCount);
        }

        [Test]
        public void Show_Selected_StillShowsWhenNotHovered()
        {
            _preview.Show(false, false);
            _preview.SetPreviewState(true, false);
            _preview.Refresh();

            Assert.AreEqual(1, _owner.liveCount);
        }

        [Test]
        public void Init_WithoutZones_ShowsNothing()
        {
            _preview.Init(_entity, (ZoneRegistry)null);

            _preview.Show(true, false);
            _preview.Refresh();

            Assert.AreEqual(0, _owner.liveCount);
        }
    }
}
