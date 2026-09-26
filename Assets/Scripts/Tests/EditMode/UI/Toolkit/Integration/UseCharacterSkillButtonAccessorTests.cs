using NUnit.Framework;
using UnityEngine;

namespace UI.Toolkit.Integration
{
    public class UseCharacterSkillButtonAccessorTests : UiAccessorFixture
    {
        [Test]
        public void Presentation_UnwiredButton_ReturnsNullText()
        {
            UseCharacterSkillButton button = root.AddComponent<UseCharacterSkillButton>();

            Assert.IsNull(button.costText);
            Assert.IsNull(button.cooldownText);
        }

        [Test]
        public void Presentation_ExistingSettersProduceObservedText()
        {
            UseCharacterSkillButton button = root.AddComponent<UseCharacterSkillButton>();
            UnityEngine.UI.Text cost = CreateChild<UnityEngine.UI.Text>();
            UnityEngine.UI.Text cooldown = CreateChild<UnityEngine.UI.Text>();
            TestHelpers.SetPrivateField(button, "_costText", cost);
            TestHelpers.SetPrivateField(button, "_cooldownText", cooldown);
            TestHelpers.SetPrivateField(button, "_cooldownImage", CreateChild<UnityEngine.UI.Image>());
            button.hasCost = true;
            button.hasCooldown = true;

            button.SetCost(4f);
            button.SetCooldown(3f, 6f);

            Assert.AreEqual("4", button.costText);
            Assert.AreEqual("3s", button.cooldownText);
            button.hasCost = false;
            button.SetCost(4f);
            button.SetCooldown(0f, 6f);
            Assert.IsEmpty(button.costText);
            Assert.IsEmpty(button.cooldownText);
        }
    }
}
