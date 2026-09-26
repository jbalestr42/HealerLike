using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UI.Toolkit.Integration
{

    public class LegacyUiReaderTests
    {
        class FakeItem : AItem<BaseItemData>
        {
            public override void Equip(GameObject target) { }

            public override void Unequip(GameObject target) { }
        }

        class BareItem : AItem
        {
            public override string title { get { return "Custom item"; } }
            public override Sprite icon { get { return null; } }
            public override List<GameplayTag> tags { get { return new List<GameplayTag>(); } }
            public override void Equip(GameObject target) { }
            public override void Unequip(GameObject target) { }
        }

        GameObject _go;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("Legacy UI fixture");
            _go.SetActive(false);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void GameState_NullSource_LogsErrorAndReturnsNone()
        {
            LogAssert.Expect(LogType.Error, new Regex("GameManager.state"));

            GameManager.GameState state = LegacyUiReader.GameState(null);

            Assert.AreEqual(GameManager.GameState.None, state);
        }

        [Test]
        public void ItemDescription_GenericItem_ReadsDataDescription()
        {
            BaseItemData data = new BaseItemData { name = "Guard", description = "Protect an ally" };
            FakeItem item = new FakeItem { data = data };

            string description = LegacyUiReader.ItemDescription(item);

            Assert.AreEqual("Protect an ally", description);
            Assert.AreSame(data, item.data);
        }

        [Test]
        public void ItemDescription_NonGenericItem_RemainsValidWithoutDataContract()
        {
            BareItem item = new BareItem();

            Assert.IsEmpty(LegacyUiReader.ItemDescription(item));
            Assert.AreEqual(nameof(BareItem), DataIconDescriptor.From(item).label);
        }

        [Test]
        public void ItemDescription_NullItem_ReturnsEmpty()
        {
            string description = LegacyUiReader.ItemDescription(null);

            Assert.IsEmpty(description);
        }

        [Test]
        public void SelectedObject_NoPanelSelected_ReturnsNull()
        {
            GameView view = _go.AddComponent<GameView>();
            TestHelpers.SetPrivateField(view, "_selectedObject", _go);

            GameObject selected = LegacyUiReader.SelectedObject(view);

            Assert.IsNull(selected);
        }

        [Test]
        public void SelectedObject_EntityPanelSelected_ReturnsObject()
        {
            GameView view = _go.AddComponent<GameView>();
            TestHelpers.SetPrivateField(view, "_selectedObject", _go);
            TestHelpers.SetPrivateField(view, "_selectedPanel", PanelType.Entity);

            GameObject selected = LegacyUiReader.SelectedObject(view);

            Assert.AreSame(_go, selected);
        }

        [Test]
        public void AvailableEntities_NewInventory_ReturnsEmptyReadOnlyList()
        {
            EntityInventory inventory = _go.AddComponent<EntityInventory>();

            IReadOnlyList<SelectEntityButton> choices = LegacyUiReader.AvailableEntities(inventory);

            Assert.AreEqual(0, choices.Count);
            Assert.IsTrue(((ICollection<SelectEntityButton>)choices).IsReadOnly);
        }
    }
}
