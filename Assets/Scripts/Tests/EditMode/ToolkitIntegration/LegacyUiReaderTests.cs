using System;
using System.Collections.Generic;
using HealerLike.UI.Toolkit.Integration;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Tests.ToolkitIntegration
{
    public class LegacyUiReaderTests
    {
        sealed class Item : AItem<BaseItemData>
        {
            public override void Equip(GameObject target) { }
            public override void Unequip(GameObject target) { }
        }

        [Test]
        public void AllBindingsMatchUnmodifiedLegacyTypes()
        {
            Assert.DoesNotThrow(LegacyUiReader.ValidateContract);
        }

        [Test]
        public void NullStateSourceHasExplicitDiagnostic()
        {
            var error = Assert.Throws<ArgumentNullException>(() => LegacyUiReader.GameState(null));
            StringAssert.Contains("GameManager._state", error.Message);
        }

        [Test]
        public void DescriptionReadsExistingPublicDataWithoutChangingIt()
        {
            var data = new BaseItemData { name = "Guard", description = "Protect an ally" };
            var item = new Item { data = data };
            Assert.That(LegacyUiReader.ItemDescription(item), Is.EqualTo("Protect an ally"));
            Assert.That(item.data, Is.SameAs(data));
            Assert.That(LegacyUiReader.ItemDescription(null), Is.Empty);
        }

        [Test]
        public void HiddenEntityPanelDoesNotExposeStaleSelection()
        {
            var owner = new GameObject("Legacy selection fixture");
            owner.SetActive(false);
            try
            {
                var view = owner.AddComponent<GameView>();
                TestHelpers.SetPrivateField(view, "_selectedObject", owner);
                Assert.That(LegacyUiReader.SelectedObject(view), Is.Null);
                TestHelpers.SetPrivateField(view, "_selectedPanel", PanelType.Entity);
                Assert.That(LegacyUiReader.SelectedObject(view), Is.SameAs(owner));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void EntityChoicesCannotBeMutatedThroughAdapter()
        {
            var owner = new GameObject("Legacy inventory fixture");
            owner.SetActive(false);
            try
            {
                var inventory = owner.AddComponent<EntityInventory>();
                var choices = LegacyUiReader.AvailableEntities(inventory);
                Assert.That(((ICollection<SelectEntityButton>)choices).IsReadOnly, Is.True);
                Assert.Throws<NotSupportedException>(() => ((ICollection<SelectEntityButton>)choices).Add(null));
                Assert.That(LegacyUiReader.AvailableEntities(inventory).Count, Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }
    }
}
