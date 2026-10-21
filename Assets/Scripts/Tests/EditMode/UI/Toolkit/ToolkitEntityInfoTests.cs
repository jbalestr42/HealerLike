using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace UI.Toolkit
{
    // The sections of the detail panel, before any element shows them
    public class ToolkitEntityInfoTests
    {
        class StubItem : AItem
        {
            readonly string _title;
            readonly string _description;

            public StubItem(string title, string description)
            {
                _title = title;
                _description = description;
            }

            public override void Equip(GameObject target) { }
            public override void Unequip(GameObject target) { }
            public override string title => _title;
            public override string description => _description;
            public override Sprite icon => null;
            public override List<GameplayTag> tags => new List<GameplayTag>();
        }

        readonly TestUnits _units = new TestUnits();
        readonly List<Object> _objects = new List<Object>();
        Entity _entity;

        [SetUp]
        public void SetUp()
        {
            _entity = _units.Create(63f, 100f, "Zealot");
        }

        [TearDown]
        public void TearDown()
        {
            _units.DestroyAll();
            foreach (Object obj in _objects)
            {
                Object.DestroyImmediate(obj);
            }

            _objects.Clear();
        }

        [Test]
        public void GetHealthLine_DamagedCreature_ShowsValueAndPercent()
        {
            Assert.That(ToolkitEntityInfo.GetHealthLine(_entity), Is.EqualTo("Health: 63 / 100 (63 %)"));
        }

        [Test]
        public void GetHealthLine_HealthCanNotBeConsumed_FlagsInvulnerable()
        {
            _entity.health.preventConsumers = true;

            Assert.That(ToolkitEntityInfo.GetHealthLine(_entity), Does.EndWith("Invulnerable"));
        }

        [Test]
        public void GetHealthLine_ZeroMaximum_ShowsZeroPercent()
        {
            Entity empty = _units.Create(0f, 0f, "Empty");

            Assert.That(ToolkitEntityInfo.GetHealthLine(empty), Does.Contain("(0 %)"));
        }

        [Test]
        public void GetItems_BornWithAndEquipped_GroupsEachUnderItsHeading()
        {
            StubItem born = new StubItem("Zeal", "+50% damage");
            StubItem equipped = new StubItem("Iron Plating", "+3 flat armor");
            _entity.items.Add(born);
            _entity.inventoryHandler.AddItem(equipped, -1);

            ToolkitInfoSection section = ToolkitEntityInfo.GetItems(_entity);

            Assert.That(section.count, Is.EqualTo(2));
            Assert.That(section.lines, Has.Count.EqualTo(4));
            Assert.That(section.lines[0], Does.Contain("Born with"));
            Assert.That(section.lines[1], Is.EqualTo(EntityInfoFormatter.FormatItemDetails(born)));
            Assert.That(section.lines[2], Does.Contain("Equipped"));
            Assert.That(section.lines[3], Is.EqualTo(EntityInfoFormatter.FormatItemDetails(equipped)));
        }

        [Test]
        public void GetItems_NoItem_ReturnsEmptySection()
        {
            ToolkitInfoSection section = ToolkitEntityInfo.GetItems(_entity);

            Assert.That(section.lines, Is.Empty);
            Assert.That(section.count, Is.EqualTo(0));
        }

        [Test]
        public void GetSkills_SkillMissingFromEntitySkills_FlagsItAsItem()
        {
            global::Buff.FakeSkill skill = _entity.gameObject.AddComponent<global::Buff.FakeSkill>();

            ToolkitInfoSection section = ToolkitEntityInfo.GetSkills(_entity);

            Assert.That(section.count, Is.EqualTo(1));
            Assert.That(section.lines[0], Is.EqualTo(EntityInfoFormatter.FormatSkill(skill)
                + $" <color={EntityInfoFormatter.MutedColor}>(item)</color>"));
        }

        [Test]
        public void GetSkills_SkillOfTheEntity_IsNotFlagged()
        {
            global::Buff.FakeSkill skill = _entity.gameObject.AddComponent<global::Buff.FakeSkill>();
            _entity.skills.Add(skill);

            ToolkitInfoSection section = ToolkitEntityInfo.GetSkills(_entity);

            Assert.That(section.lines, Is.EqualTo(new[] { EntityInfoFormatter.FormatSkill(skill) }));
        }

        [Test]
        public void GetSkills_NoSkill_ReturnsEmptySection()
        {
            Assert.That(ToolkitEntityInfo.GetSkills(_entity).lines, Is.Empty);
        }

        [Test]
        public void GetEffects_SameOnHitEffectTwice_GroupsItWithItsCount()
        {
            BuffHandlerFactory poison = ScriptableObject.CreateInstance<BuffHandlerFactory>();
            poison.name = "Poison";
            _objects.Add(poison);
            _entity.AddOnHitEffect(poison);
            _entity.AddOnHitEffect(poison);

            ToolkitInfoSection section = ToolkitEntityInfo.GetEffects(_entity);

            Assert.That(section.count, Is.EqualTo(1));
            Assert.That(section.lines[0], Does.Contain("On hit"));
            Assert.That(section.lines[1], Is.EqualTo("Applies: Poison ×2"));
        }

        [Test]
        public void GetEffects_NothingActiveOrOnHit_ReturnsEmptySection()
        {
            Assert.That(ToolkitEntityInfo.GetEffects(_entity).lines, Is.Empty);
        }

        [Test]
        public void GetTargeting_WithoutTargetProvider_ReturnsEmptySection()
        {
            Assert.That(ToolkitEntityInfo.GetTargeting(_entity).lines, Is.Empty);
        }

        [Test]
        public void GetAttributeLines_WithDescription_StartsWithItThenTheFormatterLines()
        {
            GameObject holder = new GameObject("Attributes");
            _objects.Add(holder);
            _entity.attributeManager = TestHelpers.CreateAttributeManager(holder, AttributeType.Damage, 12f);
            EntityData data = ScriptableObject.CreateInstance<EntityData>();
            data.description = "Tough";
            _objects.Add(data);
            _entity.data = data;

            List<string> lines = ToolkitEntityInfo.GetAttributeLines(_entity);

            Assert.That(lines[0], Is.EqualTo("<i>Tough</i>"));
            Assert.That(lines.GetRange(1, lines.Count - 1),
                Is.EqualTo(EntityInfoFormatter.GetAttributeLines(_entity.attributeManager, _entity.gameObject)));
        }

        [Test]
        public void GetAttributeLines_WithoutAttributeManagerOrData_ReturnsEmpty()
        {
            Assert.That(ToolkitEntityInfo.GetAttributeLines(_entity), Is.Empty);
        }
    }
}
