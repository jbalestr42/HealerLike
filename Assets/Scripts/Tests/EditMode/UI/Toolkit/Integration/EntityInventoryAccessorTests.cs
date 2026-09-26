using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace UI.Toolkit.Integration
{
    public class EntityInventoryAccessorTests : UiAccessorFixture
    {
        [Test]
        public void Choices_CurrentContentsAreLiveReadOnlyAndCached()
        {
            EntityInventory owner = root.AddComponent<EntityInventory>();
            List<SelectEntityButton> backing = new List<SelectEntityButton>();
            TestHelpers.SetPrivateField(owner, "_entityButtons", backing);
            IReadOnlyList<SelectEntityButton> view = owner.entityButtons;
            SelectEntityButton choice = root.AddComponent<SelectEntityButton>();

            backing.Add(choice);

            Assert.AreSame(view, owner.entityButtons);
            Assert.AreSame(choice, view[0]);
            Assert.Throws<System.NotSupportedException>(() => ((ICollection<SelectEntityButton>)view).Clear());
            backing.Clear();
            Assert.IsEmpty(view);
        }

        [Test]
        public void Choices_ReplacedBackingList_RebuildsViewWithoutEditingOldList()
        {
            EntityInventory owner = root.AddComponent<EntityInventory>();
            IReadOnlyList<SelectEntityButton> oldView = owner.entityButtons;
            SelectEntityButton choice = root.AddComponent<SelectEntityButton>();
            TestHelpers.SetPrivateField(owner, "_entityButtons", new List<SelectEntityButton> { choice });

            IReadOnlyList<SelectEntityButton> current = owner.entityButtons;

            Assert.AreNotSame(oldView, current);
            Assert.IsEmpty(oldView);
            Assert.AreSame(choice, current[0]);
        }
    }
}
