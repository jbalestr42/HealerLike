using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UI.Toolkit
{

    // The healer sheet: Julien's CharacterInfoPanel text, in a Toolkit panel the header opens
    public class ToolkitSheetPanelTests
    {
        class StubItem : AItem
        {
            readonly string _title;
            readonly string _description;

            public StubItem(string title, string description)
            {
                _title = title;
                _description = description;
            }

            public override void Equip(GameObject target) { }
            public override void Unequip(GameObject target) { }
            public override string title => _title;
            public override string description => _description;
            public override Sprite icon => null;
            public override List<GameplayTag> tags => new List<GameplayTag>();
        }

        ToolkitTestPanel _panel;
        ToolkitGameView _view;
        ToolkitGameContext _context;
        ToolkitSheetPanel _sheet;
        GameObject _host;
        GameObject _characterObject;
        Character _character;
        ResourceAttribute _mana;
        int _changes;

        [SetUp]
        public void SetUp()
        {
            _characterObject = new GameObject("Cleric");
            _mana = TestHelpers.CreateResourceAttribute(_characterObject, AttributeType.ManaMax, 100f);
            _mana.SetValue(40f);
            AttributeManager attributeManager = _characterObject.GetComponent<AttributeManager>();
            TestHelpers.WithLoggingDisabled(() => _character = _characterObject.AddComponent<Character>());
            _character.attributeManager = attributeManager;
            TestHelpers.SetPrivateField(_character, "_mana", _mana);

            _host = new GameObject("Sheet fixture");
            _host.SetActive(false);
            _context = new ToolkitGameContext();
            _context.ui = _host.AddComponent<UIManager>();
            TestHelpers.SetPrivateField(_context.ui, "_currentView", ViewType.Game);
            _context.player = _host.AddComponent<PlayerBehaviour>();
            _context.player.character = _character;

            _panel = new ToolkitTestPanel();
            VisualElement root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
            _panel.root.Add(root);
            _view = new ToolkitGameView(root);
            _changes = 0;
            _sheet = new ToolkitSheetPanel();
            _sheet.Init(_context, _view, OnChanged);
        }

        [TearDown]
        public void TearDown()
        {
            _sheet.Dispose();
            _view.Release();
            _panel.Dispose();
            Object.DestroyImmediate(_host);
            Object.DestroyImmediate(_characterObject);
        }

        void OnChanged()
        {
            _changes++;
            _sheet.Refresh();
        }

        bool IsShown(string name)
        {
            return !_view.root.Q(name).ClassListContains("is-hidden");
        }

        string SheetText()
        {
            return _view.root.Q<Label>("sheet-text").text;
        }

        [Test]
        public void Refresh_ClosedWithACharacter_ShowsTheButtonAndHidesThePanel()
        {
            _sheet.Refresh();

            Assert.IsTrue(IsShown("sheet-button"));
            Assert.IsTrue(_view.root.Q<Button>("sheet-button").enabledSelf);
            Assert.IsFalse(IsShown("sheet-panel"));
        }

        [Test]
        public void Refresh_Open_ShowsJuliensTextWithItsSizeNormalised()
        {
            _context.isSheetOpen = true;

            _sheet.Refresh();

            Assert.IsTrue(IsShown("sheet-panel"));
            StringAssert.Contains("<b>40 / 100</b>", SheetText());
            StringAssert.Contains(EntityInfoFormatter.GetAttributeName(AttributeType.ManaMax) + ": <b>100</b>", SheetText());
            StringAssert.DoesNotContain("<size=+", SheetText());
            Assert.IsTrue(Regex.IsMatch(SheetText(), @"<size=\d+(\.\d+)?><b>Cleric</b></size>"), SheetText());
        }

        [Test]
        public void Refresh_OpenWithAStartingItem_ListsItAsInnate()
        {
            _character.items.Add(new StubItem("Holy Book", "Heals more"));
            _context.isSheetOpen = true;

            _sheet.Refresh();

            StringAssert.Contains("<b>Holy Book</b> \u2014 Heals more", SheetText());
            StringAssert.Contains("(innate)", SheetText());
        }

        [Test]
        public void Refresh_ManaChanges_TheTextFollows()
        {
            _context.isSheetOpen = true;
            _sheet.Refresh();

            _mana.SetValue(10f);
            _sheet.Refresh();

            StringAssert.Contains("<b>10 / 100</b>", SheetText());
        }

        [Test]
        public void Refresh_NoCharacter_HidesTheButtonAndClosesTheSheet()
        {
            _context.isSheetOpen = true;
            _context.player.character = null;

            _sheet.Refresh();

            Assert.IsFalse(IsShown("sheet-button"));
            Assert.IsFalse(IsShown("sheet-panel"));
            Assert.IsFalse(_context.isSheetOpen);
        }

        [Test]
        public void Refresh_Menu_HidesTheButtonAndClosesTheSheet()
        {
            _context.isSheetOpen = true;
            _context.isMenu = true;

            _sheet.Refresh();

            Assert.IsFalse(IsShown("sheet-button"));
            Assert.IsFalse(IsShown("sheet-panel"));
            Assert.IsFalse(_context.isSheetOpen);
        }

        [Test]
        public void Refresh_AnotherScreenIsCurrent_ClosesTheSheet()
        {
            _context.isSheetOpen = true;
            TestHelpers.SetPrivateField(_context.ui, "_currentView", ViewType.Map);

            _sheet.Refresh();

            Assert.IsFalse(IsShown("sheet-panel"));
            Assert.IsFalse(_context.isSheetOpen);
        }

        [Test]
        public void Refresh_Paused_HidesThePanelAndKeepsItOpenForTheResume()
        {
            _context.isSheetOpen = true;
            _context.isPaused = true;

            _sheet.Refresh();

            Assert.IsFalse(IsShown("sheet-panel"));
            Assert.IsFalse(_view.root.Q<Button>("sheet-button").enabledSelf);
            Assert.IsTrue(_context.isSheetOpen);
        }

        [Test]
        public void Refresh_InventoryOpen_DisablesTheSheetButton()
        {
            _context.isInventoryOpen = true;

            _sheet.Refresh();

            Assert.IsFalse(_view.root.Q<Button>("sheet-button").enabledSelf);
        }

        [Test]
        public void SheetButton_Submit_OpensTheSheetAndAsksTheHostToRefresh()
        {
            _sheet.Refresh();
            _changes = 0;

            ToolkitTestPanel.Submit(_view.root.Q<Button>("sheet-button"));

            Assert.IsTrue(_context.isSheetOpen);
            Assert.IsTrue(IsShown("sheet-panel"));
            Assert.AreEqual(1, _changes);
        }

        [Test]
        public void SheetButton_SubmitWhilePaused_StaysClosed()
        {
            _context.isPaused = true;
            _sheet.Refresh();

            ToolkitTestPanel.Submit(_view.root.Q<Button>("sheet-button"));

            Assert.IsFalse(_context.isSheetOpen);
        }

        [Test]
        public void CloseButton_Submit_ClosesTheSheet()
        {
            _context.isSheetOpen = true;
            _sheet.Refresh();

            ToolkitTestPanel.Submit(_view.root.Q<Button>("sheet-close-button"));

            Assert.IsFalse(_context.isSheetOpen);
            Assert.IsFalse(IsShown("sheet-panel"));
        }

        [Test]
        public void Close_SheetOpen_ClosesItWithoutTheButton()
        {
            _context.isSheetOpen = true;

            _sheet.Close();

            Assert.IsFalse(_context.isSheetOpen);
        }

        [Test]
        public void Refresh_CharacterNotInitialised_ShowsANoticeInsteadOfThrowing()
        {
            _character.attributeManager = null;
            _context.isSheetOpen = true;

            _sheet.Refresh();

            StringAssert.Contains("Character not ready", SheetText());
        }

        [Test]
        public void Refresh_JuliensFormatterThrows_LogsOnceAndShowsANotice()
        {
            // A null starting item makes his item line throw
            _character.items.Add(null);
            _context.isSheetOpen = true;
            LogAssert.Expect(LogType.Error, new Regex(@"\[ToolkitSheetPanel\] CharacterInfoPanel\.BuildBody failed"));

            _sheet.Refresh();
            _sheet.Refresh();

            StringAssert.Contains("Sheet unavailable", SheetText());
        }

        [Test]
        public void Refresh_AfterDispose_DoesNothing()
        {
            _sheet.Dispose();
            _context.isSheetOpen = true;

            Assert.DoesNotThrow(() => _sheet.Refresh());
            Assert.IsFalse(IsShown("sheet-panel"));
        }

        [Test]
        public void Init_MissingBody_LogsAndStaysInert()
        {
            _sheet.Dispose();
            _view.root.Q("sheet-body").RemoveFromHierarchy();
            LogAssert.Expect(LogType.Error, "[ToolkitTemplates] Required ScrollView 'sheet-body' is missing.");

            _sheet.Init(_context, _view, OnChanged);

            Assert.DoesNotThrow(() => _sheet.Refresh());
        }
    }
}
