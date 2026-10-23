using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Toolkit
{
    // Editor only: Kill wave and the item catalog beside Start battle, a tap on an item gives a new copy of it
    public class ToolkitEditorToolsTests
    {
        ToolkitTestRun _run;
        ToolkitTestPanel _panel;
        ToolkitGameView _view;
        ToolkitEditorTools _tools;
        GameObject _characterObject;
        GameData _data;
        readonly List<Object> _objects = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            _run = new ToolkitTestRun();
            _characterObject = new GameObject("Cleric");
            Character character = null;
            TestHelpers.WithLoggingDisabled(() => character = _characterObject.AddComponent<Character>());
            _run.player.character = character;

            GameplayTag playerTag = Track(ScriptableObject.CreateInstance<GameplayTag>());
            playerTag.name = TagNames.Player;
            _data = Track(ScriptableObject.CreateInstance<GameData>());
            _data.items = new List<AItemFactory> { CreateItem("Ward"), CreateItem("Rosary", playerTag) };
            _run.context.data = _data;

            _panel = new ToolkitTestPanel();
            VisualElement root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
            _panel.root.Add(root);
            _view = new ToolkitGameView(root);
            _tools = new ToolkitEditorTools();
            _tools.Init(_run.context, _view);
        }

        [TearDown]
        public void TearDown()
        {
            _tools.Dispose();
            _view.Release();
            _panel.Dispose();
            _run.Dispose();
            Object.DestroyImmediate(_characterObject);
            foreach (Object obj in _objects)
            {
                Object.DestroyImmediate(obj);
            }

            _objects.Clear();
        }

        T Track<T>(T obj) where T : Object
        {
            _objects.Add(obj);
            return obj;
        }

        ItemFactory CreateItem(string name, params GameplayTag[] tags)
        {
            ItemFactory factory = Track(ScriptableObject.CreateInstance<ItemFactory>());
            factory.data = new ItemData { name = name, description = name + " description", tags = new List<GameplayTag>(tags) };
            return factory;
        }

        bool IsHidden(string name)
        {
            return _view.root.Q(name).ClassListContains("is-hidden");
        }

        ToolkitCardModel FirstCard(string listName)
        {
            return (ToolkitCardModel)_view.root.Q(listName).Q<Button>("data-card").userData;
        }

        [Test]
        public void Init_AddsTheButtonsAndTheHiddenCatalog()
        {
            Assert.IsNotNull(_view.root.Q("encounter-actions").Q<Button>(ToolkitEditorTools.KillWaveName));
            Assert.IsNotNull(_view.root.Q("encounter-actions").Q<Button>(ToolkitEditorTools.ItemsName));
            Assert.IsNotNull(_view.root.Q(ToolkitEditorTools.PanelName));
        }

        [Test]
        public void Refresh_OnTheBoardOfARun_ShowsTheButtonsAndTheToolbar()
        {
            _view.Show("field-toolbar", false);

            _tools.Refresh();

            Assert.IsFalse(IsHidden(ToolkitEditorTools.KillWaveName));
            Assert.IsFalse(IsHidden(ToolkitEditorTools.ItemsName));
            Assert.IsFalse(IsHidden("field-toolbar"), "Kill wave is reached in battle too");
        }

        [Test]
        public void Refresh_OutsideARun_HidesTheButtons()
        {
            _run.context.ascension = null;

            _tools.Refresh();

            Assert.IsTrue(IsHidden(ToolkitEditorTools.KillWaveName));
            Assert.IsTrue(IsHidden(ToolkitEditorTools.ItemsName));
        }

        [Test]
        public void Toggle_SplitsTheUnitItemsFromTheHealerItems()
        {
            _tools.Toggle();

            Assert.IsTrue(_tools.isOpen);
            Assert.IsFalse(IsHidden(ToolkitEditorTools.PanelName));
            Assert.AreEqual("Ward", FirstCard(ToolkitEditorTools.UnitListName).title);
            Assert.AreEqual("Rosary", FirstCard(ToolkitEditorTools.HealerListName).title);
        }

        [Test]
        public void Toggle_Twice_ClosesTheCatalog()
        {
            _tools.Toggle();

            _tools.Toggle();

            Assert.IsFalse(_tools.isOpen);
            Assert.IsTrue(IsHidden(ToolkitEditorTools.PanelName));
        }

        [Test]
        public void TapUnitItem_PutsANewCopyInTheStash()
        {
            _tools.Toggle();
            ToolkitCardModel card = FirstCard(ToolkitEditorTools.UnitListName);

            card.activate.Invoke(card);
            card.activate.Invoke(card);

            Assert.AreEqual(2, _run.player.stash.items.Count);
            Assert.AreEqual("Ward", _run.player.stash.items[0].item.title);
            Assert.AreNotSame(card.source, _run.player.stash.items[0].item, "The catalog keeps its own");
            Assert.AreNotSame(_run.player.stash.items[0].item, _run.player.stash.items[1].item);
        }

        [Test]
        public void TapHealerItem_GivesANewCopyToTheHealer()
        {
            _tools.Toggle();
            ToolkitCardModel card = FirstCard(ToolkitEditorTools.HealerListName);

            card.activate.Invoke(card);

            Assert.AreEqual(1, _run.player.character.inventoryHandler.items.Count);
            Assert.AreEqual("Rosary", _run.player.character.inventoryHandler.items[0].item.title);
            Assert.IsEmpty(_run.player.stash.items);
        }

        [Test]
        public void Dispose_RemovesWhatItAdded()
        {
            _tools.Dispose();

            Assert.IsNull(_view.root.Q(ToolkitEditorTools.KillWaveName));
            Assert.IsNull(_view.root.Q(ToolkitEditorTools.PanelName));
        }
    }
}
