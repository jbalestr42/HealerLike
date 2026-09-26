using NUnit.Framework;
using UnityEngine;

namespace UI.Toolkit.Integration
{
    public class SelectItemUpgradeButtonAccessorTests : UiAccessorFixture
    {
        [Test]
        public void Item_ReplacedChoiceExposesTheExactCurrentInstance()
        {
            SelectItemUpgradeButton button = root.AddComponent<SelectItemUpgradeButton>();
            Item first = new Item { data = new ItemData { name = "Ward" } };
            Item second = new Item { data = new ItemData { name = "Bloom" } };

            TestHelpers.SetPrivateField(button, "_item", first);
            Assert.AreSame(first, button.item);
            TestHelpers.SetPrivateField(button, "_item", second);

            Assert.AreSame(second, button.item);
        }
    }
}
