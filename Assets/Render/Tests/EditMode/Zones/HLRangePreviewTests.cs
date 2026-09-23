using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Zones
{
    public class HLRangePreviewTests
    {
        GameObject _ownerGo;
        GameObject _entityGo;
        HLZoneRegistry _owner;
        Entity _entity;
        HLRangePreview _preview;

        [SetUp]
        public void SetUp()
        {
            _ownerGo = new GameObject("zones");
            _owner = _ownerGo.AddComponent<HLZoneRegistry>();
            _owner.Init(new HLZoneFakeUpload());
            _entityGo = new GameObject("entity");
            AttributeManager attributes = TestHelpers.CreateAttributeManager(_entityGo, AttributeType.Range, 3);
            TestHelpers.WithLoggingDisabled(() => _entity = _entityGo.AddComponent<Entity>());
            _entity.attributeManager = attributes;
            _entity.entityType = Entity.EntityType.Player;
            _preview = _entityGo.AddComponent<HLRangePreview>();
            _preview.observePointer = false;
            _preview.observeHover = false;
            HLRangePreview.allRanges = false;
            _preview.Init(_entity);
        }

        [TearDown]
        public void TearDown()
        {
            HLRangePreview.allRanges = false;
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
            Assert.AreEqual((int)HLZoneKind.Range, _owner.snapshot[0].kind);
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
            _owner.Init(new HLZoneFakeUpload());

            _preview.Refresh();
            _owner.PublishFrame(0f);

            Assert.AreEqual(1, _owner.count);
        }

        [Test]
        public void AllRangesUsesLowStrengthAndExcludesEnemies()
        {
            HLRangePreview.allRanges = true;
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
        public void HoverUsesEntityColliderAndCachesOnlyOneRaycastPerFrame()
        {
            _entityGo.AddComponent<BoxCollider>();
            Physics.SyncTransforms();
            Ray hit = new Ray(Vector3.back * 3f, Vector3.forward);
            Ray miss = new Ray(Vector3.back * 3f, Vector3.back);

            Assert.AreSame(_entity, HLRangePreview.SampleHover(hit, -100));
            Assert.AreSame(_entity, HLRangePreview.SampleHover(miss, -100));
            Assert.IsNull(HLRangePreview.SampleHover(miss, -99));
            Assert.AreEqual(0, _owner.liveCount, "Hover does not invent selection state.");
        }

        [Test]
        public void HoverShowsRangeWithoutOverwritingExplicitSelection()
        {
            _entityGo.AddComponent<BoxCollider>();
            Physics.SyncTransforms();
            GameObject cameraGo = new GameObject("preview camera");
            cameraGo.transform.SetParent(_entityGo.transform);
            _preview.previewCamera = cameraGo.AddComponent<Camera>();
            _preview.observeHover = true;

            HLRangePreview.SampleHover(new Ray(Vector3.back * 3f, Vector3.forward), Time.frameCount);
            _preview.Refresh();
            _owner.PublishFrame(0f);
            Assert.AreEqual(3, _owner.snapshot[0].kind);

            _preview.SetPreviewState(true, false);
            _preview.observeHover = false;
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

            _preview.Init(null);
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
        public void Show_NeitherHoveredNorShowAll_IgnoresTheStaticAllRanges()
        {
            HLRangePreview.allRanges = true;

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
        public void Init_WithoutZones_IgnoresTheStaticRegistry()
        {
            _preview.Init(_entity, null);

            _preview.Show(true, false);
            _preview.Refresh();

            Assert.AreEqual(0, _owner.liveCount);
        }
    }
}
