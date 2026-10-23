using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UI.Toolkit.Integration;

namespace UI.Toolkit
{
    public class ToolkitSpellStateTests : UiAccessorFixture
    {
        // A skill costing 17 mana for a character with 12
        CharacterSkillSlot CreateCostingSlot(Character character, bool useValidators, List<Object> created)
        {
            ResourceValidatorFactory resource = ScriptableObject.CreateInstance<ResourceValidatorFactory>();
            ConsumerFactory consumer = ScriptableObject.CreateInstance<ConsumerFactory>();
            created.Add(resource);
            created.Add(consumer);
            consumer.data = new ConsumerData { value = new FlatValue { data = new FlatValueData { value = 17 } } };
            resource.data = new ResourceValidatorData { consumer = consumer, useCostMultiplier = false };
            CharacterSkillSlot slot = root.AddComponent<CharacterSkillSlot>();
            slot.Init(new ApplyConsumerCharacterSkill { data = new ApplyConsumerCharacterSkillData
                { name = "Authored cost", validators = new List<ACharacterSkillValidatorFactory> { resource } } }, useValidators);
            return slot;
        }

        Character CreateCharacter(float mana)
        {
            Character character = null;
            TestHelpers.WithLoggingDisabled(() => character = root.AddComponent<Character>());
            TestHelpers.SetPrivateField(character, "_mana", TestHelpers.CreateResourceAttribute(root, ResourceType.Mana, AttributeType.ManaMax, mana));
            return character;
        }

        [Test]
        public void Read_CostOfTheSkill_ShortOfManaWithoutCooldown()
        {
            List<Object> created = new List<Object>();
            try
            {
                Character character = CreateCharacter(12);
                CharacterSkillSlot slot = CreateCostingSlot(character, true, created);

                ToolkitSpellState state = ToolkitSpellState.Read(slot, character);

                Assert.That(state.cost, Is.EqualTo(17));
                Assert.That(state.insufficientMana, Is.True);
                Assert.That(state.canUse, Is.False);
                Assert.That(state.remaining, Is.Zero, "Mana shortage is distinct from cooldown");
            }
            finally { created.ForEach(Object.DestroyImmediate); }
        }

        [Test]
        public void Read_UnrestrictedSlot_HasNoCost()
        {
            List<Object> created = new List<Object>();
            try
            {
                Character character = CreateCharacter(12);
                CharacterSkillSlot slot = CreateCostingSlot(character, false, created);

                ToolkitSpellState state = ToolkitSpellState.Read(slot, character);

                Assert.That(state.cost, Is.Zero);
                Assert.That(state.insufficientMana, Is.False);
            }
            finally { created.ForEach(Object.DestroyImmediate); }
        }

        [Test]
        public void Read_Cooldown_IsTheShareLeft()
        {
            Character character = CreateCharacter(12);
            CharacterSkillSlot slot = root.AddComponent<CharacterSkillSlot>();
            slot.Init(new ApplyConsumerCharacterSkill { data = new ApplyConsumerCharacterSkillData
                { name = "Heal", validators = new List<ACharacterSkillValidatorFactory>() } });
            slot.state.hasCooldown = true;
            slot.state.cooldown = 6.5f;
            slot.state.cooldownDuration = 10f;
            slot.isEnabled = false;

            ToolkitSpellState state = ToolkitSpellState.Read(slot, character);

            Assert.That(state.remaining, Is.EqualTo(0.65f).Within(0.001f));
            Assert.That(state.duration, Is.EqualTo(10f));
            Assert.That(state.canUse, Is.False);
        }
    }
}
