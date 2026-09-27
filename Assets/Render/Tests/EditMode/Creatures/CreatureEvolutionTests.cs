using System.Collections.Generic;
using HealerLike.Render.Grammar;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public class CreatureEvolutionTests
    {
        EntityData _data;
        GameObject _owner;
        AttributeManager _manager;
        CreatureEvolution _evolution;
        readonly List<Object> _objects = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            _data = ScriptableObject.CreateInstance<EntityData>();
            _owner = new GameObject("Evolution observer");
            _manager = TestHelpers.CreateAttributeManager(_owner, AttributeType.HealthMax, 100f);
            _evolution = new CreatureEvolution();
            _evolution.Init(_data, Entity.EntityType.Player, _manager);
        }

        [TearDown]
        public void TearDown()
        {
            _evolution.Dispose();
            Object.DestroyImmediate(_owner);
            Object.DestroyImmediate(_data);
            foreach (Object obj in _objects) Object.DestroyImmediate(obj);
            _objects.Clear();
        }

        void Accept()
        {
            Assert.IsTrue(_evolution.TryRead(out UnitChannels channels));
            _evolution.Accept(channels);
        }

        [Test]
        public void AttributeChanges_CoalesceAndOnlyCrossingBandsRecomposes()
        {
            Accept();
            Attribute health = _manager.Get(AttributeType.HealthMax);
            health.BaseValue = 105f;
            health.Update();
            Assert.IsFalse(_evolution.TryRead(out _));
            health.BaseValue = 160f;
            health.Update();
            health.BaseValue = 240f;
            health.Update();
            Assert.IsTrue(_evolution.TryRead(out UnitChannels channels));
            Assert.AreEqual(MassBand.Heavy, channels.mass);
            _evolution.Accept(channels);
            Assert.IsFalse(_evolution.TryRead(out _));
            health.BaseValue = 100f;
            health.Update();
            Assert.IsTrue(_evolution.TryRead(out channels));
            Assert.AreEqual(MassBand.Light, channels.mass);
        }

        [Test]
        public void NewRangeAttribute_IsObservedAfterSpawn()
        {
            Accept();
            Attribute range = _manager.Add(AttributeType.Range, new Attribute(40f));
            Assert.IsTrue(_evolution.TryRead(out UnitChannels channels));
            Assert.AreEqual(ReachBand.Short, channels.reach);
            _evolution.Accept(channels);
            range.BaseValue = 900f;
            range.Update();
            Assert.IsTrue(_evolution.TryRead(out channels));
            Assert.AreEqual(ReachBand.Long, channels.reach);
        }

        [Test]
        public void Dispose_DetachesListenersAndClearsPendingChanges()
        {
            Accept();
            _evolution.Dispose();
            Attribute health = _manager.Get(AttributeType.HealthMax);
            health.BaseValue = 900f;
            health.Update();
            Assert.IsFalse(_evolution.TryRead(out _));
            _evolution.Init(_data, Entity.EntityType.Computer, _manager);
            Assert.IsTrue(_evolution.TryRead(out UnitChannels channels));
            Assert.AreEqual(MassBand.Heavy, channels.mass);
            Assert.AreEqual(LookSide.Stone, channels.side);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Cadence_UsesActualRuntimeSecondsForShootersAndAreaSkills(bool shooter)
        {
            ASkillFactory skill = shooter ? (ASkillFactory)ScriptableObject.CreateInstance<ShootProjectileSkillFactory>()
                : ScriptableObject.CreateInstance<AreaOfEffectSkillFactory>();
            _objects.Add(skill);
            _data.skillFactories = new List<ASkillFactory> { skill };
            _manager.Add(AttributeType.AttackRate, new Attribute(0.3f));
            Assert.IsTrue(_evolution.TryRead(out UnitChannels channels));
            Assert.AreEqual(StemBand.Quick, channels.stem);
            _evolution.Accept(channels);
            Attribute cadence = _manager.Get(AttributeType.AttackRate);
            cadence.BaseValue = 2f;
            cadence.Update();
            Assert.IsTrue(_evolution.TryRead(out channels));
            Assert.AreEqual(StemBand.Slow, channels.stem);
        }

        [Test]
        public void BuffRemoval_RestoresAccessoryWithoutMutatingData()
        {
            Attribute damage = _manager.Add(AttributeType.Damage, new Attribute(10f));
            Accept();
            FlatModifier buff = new FlatModifier
            {
                data = new FlatModifierData { value = 10f, modifierType = AttributeModifierType.Add }
            };
            buff.Init(_owner, _owner);
            damage.AddModifier(AttributeModifierType.Add, _owner, buff);
            damage.Update();
            Assert.IsTrue(_evolution.TryRead(out UnitChannels channels));
            Assert.AreEqual(AccessoryKind.SmallTorus, channels.accessory);
            _evolution.Accept(channels);
            damage.RemoveModifier(buff);
            damage.Update();
            Assert.IsTrue(_evolution.TryRead(out channels));
            Assert.AreEqual(AccessoryKind.None, channels.accessory);
            Assert.AreEqual(10f, damage.BaseValue);
            Assert.IsTrue(_data.attributes == null || !_data.attributes.ContainsKey(AttributeType.Damage));
        }

        [Test]
        public void DetrimentalBuff_WinsOverPositiveUpgrade()
        {
            var attributes = new Dictionary<AttributeType, Attribute>();
            Attribute damage = new Attribute(20f) { BaseValue = 10f };
            Attribute speed = new Attribute(1f) { BaseValue = 2f };
            attributes.Add(AttributeType.Damage, damage);
            attributes.Add(AttributeType.Speed, speed);
            UnitChannels channels = CreatureEvolution.Read(_data, Entity.EntityType.Player, attributes);
            Assert.AreEqual(AccessoryKind.ConeCrown, channels.accessory);
        }
    }
}
