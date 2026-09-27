using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Grammar
{
    public class EffectLayerDerivationTests
    {
        readonly List<Object> _objects = new List<Object>();
        T Make<T>() where T : ScriptableObject
        {
            T item = ScriptableObject.CreateInstance<T>();
            _objects.Add(item);
            return item;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object item in _objects) Object.DestroyImmediate(item);
            _objects.Clear();
        }

        BuffHandlerFactory Handler(params ABuffFactory[] buffs)
        {
            BuffHandlerFactory handler = Make<BuffHandlerFactory>();
            handler.data = new BuffHandlerData
            {
                buffFactoryList = new List<ABuffFactory>(buffs),
                durationType = DurationType.Duration,
                duration = 5f
            };
            return handler;
        }

        FlatModifierFactory Modifier(AttributeType type, float value)
        {
            FlatModifierFactory buff = Make<FlatModifierFactory>();
            buff.data = new FlatModifierData { type = type, value = value, modifierType = AttributeModifierType.Add };
            return buff;
        }

        ConsumerFactory Consumer(float value)
        {
            ConsumerFactory consumer = Make<ConsumerFactory>();
            consumer.data = new ConsumerData { value = new FlatValue { data = new FlatValueData { value = value } } };
            return consumer;
        }

        ApplyConsumerBuffFactory Apply(float value)
        {
            ApplyConsumerBuffFactory buff = Make<ApplyConsumerBuffFactory>();
            buff.data = new ApplyConsumerBuffData { consumerFactory = Consumer(value) };
            return buff;
        }

        [TestCase(true)]
        [TestCase(false)]
        public void OpposedModifiers_KeepIndependentMeaningOnEitherSide(bool sameSide)
        {
            BuffHandlerFactory handler = Handler(Modifier(AttributeType.Damage, 20f),
                Modifier(AttributeType.AttackRate, 1f), Modifier(AttributeType.HitArmor, 2f));
            IReadOnlyList<EffectChannels> layers = EffectDerivation.Layers(handler, sameSide);
            Assert.AreEqual(3, layers.Count);
            Assert.AreEqual(EffectOperation.Boon, layers[0].operation);
            Assert.AreEqual(EffectOperation.Bane, layers[1].operation);
            Assert.AreEqual(EffectOperation.Boon, layers[2].operation);
            Assert.AreEqual(EffectAspect.Offence, layers[0].aspect);
            Assert.AreEqual(EffectAspect.Defence, layers[2].aspect);
            Assert.AreEqual(sameSide ? EffectSide.Ally : EffectSide.Opposing, layers[1].side);
            Assert.AreEqual(3, handler.data.buffFactoryList.Count);
        }

        [TestCase(false, EffectFamily.Damage, EffectFamily.Heal, EffectTempo.ForDuration)]
        [TestCase(true, EffectFamily.Rot, EffectFamily.Renew, EffectTempo.PerPeriod)]
        public void Consumers_KeepTheirOwnSignMagnitudeAndPeriodicFamily(bool periodic, EffectFamily harm,
            EffectFamily heal, EffectTempo tempo)
        {
            BuffHandlerFactory handler = Handler(Apply(80f), Apply(-5f));
            handler.data.isPeriodic = periodic;
            handler.data.periodDuration = 1.5f;
            IReadOnlyList<EffectChannels> layers = EffectDerivation.Layers(handler, true);
            Assert.AreEqual(harm, layers[0].family);
            Assert.AreEqual(heal, layers[1].family);
            Assert.AreEqual(EffectOperation.Damage, layers[0].operation);
            Assert.AreEqual(EffectOperation.Heal, layers[1].operation);
            Assert.AreEqual(EffectMagnitude.Heavy, layers[0].magnitude);
            Assert.AreEqual(EffectMagnitude.Light, layers[1].magnitude);
            Assert.AreEqual(tempo, layers[0].tempo);
            Assert.AreEqual(periodic ? 1.5f : 0f, layers[1].periodSeconds);
        }

        [Test]
        public void ManaAndWard_DoNotOverrideNeighbouringLayers()
        {
            ManaOnRoundEndBuffFactory mana = Make<ManaOnRoundEndBuffFactory>();
            mana.data = new ManaOnRoundEndBuffData { consumerFactory = Consumer(-10f) };
            BuffHandlerFactory handler = Handler(mana, Make<InvincibilityBuffFactory>(), Apply(10f));
            IReadOnlyList<EffectChannels> layers = EffectDerivation.Layers(handler, true);
            Assert.AreEqual(EffectOperation.Mana, layers[0].operation);
            Assert.AreEqual(EffectTrigger.RoundEnd, layers[0].trigger);
            Assert.AreEqual(EffectOperation.Ward, layers[1].operation);
            Assert.AreEqual(EffectAspect.Prevention, layers[1].aspect);
            Assert.AreEqual(EffectOperation.Damage, layers[2].operation);
            Assert.AreEqual(EffectTrigger.Cast, layers[2].trigger);
        }

        [Test]
        public void ContextAndInstantTempo_ArePreservedForEveryLayer()
        {
            BuffHandlerFactory handler = Handler(Apply(10f), Modifier(AttributeType.HealthMax, 30f));
            handler.data.durationType = DurationType.Instant;
            handler.data.isPeriodic = true;
            var context = new EffectContext
            {
                origin = EffectOrigin.Item, targetCount = int.MaxValue,
                triggers = new[] { EffectTrigger.OnHit }
            };
            foreach (EffectChannels layer in EffectDerivation.Layers(handler, false, context))
            {
                Assert.AreEqual(EffectTempo.Once, layer.tempo);
                Assert.AreEqual(0f, layer.periodSeconds);
                Assert.AreEqual(EffectOrigin.Item, layer.origin);
                Assert.AreEqual(EffectReach.All, layer.reach);
                Assert.AreEqual(EffectTrigger.OnHit, layer.trigger);
            }
        }

        [Test]
        public void EmptyAndPartiallyAuthoredHandlers_ProduceNoFabricatedLayers()
        {
            Assert.IsEmpty(EffectDerivation.Layers(null, true));
            BuffHandlerFactory handler = Make<BuffHandlerFactory>();
            handler.data = null;
            Assert.IsEmpty(EffectDerivation.Layers(handler, true));
            handler = Handler(null, Modifier(AttributeType.Damage, 10f), null);
            Assert.AreEqual(1, EffectDerivation.Layers(handler, true).Count);
        }
    }
}
