using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Grammar
{
    public class PlayerClassContextTests
    {
        readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
            {
                if (created) Object.DestroyImmediate(created);
            }
            _created.Clear();
        }

        T Create<T>() where T : ScriptableObject
        {
            T instance = ScriptableObject.CreateInstance<T>();
            _created.Add(instance);
            return instance;
        }

        CharacterData Character(float healPower)
        {
            CharacterData character = Create<CharacterData>();
            character.attributes = new Dictionary<AttributeType, float>
            {
                { AttributeType.HealPower, healPower }, { AttributeType.ManaMax, 100f }
            };
            return character;
        }

        BuffHandlerFactory Handler(params ABuffFactory[] buffs)
        {
            BuffHandlerFactory handler = Create<BuffHandlerFactory>();
            handler.data = new BuffHandlerData { durationType = DurationType.Duration,
                buffFactoryList = new List<ABuffFactory>(buffs) };
            return handler;
        }

        BuffCharacterSkillFactory BuffSkill(params ABuffHandlerFactory[] handlers)
        {
            BuffCharacterSkillFactory skill = Create<BuffCharacterSkillFactory>();
            skill.data = new BuffCharacterSkillData { buffHandlerFactory = new List<ABuffHandlerFactory>(handlers) };
            return skill;
        }

        ItemFactory Item(ABuffHandlerFactory handler)
        {
            ItemFactory item = Create<ItemFactory>();
            item.data = new ItemData { buffs = new List<ABuffHandlerFactory> { handler },
                onHitEffects = new List<ABuffHandlerFactory>() };
            return item;
        }

        // A heal scaled by the caster's HealPower, as Julien's class heals are authored
        ConsumerFactory HealPowerConsumer(float multiplier)
        {
            ConsumerFactory consumer = Create<ConsumerFactory>();
            consumer.data = new ConsumerData { value = new AttributeValue { data = new AttributeValueData {
                type = AttributeType.HealPower, multiplier = multiplier } } };
            return consumer;
        }

        [Test]
        public void Owner_HandlerOfAClassBuffSkill_IsThatClass()
        {
            BuffHandlerFactory handler = Handler();
            CharacterData other = Character(15f);
            CharacterData owner = Character(25f);
            owner.skills = new List<ACharacterSkillFactory> { BuffSkill(handler) };
            Assert.AreSame(owner, PlayerClassContext.Owner(handler, new[] { other, owner }));
        }

        [Test]
        public void Owner_HandlerOfAClassItem_IsThatClass()
        {
            BuffHandlerFactory handler = Handler();
            CharacterData owner = Character(25f);
            owner.items = new List<AItemFactory> { Item(handler) };
            Assert.AreSame(owner, PlayerClassContext.Owner(handler, new[] { Character(20f), owner }));
        }

        [Test]
        public void Owner_HandlerNoClassLists_IsNone()
        {
            CharacterData character = Character(25f);
            character.skills = new List<ACharacterSkillFactory> { BuffSkill(Handler()) };
            Assert.IsNull(PlayerClassContext.Owner(Handler(), new[] { character }));
            Assert.IsNull(PlayerClassContext.Owner((ABuffHandlerFactory)null, new[] { character }));
            Assert.IsNull(PlayerClassContext.Owner(Handler(), null));
        }

        // A shared skill is sized by the first class listing it, not by the class that reads it largest
        [Test]
        public void Owner_SharedHandler_IsTheFirstClassListed()
        {
            BuffHandlerFactory handler = Handler();
            CharacterData weaker = Character(15f);
            CharacterData stronger = Character(25f);
            weaker.skills = new List<ACharacterSkillFactory> { BuffSkill(handler) };
            stronger.skills = new List<ACharacterSkillFactory> { BuffSkill(handler) };
            Assert.AreSame(weaker, PlayerClassContext.Owner(handler, new[] { weaker, stronger }));
            Assert.AreSame(stronger, PlayerClassContext.Owner(handler, new[] { stronger, weaker }));
        }

        [Test]
        public void Owner_SkillData_IsTheClassListingItsFactory()
        {
            BuffCharacterSkillFactory skill = BuffSkill();
            CharacterData owner = Character(20f);
            owner.skills = new List<ACharacterSkillFactory> { skill };
            Assert.AreSame(owner, PlayerClassContext.Owner(skill.data, new[] { Character(25f), owner }));
            Assert.IsNull(PlayerClassContext.Owner(new BuffCharacterSkillData(), new[] { owner }));
        }

        [Test]
        public void With_Class_PutsItsBaseStatsInBothBaselinesAndKeepsTheRest()
        {
            EffectContext context = EffectContext.Default;
            context.origin = EffectOrigin.Healer;
            context.targetCount = 3;
            EffectContext sized = PlayerClassContext.With(context, Character(25f));
            Assert.AreEqual(25f, sized.attributeBaselines[AttributeType.HealPower]);
            Assert.AreEqual(100f, sized.attributeBaselines[AttributeType.ManaMax]);
            Assert.AreEqual(25f, sized.casterBaselines[AttributeType.HealPower]);
            Assert.AreEqual(EffectOrigin.Healer, sized.origin);
            Assert.AreEqual(3, sized.targetCount);
            // The heal reference stays the 100-point fallback: no class carries HealthMax
            Assert.AreEqual(LookDerivation.DefaultHealth, EffectDerivation.HealthReference(sized));
        }

        [Test]
        public void With_NoClass_LeavesTheContextUnchanged()
        {
            EffectContext sized = PlayerClassContext.With(EffectContext.Default, null);
            Assert.IsNull(sized.attributeBaselines);
            Assert.IsNull(sized.casterBaselines);
        }

        // The class's stats are copied: editing the asset later does not change a context already built
        [Test]
        public void With_Class_SnapshotsItsStats()
        {
            CharacterData character = Character(25f);
            EffectContext sized = PlayerClassContext.With(EffectContext.Default, character);
            character.attributes[AttributeType.HealPower] = 99f;
            Assert.AreEqual(25f, sized.casterBaselines[AttributeType.HealPower]);
        }

        // -1.5 x HealPower read on a HealPower 20 class is a 30 heal; without a class it keeps the 1 x reading
        [Test]
        public void Harm_CasterScaledValue_ReadsTheClassBase()
        {
            ConsumerFactory consumer = HealPowerConsumer(-1.5f);
            EffectContext sized = PlayerClassContext.With(EffectContext.Default, Character(20f));
            Assert.AreEqual(-30f, EffectDerivation.Harm(consumer, sized), 1e-4f);
            Assert.AreEqual(-1.5f, EffectDerivation.Harm(consumer, EffectContext.Default), 1e-4f);
            Assert.AreEqual(-1.5f, EffectDerivation.Harm(consumer), 1e-4f);
        }

        // A class without the scaled stat keeps the plain 1 x reading rather than reading 0
        [Test]
        public void Harm_StatTheClassLacks_KeepsThePlainReading()
        {
            ConsumerFactory consumer = Create<ConsumerFactory>();
            consumer.data = new ConsumerData { value = new AttributeValue { data = new AttributeValueData {
                type = AttributeType.Damage, multiplier = 2f } } };
            EffectContext sized = PlayerClassContext.With(EffectContext.Default, Character(25f));
            Assert.AreEqual(2f, EffectDerivation.Harm(consumer, sized), 1e-4f);
        }

        // A heal of 0.2 x HealPower: 25 x 0.2 = 5, 5 / 100 = 0.05 Light; 1.5 x HealPower on 25 is 37.5, 0.375 Solid;
        // 2 x HealPower on 25 is 50, 0.5 Heavy. Unowned it reads 1 x multiplier / 100, always Light here
        [TestCase(-.2f, EffectMagnitude.Light)]
        [TestCase(-1.5f, EffectMagnitude.Solid)]
        [TestCase(-2f, EffectMagnitude.Heavy)]
        public void Magnitude_ClassHeal_SizedAtTheClassHealPower(float multiplier, EffectMagnitude expected)
        {
            ApplyConsumerBuffFactory apply = Create<ApplyConsumerBuffFactory>();
            apply.data = new ApplyConsumerBuffData { consumerFactory = HealPowerConsumer(multiplier) };
            BuffHandlerFactory handler = Handler(apply);
            CharacterData owner = Character(25f);
            owner.skills = new List<ACharacterSkillFactory> { BuffSkill(handler) };
            EffectContext sized = PlayerClassContext.For(handler, EffectContext.Default, new[] { owner });
            Assert.AreEqual(expected, EffectDerivation.Magnitude(handler, sized));
            Assert.AreEqual(expected, EffectDerivation.Layers(handler, true, sized)[0].magnitude);
            Assert.AreEqual(EffectMagnitude.Light, EffectDerivation.Magnitude(handler));
        }

        // An instant class heal read as a skill: 25 HealPower x -1 x 1.5 = 37.5, 0.375 Solid; unowned 0.015 Light
        [Test]
        public void SpellIconRead_ApplyConsumerSkill_SizedByItsOwningClass()
        {
            ApplyConsumerCharacterSkillData data = new ApplyConsumerCharacterSkillData { consumer = HealPowerConsumer(-1f),
                multiplier = 1.5f, isSingle = true, entityType = Entity.EntityType.Player };
            SpellIconDescription owned = SpellIconDerivation.Read(data, Character(25f));
            Assert.AreEqual(EffectMagnitude.Solid, owned.layers[0].magnitude);
            Assert.AreEqual(EffectOperation.Heal, owned.layers[0].operation);
            Assert.AreEqual(EffectOrigin.Healer, owned.context.origin);
            Assert.AreEqual(EffectMagnitude.Light, SpellIconDerivation.Read(data).layers[0].magnitude);
        }
    }
}
