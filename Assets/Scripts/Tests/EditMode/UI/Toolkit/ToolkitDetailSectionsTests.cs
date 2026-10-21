using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Toolkit
{
    // The foldouts of the detail panel: filled from the inspected creature, hidden when empty
    public class ToolkitDetailSectionsTests
    {
        class StubItem : AItem
        {
            readonly string _title;

            public StubItem(string title)
            {
                _title = title;
            }

            public override void Equip(GameObject target) { }
            public override void Unequip(GameObject target) { }
            public override string title => _title;
            public override string description => "";
            public override Sprite icon => null;
            public override List<GameplayTag> tags => new List<GameplayTag>();
        }

        readonly TestUnits _units = new TestUnits();
        TemplateContainer _root;
        ToolkitGameView _view;
        ToolkitDetailPanel _details;
        ToolkitGameContext _context;
        EntityData _data;
        Entity _entity;

        [SetUp]
        public void SetUp()
        {
            _root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
            _view = new ToolkitGameView(_root);
            _context = new ToolkitGameContext();
            _details = new ToolkitDetailPanel();
            _details.Init(_context, _view);
            _entity = _units.Create(63f, 100f, "Zealot");
            _data = ScriptableObject.CreateInstance<EntityData>();
            _data.title = "Zealot";
            _entity.data = _data;
            _context.selectedEntity = _entity;
        }

        [TearDown]
        public void TearDown()
        {
            _details.Dispose();
            _view.Release();
            _units.DestroyAll();
            Object.DestroyImmediate(_data);
        }

        [Test]
        public void Validate_ShippedLayoutWithDetailSections_ReturnsTrue()
        {
            Assert.That(ToolkitLayoutContract.Validate(_root), Is.True);
        }

        [Test]
        public void RefreshEntity_EntityWithItem_ShowsItemsSectionWithItsCount()
        {
            StubItem item = new StubItem("Zeal");
            _entity.items.Add(item);

            _details.RefreshEntity();

            Foldout items = _root.Q<Foldout>("detail-items");
            Assert.That(items.ClassListContains("is-hidden"), Is.False);
            Assert.That(items.text, Is.EqualTo("Items (1)"));
            Assert.That(_root.Q<Label>("detail-items-text").text, Does.Contain(EntityInfoFormatter.FormatItemDetails(item)));
        }

        [Test]
        public void RefreshEntity_EntityWithoutItem_HidesItemsSection()
        {
            _details.RefreshEntity();

            Assert.That(_root.Q<Foldout>("detail-items").ClassListContains("is-hidden"), Is.True);
        }

        [Test]
        public void RefreshEntity_ItemAddedAfterFirstRefresh_UpdatesTheCount()
        {
            _details.RefreshEntity();
            _entity.items.Add(new StubItem("Zeal"));

            _details.RefreshEntity();

            Assert.That(_root.Q<Foldout>("detail-items").text, Is.EqualTo("Items (1)"));
        }

        [Test]
        public void RefreshEntity_HealthCanNotBeConsumed_SummaryShowsInvulnerable()
        {
            _entity.health.preventConsumers = true;

            _details.RefreshEntity();

            Assert.That(_root.Q<Label>("detail-description").text, Does.Contain("Invulnerable"));
        }

        [Test]
        public void RefreshEntity_EntityWithoutAttributes_LeavesFullStatsEmpty()
        {
            _details.RefreshEntity();

            Assert.That(_root.Q<Label>("detail-full-stats").text, Is.EqualTo(""));
        }

        [Test]
        public void ShowDetail_ModelWithoutEntity_HidesEverySection()
        {
            _entity.items.Add(new StubItem("Zeal"));
            _details.RefreshEntity();

            _view.ShowDetail(new ToolkitCardModel { title = "Spell", description = "A spell" });

            Assert.That(_root.Q<Foldout>("detail-items").ClassListContains("is-hidden"), Is.True);
        }

        [Test]
        public void OnInspect_ExpandedSection_CollapsesIt()
        {
            _entity.items.Add(new StubItem("Zeal"));
            _details.RefreshEntity();
            Foldout items = _root.Q<Foldout>("detail-items");
            items.value = true;

            _details.OnInspect(new ToolkitCardModel { source = _entity });

            Assert.That(items.value, Is.False);
        }
    }
}
