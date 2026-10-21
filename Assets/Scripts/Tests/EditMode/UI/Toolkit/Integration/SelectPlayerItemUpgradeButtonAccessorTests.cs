using NUnit.Framework;
using UnityEngine;

namespace UI.Toolkit.Integration
{
    public class SelectPlayerItemUpgradeButtonAccessorTests : UiAccessorFixture
    {
        [Test]
        public void Item_ReplacedChoiceExposesTheExactCurrentInstance()
        {
            SelectPlayerItemUpgradeButton button = root.AddComponent<SelectPlayerItemUpgradeButton>();
            Item first = new Item { data = new ItemData { name = "Ward" } };
            Item second = new Item { data = new ItemData { name = "Bloom" } };

            TestHelpers.SetPrivateField(button, "_item", first);
            Assert.AreSame(first, button.item);
            TestHelpers.SetPrivateField(button, "_item", second);

            Assert.AreSame(second, button.item);
        }
    }
}
