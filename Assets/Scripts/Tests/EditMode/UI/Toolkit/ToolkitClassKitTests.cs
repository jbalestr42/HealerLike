using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UI.Toolkit
{
    // The class card's kit: the stats and tooltips read off a built preview, and that the preview never stays behind
    public class ToolkitClassKitTests
    {
        readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        T Create<T>() where T : ScriptableObject
        {
            T created = ScriptableObject.CreateInstance<T>();
            _created.Add(created);
            return created;
        }

        ApplyConsumerCharacterSkillFactory Skill(string name, string description = "")
        {
            ApplyConsumerCharacterSkillFactory skill = Create<ApplyConsumerCharacterSkillFactory>();
            skill.data = new ApplyConsumerCharacterSkillData { name = name, description = description };
            return skill;
        }

        EntityData Unit(string title, string description = "")
        {
            EntityData entity = Create<EntityData>();
            entity.title = title;
            entity.description = description;
            return entity;
        }

        // A starting item adding Heal Power, like the Sacred Tome
        AItemFactory HealPowerItem(float healPower)
        {
            FlatModifierFactory modifier = Create<FlatModifierFactory>();
            modifier.data = new FlatModifierData { type = AttributeType.HealPower, modifierType = AttributeModifierType.Add, value = healPower };
            BuffHandlerFactory handler = Create<BuffHandlerFactory>();
            handler.data = new BuffHandlerData { durationType = DurationType.Infinite, buffFactoryList = new List<ABuffFactory> { modifier } };
            ItemFactory item = Create<ItemFactory>();
            item.data = new ItemData { name = "Sacred Tome", description = "+" + healPower + " Heal Power", buffs = new List<ABuffHandlerFactory> { handler } };
            return item;
        }

        CharacterData Character(string title)
        {
            CharacterData character = Create<CharacterData>();
            character.title = title;
            character.text = title + " description";
            character.skills = new List<ACharacterSkillFactory>();
            character.entities = new List<EntityData>();
            character.items = new List<AItemFactory>();
            character.attributes = new Dictionary<AttributeType, float> { { AttributeType.ManaMax, 100f }, { AttributeType.HealPower, 20f } };
            return character;
        }

        static int PreviewCount()
        {
            return Object.FindObjectsByType<global::Character>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        }

        [Test]
        public void Build_StartingItem_ReadsTheStatsWithItsBuffsApplied()
        {
            CharacterData cleric = Character("Cleric");
            cleric.items = new List<AItemFactory> { HealPowerItem(10f) };

            ToolkitClassKit kit = ToolkitClassKit.Build(cleric);

            Assert.AreEqual("- Max Mana: 100\n- Heal Power: 30", kit.stats);
        }

        [Test]
        public void Build_SkillValue_ResolvesAgainstThePreviewWithItsItems()
        {
            CharacterData cleric = Character("Cleric");
            cleric.items = new List<AItemFactory> { HealPowerItem(10f) };
            cleric.skills = new List<ACharacterSkillFactory> { Skill("Heal", "Heals for [{attribute:current:HealPower}x1.5]") };

            ToolkitClassKit kit = ToolkitClassKit.Build(cleric);

            Assert.AreEqual(1, kit.skills.Count);
            Assert.AreEqual("Heal", kit.skills[0].title);
            Assert.AreEqual("Heals for 45", kit.skills[0].body, "The name is the popover's title, so the body starts after it.");
            Assert.IsFalse(kit.skills[0].showLabel, "A skill is its icon alone.");
        }

        [Test]
        public void Build_SkillWithoutDescription_HasAnEmptyBody()
        {
            CharacterData cleric = Character("Cleric");
            cleric.skills = new List<ACharacterSkillFactory> { Skill("Heal") };

            ToolkitClassKit kit = ToolkitClassKit.Build(cleric);

            Assert.AreEqual("", kit.skills[0].body);
        }

        [Test]
        public void Build_Skill_IconSourceIsTheSkillDataTheSpellBarUses()
        {
            CharacterData cleric = Character("Cleric");
            ApplyConsumerCharacterSkillFactory heal = Skill("Heal");
            cleric.skills = new List<ACharacterSkillFactory> { heal };

            ToolkitClassKit kit = ToolkitClassKit.Build(cleric);

            Assert.AreSame(heal.data, kit.skills[0].iconSource);
        }

        [Test]
        public void Build_Unit_KeepsItsDataAsIconSourceAndItsNameBesideTheIcon()
        {
            CharacterData cleric = Character("Cleric");
            EntityData zealot = Unit("Zealot", "Hits harder when healthy");
            cleric.entities = new List<EntityData> { zealot };

            ToolkitClassKit kit = ToolkitClassKit.Build(cleric);

            Assert.AreEqual(1, kit.units.Count);
            Assert.AreSame(zealot, kit.units[0].iconSource);
            Assert.AreEqual("Zealot", kit.units[0].title);
            StringAssert.Contains("Hits harder when healthy", kit.units[0].body);
            StringAssert.DoesNotContain("<b>Zealot</b>", kit.units[0].body);
            Assert.IsTrue(kit.units[0].showLabel);
        }

        [Test]
        public void Build_Class_LeavesNoPreviewCharacterBehind()
        {
            CharacterData cleric = Character("Cleric");
            cleric.items = new List<AItemFactory> { HealPowerItem(10f) };
            int before = PreviewCount();

            ToolkitClassKit.Build(cleric);

            Assert.AreEqual(before, PreviewCount());
            Assert.IsNull(GameObject.Find("Cleric Preview"));
        }

        [Test]
        public void Build_EmptyEntries_AreSkipped()
        {
            CharacterData cleric = Character("Cleric");
            cleric.skills = new List<ACharacterSkillFactory> { null, Skill("Heal") };
            cleric.entities = new List<EntityData> { null, Unit("Zealot") };

            ToolkitClassKit kit = ToolkitClassKit.Build(cleric);

            Assert.AreEqual(1, kit.skills.Count);
            Assert.AreEqual(1, kit.units.Count);
            Assert.AreEqual(2, kit.Entries().Count);
            Assert.AreSame(kit.skills[0], kit.Entries()[0], "Skills lead, units follow.");
        }

        [Test]
        public void Build_StartingItemListWithAnEmptyEntry_SkipsThePreviewAndLogs()
        {
            CharacterData cleric = Character("Cleric");
            cleric.items = new List<AItemFactory> { null };
            cleric.skills = new List<ACharacterSkillFactory> { Skill("Heal") };
            LogAssert.Expect(LogType.Error, new Regex(@"\[ToolkitClassKit\] 'Cleric'"));
            int before = PreviewCount();

            ToolkitClassKit kit = ToolkitClassKit.Build(cleric);

            Assert.AreEqual("", kit.stats);
            Assert.AreEqual(1, kit.skills.Count, "The skills still list, without values from a preview.");
            Assert.AreEqual(before, PreviewCount());
        }

        [Test]
        public void Build_NoAttributeTable_SkipsThePreviewAndLogs()
        {
            CharacterData cleric = Character("Cleric");
            cleric.attributes = null;
            LogAssert.Expect(LogType.Error, new Regex(@"\[ToolkitClassKit\] 'Cleric'"));

            ToolkitClassKit kit = ToolkitClassKit.Build(cleric);

            Assert.AreEqual("", kit.stats);
        }

        [Test]
        public void Build_NoMaximumManaInTheTable_SkipsThePreviewInsteadOfThrowing()
        {
            CharacterData cleric = Character("Cleric");
            cleric.attributes.Remove(AttributeType.ManaMax);
            int before = PreviewCount();
            LogAssert.Expect(LogType.Warning, new Regex(@"\[ToolkitClassKit\] 'Cleric'"));

            ToolkitClassKit kit = ToolkitClassKit.Build(cleric);

            Assert.AreEqual("", kit.stats);
            Assert.AreEqual(before, PreviewCount());
        }

        [Test]
        public void Build_NoCharacter_IsEmpty()
        {
            ToolkitClassKit kit = ToolkitClassKit.Build(null);

            Assert.AreEqual("", kit.stats);
            CollectionAssert.IsEmpty(kit.Entries());
        }

        [Test]
        public void TooltipBody_LeadingName_IsDropped()
        {
            Assert.AreEqual("Heals", ToolkitClassKit.TooltipBody("<b>Heal</b>\nHeals", "Heal"));
            Assert.AreEqual("Hits\n\n- Max HP: 120", ToolkitClassKit.TooltipBody("<b>Zealot</b>\nHits\n\n- Max HP: 120", "Zealot"));
        }

        [Test]
        public void TooltipBody_NameOnly_IsEmpty()
        {
            Assert.AreEqual("", ToolkitClassKit.TooltipBody("<b>Heal</b>", "Heal"));
        }

        [Test]
        public void TooltipBody_OtherStart_IsKeptWhole()
        {
            Assert.AreEqual("Heals a lot", ToolkitClassKit.TooltipBody("Heals a lot", "Heal"));
            Assert.AreEqual("", ToolkitClassKit.TooltipBody(null, "Heal"));
        }
    }
}
