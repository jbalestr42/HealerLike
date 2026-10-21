using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace UI.Toolkit.Integration
{
    public class UpgradeViewAccessorTests : UiAccessorFixture
    {
        [Test]
        public void Choices_CurrentContentsAreLiveReadOnlyAndCached()
        {
            UpgradeView owner = root.AddComponent<UpgradeView>();
            List<GameObject> backing = new List<GameObject>();
            TestHelpers.SetPrivateField(owner, "_upgradeButtons", backing);
            IReadOnlyList<GameObject> view = owner.upgradeButtons;
            GameObject choice = root;

            backing.Add(choice);

            Assert.AreSame(view, owner.upgradeButtons);
            Assert.AreSame(choice, view[0]);
            Assert.Throws<System.NotSupportedException>(() => ((ICollection<GameObject>)view).Clear());
            backing.Clear();
            Assert.IsEmpty(view);
        }

        [Test]
        public void Choices_ReplacedBackingList_RebuildsViewWithoutEditingOldList()
        {
            UpgradeView owner = root.AddComponent<UpgradeView>();
            IReadOnlyList<GameObject> oldView = owner.upgradeButtons;
            GameObject choice = root;
            TestHelpers.SetPrivateField(owner, "_upgradeButtons", new List<GameObject> { choice });

            IReadOnlyList<GameObject> current = owner.upgradeButtons;

            Assert.AreNotSame(oldView, current);
            Assert.IsEmpty(oldView);
            Assert.AreSame(choice, current[0]);
        }
    }
}
