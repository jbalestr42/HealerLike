using NUnit.Framework;
using UnityEngine;

namespace UI.Toolkit.Integration
{
    public class CharacterSkillSlotAccessorTests : UiAccessorFixture
    {
        [Test]
        public void SkillButton_InitRetainsTheExistingPresentation()
        {
            CharacterSkillSlot slot = root.AddComponent<CharacterSkillSlot>();
            UseCharacterSkillButton button = root.AddComponent<UseCharacterSkillButton>();
            TestHelpers.SetPrivateField(button, "_button", CreateChild<UnityEngine.UI.Button>());
            TestHelpers.SetPrivateField(button, "_nameText", CreateChild<UnityEngine.UI.Text>());
            ApplyConsumerCharacterSkill skill = new ApplyConsumerCharacterSkill();
            skill.data = new ApplyConsumerCharacterSkillData { name = "Heal" };

            slot.Init(skill, button, false);

            Assert.AreSame(button, slot.skillButton);
            Assert.AreSame(skill.data, slot.skillButton.data);
        }
    }
}
