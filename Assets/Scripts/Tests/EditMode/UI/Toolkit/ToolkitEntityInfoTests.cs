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

        [Test]
        public void GetSideLine_AllyBeingPlaced_SaysAllyAndPlacement()
        {
            _entity.entityType = Entity.EntityType.Player;
            TestHelpers.SetPrivateField(_entity, "_isDraggable", true);

            Assert.That(ToolkitEntityInfo.GetSideLine(_entity), Is.EqualTo("Ally · placement"));
        }

        [Test]
        public void GetSideLine_EnemyFighting_SaysEnemyAndInBattle()
        {
            _entity.entityType = Entity.EntityType.Computer;
            TestHelpers.SetPrivateField(_entity, "_isDraggable", false);

            Assert.That(ToolkitEntityInfo.GetSideLine(_entity), Is.EqualTo("Enemy · in battle"));
        }

        [Test]
        public void GetShieldLine_NoShield_IsEmpty()
        {
            Assert.That(ToolkitEntityInfo.GetShieldLine(_entity), Is.Empty);
        }

        [Test]
        public void GetShieldLine_Shielded_ShowsItsValue()
        {
            GameObject shieldObject = new GameObject("Shield");
            _objects.Add(shieldObject);
            ResourceAttribute shield = TestHelpers.CreateResourceAttribute(shieldObject, ResourceType.Mana, AttributeType.ManaMax, 100f);
            shield.SetValue(12f);
            TestHelpers.SetPrivateField(_entity, "_shield", shield);

            Assert.That(ToolkitEntityInfo.GetShieldLine(_entity), Is.EqualTo("Shield: <b>12</b>"));
        }

        [Test]
        public void GetCombat_BeforeAnyFight_IsEmpty()
        {
            Assert.That(ToolkitEntityInfo.GetCombat(_entity).lines, Is.Empty);
        }

        [Test]
        public void GetCombat_AfterAFight_ShowsTheCombatMeter()
        {
            _entity.combatMeter.StartFight(0f);
            _entity.combatMeter.AddDamage(1f, 30f);
            _entity.combatMeter.EndFight(2f);

            ToolkitInfoSection section = ToolkitEntityInfo.GetCombat(_entity);

            Assert.That(section.lines.Count, Is.EqualTo(1), "The damage of the last fight");
            StringAssert.Contains("30 damage", section.lines[0]);
            Assert.That(section.count, Is.EqualTo(1));
        }

        StatusData CreateStatus(string title, int maxStacks)
        {
            GameplayTag tag = ScriptableObject.CreateInstance<GameplayTag>();
            tag.name = title;
            AddTagBuffFactory addTag = ScriptableObject.CreateInstance<AddTagBuffFactory>();
            addTag.data = new AddTagBuffData { tag = tag };
            BuffHandlerFactory handler = ScriptableObject.CreateInstance<BuffHandlerFactory>();
            handler.data = new BuffHandlerData
            {
                durationType = DurationType.Duration,
                duration = 4f,
                maxStacks = maxStacks,
                buffFactoryList = new List<ABuffFactory> { addTag },
                tags = new List<GameplayTag> { tag },
            };
            StatusData status = ScriptableObject.CreateInstance<StatusData>();
            status.title = title;
            status.tag = tag;
            status.buffHandlerFactory = handler;
            _objects.AddRange(new Object[] { tag, addTag, handler, status });
            return status;
        }

        BuffManager EnableBuffs()
        {
            BuffManager buffManager = _entity.GetComponent<BuffManager>();
            buffManager.isEnabled = true;
            TestHelpers.SetPrivateField(_entity, "_buffManager", buffManager);
            return buffManager;
        }

        [Test]
        public void GetStatusLine_InNoStatus_IsEmpty()
        {
            StatusData blessed = CreateStatus("Blessed", 1);

            Assert.That(ToolkitEntityInfo.GetStatusLine(_entity, new[] { blessed }), Is.Empty);
        }

        [Test]
        public void GetStatusLine_InStatuses_CitesEachAsAKeyword()
        {
            StatusData blessed = CreateStatus("Blessed", 1);
            StatusData chilled = CreateStatus("Chilled", 1);
            StatusData fortified = CreateStatus("Fortified", 1);
            _entity.AddTag(blessed.tag);
            _entity.AddTag(fortified.tag);

            string line = ToolkitEntityInfo.GetStatusLine(_entity, new[] { blessed, chilled, fortified });

            Assert.That(line, Is.EqualTo("Statuses: {status:Blessed} · {status:Fortified}"));
        }

        [Test]
        public void GetStatusLine_SeveralStacks_ShowsTheirCount()
        {
            StatusData vulnerable = CreateStatus("Vulnerable", 3);
            BuffManager buffManager = EnableBuffs();
            GameObject source = new GameObject("Hexer");
            _objects.Add(source);
            _entity.ApplyStatus(vulnerable, source);
            buffManager.ForceUpdate();
            _entity.ApplyStatus(vulnerable, source);
            buffManager.ForceUpdate();

            string line = ToolkitEntityInfo.GetStatusLine(_entity, new[] { vulnerable });

            Assert.That(line, Is.EqualTo("Statuses: {status:Vulnerable} ×2"));
        }

        [Test]
        public void GetStatusLine_OneStack_ShowsNoCount()
        {
            StatusData vulnerable = CreateStatus("Vulnerable", 3);
            BuffManager buffManager = EnableBuffs();
            GameObject source = new GameObject("Hexer");
            _objects.Add(source);
            _entity.ApplyStatus(vulnerable, source);
            buffManager.ForceUpdate();

            Assert.That(ToolkitEntityInfo.GetStatusLine(_entity, new[] { vulnerable }), Is.EqualTo("Statuses: {status:Vulnerable}"));
        }

        [Test]
        public void GetStatusLine_ThroughTheStatusText_ExplainsWhatEachStatusDoes()
        {
            StatusData blessed = CreateStatus("Blessed", 1);
            blessed.description = "+2 armor";
            blessed.isPositive = true;
            _entity.AddTag(blessed.tag);
            StatusData[] statuses = { blessed };

            string text = ToolkitStatusText.WithExplanation(ToolkitEntityInfo.GetStatusLine(_entity, statuses), statuses);

            StringAssert.Contains(StatusKeywords.FormatStatus("Blessed", statuses), text);
            StringAssert.EndsWith("Blessed</color></b>\n+2 armor", text);
        }
    }
}
