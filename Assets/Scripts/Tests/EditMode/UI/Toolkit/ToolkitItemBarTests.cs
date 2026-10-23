using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Toolkit
{
    // The healer's items as icons: starting ones then won ones, outlined by their style, explained in the popover
    public class ToolkitItemBarTests
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

        ToolkitTestRun _run;
        GameObject _characterObject;
        Character _character;
        ItemStyle _style;

        [SetUp]
        public void SetUp()
        {
            _run = new ToolkitTestRun();
            _characterObject = new GameObject("Cleric");
            TestHelpers.WithLoggingDisabled(() => _character = _characterObject.AddComponent<Character>());
            _run.player.character = _character;
            _style = ScriptableObject.CreateInstance<ItemStyle>();
            _style.label = "Ascension";
            _style.outlineColor = Color.red;
        }

        [TearDown]
        public void TearDown()
        {
            _run.Dispose();
            Object.DestroyImmediate(_characterObject);
            Object.DestroyImmediate(_style);
        }

        [Test]
        public void GetEntries_StartingThenWonItems_InThatOrder()
        {
            _character.items.Add(new StubItem("Prayer Book", ""));
            _character.inventoryHandler.AddItem(new StubItem("Rosary", ""), -1);

            List<ToolkitKitEntry> entries = ToolkitItemBar.GetEntries(_character, null);

            CollectionAssert.AreEqual(new[] { "Prayer Book", "Rosary" }, entries.ConvertAll(entry => entry.title));
        }

        [Test]
        public void GetEntry_RegularItem_NoOutlineNoBadge()
        {
            ToolkitKitEntry entry = ToolkitItemBar.GetEntry(new StubItem("Rosary", "Every fifth spell is free."), null, null);

            Assert.IsNull(entry.outline);
            Assert.IsNull(entry.badge);
            Assert.AreEqual("Every fifth spell is free.", entry.body);
        }

        [Test]
        public void GetEntry_StyledItem_OutlinedInItsColor()
        {
            ToolkitKitEntry entry = ToolkitItemBar.GetEntry(new StubItem("Heavy Chains", ""), _style, null);

            Assert.AreEqual(Color.red, entry.outline);
        }

        [Test]
        public void GetBody_StyledItem_ItsLabelInItsColorThenTheDescription()
        {
            string body = ToolkitItemBar.GetBody(new StubItem("Heavy Chains", "Slower spells."), _style);

            Assert.AreEqual("<color=#FF0000>Ascension</color>\nSlower spells.", body);
        }

        [Test]
        public void Refresh_ShowsOneChipPerItem()
        {
            _character.items.Add(new StubItem("Prayer Book", ""));
            _character.inventoryHandler.AddItem(new StubItem("Rosary", ""), -1);
            ToolkitTestPanel panel = new ToolkitTestPanel();
            VisualElement root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
            panel.root.Add(root);
            ToolkitGameView view = new ToolkitGameView(root);
            ToolkitItemBar bar = new ToolkitItemBar();
            try
            {
                bar.Init(_run.context, view);

                bar.Refresh(true);

                VisualElement element = root.Q(ToolkitItemBar.BarName);
                Assert.AreEqual(2, element.Query(className: ToolkitKitRow.ChipClass).ToList().Count);
                Assert.AreEqual(DisplayStyle.Flex, element.style.display.value);

                bar.Refresh(false);

                Assert.AreEqual(DisplayStyle.None, element.style.display.value, "Hidden on the menu");
            }
            finally
            {
                bar.Dispose();
                view.Release();
                panel.Dispose();
            }
        }
    }
}
