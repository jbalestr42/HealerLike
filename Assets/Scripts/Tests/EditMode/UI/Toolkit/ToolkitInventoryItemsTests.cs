using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Toolkit
{

    // The healer's items in the inventory panel: the ones a class starts with (Character.items, outside its
    // inventory handler) are listed like the ones won, and a cursed item is marked
    public class ToolkitInventoryItemsTests
    {
        class StubItem : AItem
        {
            readonly string _title;
            readonly List<GameplayTag> _tags;

            public StubItem(string title, params GameplayTag[] tags)
            {
                _title = title;
                _tags = new List<GameplayTag>(tags);
            }

            public override void Equip(GameObject target) { }
            public override void Unequip(GameObject target) { }
            public override string title => _title;
            public override string description => "";
            public override Sprite icon => null;
            public override List<GameplayTag> tags => _tags;
        }

        GameObject _go;
        Character _character;
        GameplayTag _cursedTag;
        ToolkitInventoryPanel _panel;
        List<ToolkitCardModel> _models;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("Cleric");
            TestHelpers.WithLoggingDisabled(() => _character = _go.AddComponent<Character>());
            _cursedTag = ScriptableObject.CreateInstance<GameplayTag>();
            _cursedTag.name = TagNames.Cursed;
            _panel = new ToolkitInventoryPanel();
            _models = new List<ToolkitCardModel>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_cursedTag);
        }

        [Test]
        public void AddInnateItems_StartingItem_AddsACardWithoutAnOwner()
        {
            AItem item = new StubItem("Holy Book");
            _character.items.Add(item);

            _panel.AddInnateItems(_models, _character);

            Assert.AreEqual(1, _models.Count);
            ToolkitItemEntry entry = (ToolkitItemEntry)_models[0].source;
            Assert.AreSame(item, entry.item);
            Assert.IsNull(entry.owner);
            Assert.IsTrue(entry.isInnate);
            Assert.AreEqual("Holy Book", _models[0].title);
            Assert.IsNotNull(_models[0].activate);
        }

        [Test]
        public void AddInnateItems_StartingItem_SaysItIsAStartingItem()
        {
            _character.items.Add(new StubItem("Holy Book"));

            _panel.AddInnateItems(_models, _character);

            StringAssert.Contains("Starting item", _models[0].status);
            StringAssert.Contains("Starting item", _models[0].description);
        }

        [Test]
        public void AddInnateItems_CursedItem_MarksTheCard()
        {
            _character.items.Add(new StubItem("Idol", _cursedTag));

            _panel.AddInnateItems(_models, _character);

            Assert.IsTrue(_models[0].isCursed);
            StringAssert.Contains("Cursed", _models[0].status);
        }

        [Test]
        public void AddInnateItems_RegularItem_LeavesTheCardUnmarked()
        {
            _character.items.Add(new StubItem("Holy Book"));

            _panel.AddInnateItems(_models, _character);

            Assert.IsFalse(_models[0].isCursed);
            StringAssert.DoesNotContain("Cursed", _models[0].status);
        }

        [Test]
        public void AddInnateItems_NullItem_IsSkipped()
        {
            _character.items.Add(null);
            _character.items.Add(new StubItem("Holy Book"));

            _panel.AddInnateItems(_models, _character);

            Assert.AreEqual(1, _models.Count);
        }

        [Test]
        public void AddInnateItems_NoStartingItemsOrNoCharacter_AddsNothing()
        {
            _panel.AddInnateItems(_models, _character);
            _panel.AddInnateItems(_models, null);

            Assert.AreEqual(0, _models.Count);
        }

        [Test]
        public void AddItems_HandlerItem_KeepsItsOwnerAndSlot()
        {
            AItem item = new StubItem("Merchant's Ledger");
            _character.inventoryHandler.AddItem(item, 2);

            _panel.AddItems(_models, _character.inventoryHandler, "Healer");

            ToolkitItemEntry entry = (ToolkitItemEntry)_models[0].source;
            Assert.AreSame(_character.inventoryHandler, entry.owner);
            Assert.IsFalse(entry.isInnate);
            Assert.AreEqual("Healer · Slot 3", _models[0].status);
        }

        [Test]
        public void AddItems_CursedHandlerItem_MarksTheCard()
        {
            _character.inventoryHandler.AddItem(new StubItem("Idol", _cursedTag), -1);

            _panel.AddItems(_models, _character.inventoryHandler, "Healer");

            Assert.IsTrue(_models[0].isCursed);
        }

        [Test]
        public void IsInnate_StartingItem_IsTrueAndHandlerItemIsFalse()
        {
            AItem starting = new StubItem("Holy Book");
            AItem won = new StubItem("Merchant's Ledger");
            _character.items.Add(starting);
            _character.inventoryHandler.AddItem(won, -1);

            Assert.IsTrue(ToolkitInventoryPanel.IsInnate(_character, starting));
            Assert.IsFalse(ToolkitInventoryPanel.IsInnate(_character, won));
        }

        [Test]
        public void IsInnate_NothingSelectedOrNoCharacter_IsFalse()
        {
            Assert.IsFalse(ToolkitInventoryPanel.IsInnate(_character, null));
            Assert.IsFalse(ToolkitInventoryPanel.IsInnate(null, new StubItem("Holy Book")));
        }
    }

    // The card of a cursed model carries the class the theme outlines
    public class ToolkitCursedCardTests
    {
        VisualElement _root;
        VisualElement _list;
        ToolkitGameView _view;

        [SetUp]
        public void SetUp()
        {
            _root = new VisualElement();
            _list = new VisualElement();
            _list.name = "cards";
            _root.Add(_list);
            _view = new ToolkitGameView(_root);
        }

        [TearDown]
        public void TearDown()
        {
            _view.Release();
        }

        static ToolkitCardModel CreateModel(bool isCursed)
        {
            ToolkitCardModel model = new ToolkitCardModel();
            model.title = "Idol";
            model.isCursed = isCursed;
            return model;
        }

        [Test]
        public void SetCards_CursedModel_AddsTheCursedClass()
        {
            _view.SetCards("cards", new ToolkitCardModel[] { CreateModel(true), CreateModel(false) });

            Assert.IsTrue(_list[0].Q<Button>("data-card").ClassListContains("data-card--cursed"));
            Assert.IsFalse(_list[1].Q<Button>("data-card").ClassListContains("data-card--cursed"));
        }

        [Test]
        public void SetCards_ReusedCardNoLongerCursed_DropsTheCursedClass()
        {
            _view.SetCards("cards", new ToolkitCardModel[] { CreateModel(true) });
            VisualElement card = _list[0];

            _view.SetCards("cards", new ToolkitCardModel[] { CreateModel(false) });

            Assert.AreSame(card, _list[0]);
            Assert.IsFalse(card.Q<Button>("data-card").ClassListContains("data-card--cursed"));
        }
    }
}
