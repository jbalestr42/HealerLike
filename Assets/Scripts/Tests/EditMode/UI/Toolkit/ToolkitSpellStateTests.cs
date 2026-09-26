using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UI.Toolkit.Integration;

namespace UI.Toolkit
{
    public class ToolkitSpellStateTests : UiAccessorFixture
    {
        [Test]
        public void NumericCostUsesActualValidatorEnablementAndRealMana()
        {
            Character character = null;
            TestHelpers.WithLoggingDisabled(() => character = root.AddComponent<Character>());
            TestHelpers.SetPrivateField(character, "_mana", TestHelpers.CreateResourceAttribute(root, AttributeType.ManaMax, 12));
            var resource = ScriptableObject.CreateInstance<ResourceValidatorFactory>();
            var consumer = ScriptableObject.CreateInstance<ConsumerFactory>();
            try
            {
                consumer.data = new ConsumerData { value = new FlatValue { data = new FlatValueData { value = 17 } } };
                resource.data = new ResourceValidatorData { consumer = consumer };
                var slot = root.AddComponent<CharacterSkillSlot>();
                var button = root.AddComponent<UseCharacterSkillButton>();
                TestHelpers.SetPrivateField(button, "_button", CreateChild<Button>());
                TestHelpers.SetPrivateField(button, "_nameText", CreateChild<Text>());
                slot.Init(new ApplyConsumerCharacterSkill { data = new ApplyConsumerCharacterSkillData
                    { name = "Authored cost", validators = new List<ACharacterSkillValidatorFactory> { resource } } }, button, false);
                Assert.That(ToolkitSpellState.Read(slot, character).cost, Is.Zero, "Unrestricted slot has no active cost validator");
                button.hasCost = true;
                var state = ToolkitSpellState.Read(slot, character);
                Assert.That(state.cost, Is.EqualTo(17));
                Assert.That(state.insufficientMana, Is.True);
                Assert.That(state.remaining, Is.Zero, "Mana shortage is distinct from cooldown");
            }
            finally { Object.DestroyImmediate(resource); Object.DestroyImmediate(consumer); }
        }
        [Test]
        public void ReadsNumericCooldownEvenWhenLabelsAreAbsentAndUnavailableRemainsDescribed()
        {
            Character character = null;
            TestHelpers.WithLoggingDisabled(() => character = root.AddComponent<Character>());
            var slot = root.AddComponent<CharacterSkillSlot>();
            var button = root.AddComponent<UseCharacterSkillButton>();
            var action = CreateChild<Button>();
            TestHelpers.SetPrivateField(button, "_button", action);
            TestHelpers.SetPrivateField(button, "_nameText", CreateChild<Text>());
            var fill = CreateChild<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillAmount = .65f;
            button.hasCooldown = true;
            var skill = new ApplyConsumerCharacterSkill { data = new ApplyConsumerCharacterSkillData
                { name = "Heal", validators = new List<ACharacterSkillValidatorFactory>() } };
            slot.Init(skill, button, false);
            action.interactable = false;
            var state = ToolkitSpellState.Read(slot, character);
            Assert.That(state.remaining, Is.EqualTo(.65f).Within(.001f));
            Assert.That(state.canUse, Is.False);
            Assert.That(state.cost, Is.Zero);
            Assert.That(slot.data.name, Is.EqualTo("Heal"));
        }
    }
}
