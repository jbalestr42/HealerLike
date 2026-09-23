using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public class HLSpellGrammarTests
    {
        readonly HLSpellGrammar _grammar = new HLSpellGrammar();

        static ConsumerData Flat(float value, bool bypass = true)
        {
            return new ConsumerData
            {
                value = new FlatValue { data = new FlatValueData { value = value } },
                ignoreDamageReduction = bypass
            };
        }

        [TestCase(5, HLSign.Negative)]
        [TestCase(-5, HLSign.Positive)]
        [TestCase(0, HLSign.Zero)]
        public void ConsumerInvertsFlatValue(float value, HLSign sign)
        {
            HLVisualRecipe r = _grammar.DescribeData(Flat(value));
            Assert.AreEqual(sign, r.signature.sign);
            Assert.AreEqual(-value, r.previewAmount);
        }

        [Test]
        public void SourceReadIsNotDestinationAndPostReductionMultiplierIsPreserved()
        {
            HLGrammarContext c = new HLGrammarContext
            {
                sourceAttribute = t => t == AttributeType.HealPower ? 20 : null
            };
            HLVisualRecipe r = _grammar.Consumer(
                new ConsumerData
                {
                    value = new AttributeValue
                    {
                        data = new AttributeValueData { type = AttributeType.HealPower, multiplier = 1f }
                    },
                    ignoreConsumerPrevention = true
                },
                c,
                multiplier: -0.8f
            );
            Assert.AreEqual(16, r.previewAmount);
            Assert.AreEqual(AttributeType.HealthMax, r.signature.attribute);
            Assert.AreEqual(AttributeType.HealPower, r.readAttribute);
            Assert.IsFalse(r.ignoreReduction);
            Assert.IsTrue(r.ignorePrevention);
        }

        [Test]
        public void HealthProvenanceSurvivesEqualMagnitudeAndMaxIgnoresInverse()
        {
            HLGrammarContext c = new HLGrammarContext { sourceHealth = 50f, sourceMaxHealth = 100f };
            ConsumerData data = new ConsumerData
            {
                value = new CurrentHealthValue { data = new CurrentHealthValueData { multiplier = 0.1f } }
            };
            HLVisualRecipe a = _grammar.DescribeData(data, c);
            ((CurrentHealthValue)data.value).data.inverse = true;
            HLVisualRecipe b = _grammar.DescribeData(data, c);
            Assert.AreEqual(a.previewAmount, b.previewAmount);
            Assert.AreNotEqual(a.expression, b.expression);
            data.value = new MaxHealthValue
            {
                data = new MaxHealthValueData { multiplier = 0.1f, inverse = true }
            };
            Assert.AreEqual(-10, _grammar.DescribeData(data, c).previewAmount);
        }

        [Test]
        public void ActiveAndInstantMultiplyDifferAndFlatArmorIsHarmful()
        {
            HLVisualRecipe r = _grammar.DescribeData(
                new FlatModifierData
                {
                    type = AttributeType.Damage,
                    modifierType = AttributeModifierType.Multiply,
                    value = 0.5f
                }
            );
            Assert.AreEqual(HLSign.Negative, r.signature.sign);
            r = _grammar.DescribeData(new FlatModifierData { type = AttributeType.FlatArmor, value = 2f });
            Assert.AreEqual(HLSign.Negative, r.signature.sign);
        }

        [Test]
        public void BrokenModifiersAndUnknownDataAreInvalidWithoutConstruction()
        {
            Assert.IsFalse(_grammar.DescribeData(new SlowModifierData()).isValid);
            Assert.IsFalse(_grammar.DescribeData(new TimeModifierData()).isValid);
            Assert.IsFalse(_grammar.DescribeData(new object()).isValid);
            Assert.IsFalse(_grammar.DescribeData(Flat(float.NaN)).isValid);
        }

        [Test]
        public void HandlerPreservesDuplicateAtomsAndPeriodicLifetime()
        {
            ApplyConsumerBuffFactory f = ScriptableObject.CreateInstance<ApplyConsumerBuffFactory>();
            ConsumerFactory consumer = ScriptableObject.CreateInstance<ConsumerFactory>();
            consumer.data = Flat(5);
            f.data = new ApplyConsumerBuffData { consumerFactory = consumer };
            try
            {
                HLVisualRecipe r = _grammar.DescribeData(
                    new BuffHandlerData
                    {
                        durationType = DurationType.Infinite,
                        isPeriodic = true,
                        periodDuration = 1.5f,
                        buffFactoryList = new List<ABuffFactory> { f, f }
                    }
                );
                Assert.AreEqual(2, r.children.Count);
                Assert.AreEqual(HLDurationShape.Infinite, r.children[0].signature.duration);
                Assert.AreEqual(1.5f, r.children[0].periodSeconds);
                Assert.AreEqual(HLTempo.HandlerTick, r.children[0].signature.tempo);
            }
            finally
            {
                Object.DestroyImmediate(f);
                Object.DestroyImmediate(consumer);
            }
        }

        [Test]
        public void CharacterGroupIsNotAreaAndNamesDoNotClassify()
        {
            ConsumerFactory f = ScriptableObject.CreateInstance<ConsumerFactory>();
            f.data = Flat(10);
            try
            {
                ApplyConsumerCharacterSkillData data = new ApplyConsumerCharacterSkillData
                {
                    consumer = f,
                    isSingle = false,
                    multiplier = -1f,
                    name = "Anything"
                };
                HLVisualRecipe a = _grammar.DescribeData(data);
                data.name = "Changed";
                HLVisualRecipe b = _grammar.DescribeData(data);
                Assert.AreEqual(HLTopology.Group, a.signature.topology);
                Assert.AreEqual(HLSign.Positive, a.children[0].signature.sign);
                Assert.AreEqual(a.deliveryData, b.deliveryData);
            }
            finally
            {
                Object.DestroyImmediate(f);
            }
        }

        [Test]
        public void SnapshotPreservesOrderDuplicatesAndOmitsIdentity()
        {
            FlatModifierFactory a = ScriptableObject.CreateInstance<FlatModifierFactory>();
            FlatModifierFactory b = ScriptableObject.CreateInstance<FlatModifierFactory>();
            try
            {
                a.data = new FlatModifierData { value = 1f };
                b.data = new FlatModifierData { value = 2f };
                Assert.AreNotEqual(HLSpellGrammar.Snapshot(new[] { a, b }), HLSpellGrammar.Snapshot(new[] { b, a }));
                Assert.AreNotEqual(HLSpellGrammar.Snapshot(new[] { a, a }), HLSpellGrammar.Snapshot(new[] { a }));
                string before = HLSpellGrammar.Snapshot(a);
                a.name = "Renamed";
                a.uniqueID = "new-id";
                Assert.AreEqual(before, HLSpellGrammar.Snapshot(a));
            }
            finally
            {
                Object.DestroyImmediate(a);
                Object.DestroyImmediate(b);
            }
        }

        [Test]
        public void CharacterCostsAreSeparateSelfManaAtoms()
        {
            ConsumerFactory consumer = ScriptableObject.CreateInstance<ConsumerFactory>();
            ResourceValidatorFactory cost = ScriptableObject.CreateInstance<ResourceValidatorFactory>();
            try
            {
                consumer.data = Flat(10);
                cost.data = new ResourceValidatorData { consumer = consumer };
                ApplyConsumerCharacterSkillData data = new ApplyConsumerCharacterSkillData
                {
                    consumer = consumer,
                    multiplier = -1f,
                    validators = new List<ACharacterSkillValidatorFactory> { cost }
                };
                HLVisualRecipe recipe = _grammar.DescribeData(data);
                Assert.AreEqual(2, recipe.children.Count);
                Assert.AreEqual(AttributeType.ManaMax, recipe.children[1].signature.attribute);
                Assert.AreEqual(HLTopology.Self, recipe.children[1].signature.topology);
                Assert.AreEqual(HLSign.Negative, recipe.children[1].signature.sign);
            }
            finally
            {
                Object.DestroyImmediate(consumer);
                Object.DestroyImmediate(cost);
            }
        }

        [Test]
        public void CurveSnapshotRetainsCurveKeys()
        {
            ParticleSystem.MinMaxCurve a = new ParticleSystem.MinMaxCurve(
                1,
                new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1))
            );
            ParticleSystem.MinMaxCurve b = new ParticleSystem.MinMaxCurve(
                1,
                new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 2))
            );
            Assert.AreNotEqual(HLSpellGrammar.Snapshot(a), HLSpellGrammar.Snapshot(b));
        }

        [Test]
        public void MissingStatsRemainUnknownAndNoBypassHealIsConditional()
        {
            Assert.AreEqual(
                HLSign.Conditional,
                _grammar
                    .DescribeData(new ConsumerData { value = new AttributeValue { data = new AttributeValueData() } })
                    .signature.sign
            );
            Assert.AreEqual(HLSign.Conditional, _grammar.DescribeData(Flat(-2, false)).signature.sign);
        }
    }
}
