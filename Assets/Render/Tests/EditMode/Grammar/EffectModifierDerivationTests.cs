using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Grammar
{
    public class EffectModifierDerivationTests
    {
        [TestCase(.05f, EffectMagnitude.Light)]
        [TestCase(.2f, EffectMagnitude.Solid)]
        [TestCase(-.5f, EffectMagnitude.Heavy)]
        [TestCase(1f, EffectMagnitude.Heavy)]
        public void MultiplierMagnitude_UsesFractionalChangeForLegacyAndLayerReadings(float value,
            EffectMagnitude expected)
        {
            FlatModifierFactory modifier = ScriptableObject.CreateInstance<FlatModifierFactory>();
            BuffHandlerFactory handler = ScriptableObject.CreateInstance<BuffHandlerFactory>();
            try
            {
                modifier.data = new FlatModifierData { type = AttributeType.Damage,
                    modifierType = AttributeModifierType.Multiply, value = value };
                handler.data = new BuffHandlerData { durationType = DurationType.Duration,
                    buffFactoryList = new List<ABuffFactory> { modifier } };
                Assert.AreEqual(expected, EffectDerivation.Magnitude(handler));
                Assert.AreEqual(expected, EffectDerivation.Layers(handler, true)[0].magnitude);
                Assert.AreEqual(value < 0 ? EffectFamily.Bane : EffectFamily.Boon,
                    EffectDerivation.Layers(handler, true)[0].family);
            }
            finally
            {
                Object.DestroyImmediate(handler);
                Object.DestroyImmediate(modifier);
            }
        }

        // Zeal's modifier: the whole value above a health threshold, read like any other modifier of its type
        [TestCase(.5f, EffectMagnitude.Heavy, EffectFamily.Boon)]
        [TestCase(.05f, EffectMagnitude.Light, EffectFamily.Boon)]
        [TestCase(-.2f, EffectMagnitude.Solid, EffectFamily.Bane)]
        public void HealthThresholdModifier_ReadsItsValueAsAModifier(float value, EffectMagnitude magnitude,
            EffectFamily family)
        {
            HealthThresholdModifierFactory modifier = ScriptableObject.CreateInstance<HealthThresholdModifierFactory>();
            BuffHandlerFactory handler = ScriptableObject.CreateInstance<BuffHandlerFactory>();
            try
            {
                modifier.data = new HealthThresholdModifierData { type = AttributeType.Damage,
                    modifierType = AttributeModifierType.Multiply, value = value, threshold = 0.7f };
                handler.data = new BuffHandlerData { durationType = DurationType.Infinite,
                    buffFactoryList = new List<ABuffFactory> { modifier } };
                Assert.IsTrue(EffectDerivation.TryModifier(modifier, out AttributeType type, out float delta));
                Assert.AreEqual(AttributeType.Damage, type);
                Assert.AreEqual(value, delta);
                Assert.AreEqual(magnitude, EffectDerivation.Magnitude(handler));
                Assert.AreEqual(family, EffectDerivation.Family(handler, true));
                Assert.AreEqual(AttributeGroup.Offence, EffectDerivation.Group(handler));
                Assert.AreEqual(EffectKind.Conditional, EffectDerivation.Kind(handler));
            }
            finally
            {
                Object.DestroyImmediate(handler);
                Object.DestroyImmediate(modifier);
            }
        }

        [TestCase(AttributeModifierType.Add, 20f, EffectMagnitude.Solid)]
        [TestCase(AttributeModifierType.Override, 100f, EffectMagnitude.Light)]
        public void OtherOperators_KeepTheirDocumentedFallback(AttributeModifierType operation, float value,
            EffectMagnitude expected)
        {
            FlatModifierFactory modifier = ScriptableObject.CreateInstance<FlatModifierFactory>();
            BuffHandlerFactory handler = ScriptableObject.CreateInstance<BuffHandlerFactory>();
            try
            {
                modifier.data = new FlatModifierData { type = AttributeType.Damage,
                    modifierType = operation, value = value };
                handler.data = new BuffHandlerData { buffFactoryList = new List<ABuffFactory> { modifier } };
                Assert.AreEqual(expected, EffectDerivation.Magnitude(handler));
                Assert.AreEqual(expected, EffectDerivation.Layers(handler, true)[0].magnitude);
            }
            finally
            {
                Object.DestroyImmediate(handler);
                Object.DestroyImmediate(modifier);
            }
        }

        // A class-sized context: the owning class's base stats, here a HealPower 25 healer with no armor stat
        static EffectContext ClassContext()
        {
            Dictionary<AttributeType, float> stats = new Dictionary<AttributeType, float>
            {
                { AttributeType.HealPower, 25f }, { AttributeType.ManaMax, 100f }
            };
            EffectContext context = EffectContext.Default;
            context.attributeBaselines = stats;
            context.casterBaselines = stats;
            return context;
        }

        static EffectMagnitude AddedMagnitude(AttributeType type, float value, EffectContext context,
            out EffectMagnitude layer)
        {
            FlatModifierFactory modifier = ScriptableObject.CreateInstance<FlatModifierFactory>();
            BuffHandlerFactory handler = ScriptableObject.CreateInstance<BuffHandlerFactory>();
            try
            {
                modifier.data = new FlatModifierData { type = type, modifierType = AttributeModifierType.Add, value = value };
                handler.data = new BuffHandlerData { durationType = DurationType.Duration,
                    buffFactoryList = new List<ABuffFactory> { modifier } };
                layer = EffectDerivation.Layers(handler, true, context)[0].magnitude;
                return EffectDerivation.Magnitude(handler, context);
            }
            finally
            {
                Object.DestroyImmediate(handler);
                Object.DestroyImmediate(modifier);
            }
        }

        // PercentArmor scales the damage taken by (1 - value), so an added fraction is its own share:
        // 0.05 is 5% less damage (Light), 0.3 is 30% (Solid), Shield's 0.5 halves it (Heavy)
        [TestCase(.05f, EffectMagnitude.Light)]
        [TestCase(.3f, EffectMagnitude.Solid)]
        [TestCase(.5f, EffectMagnitude.Heavy)]
        public void AddedDamageReduction_SizedByItsClass_IsItsOwnShare(float value, EffectMagnitude expected)
        {
            Assert.AreEqual(expected, AddedMagnitude(AttributeType.PercentArmor, value, ClassContext(),
                out EffectMagnitude layer));
            Assert.AreEqual(expected, layer);
        }

        // Vulnerability scales the damage taken by (1 + value): +0.3 is 30% more damage, a Solid share
        [Test]
        public void AddedVulnerability_SizedByItsClass_IsItsOwnShare()
        {
            Assert.AreEqual(EffectMagnitude.Solid, AddedMagnitude(AttributeType.Vulnerability, .3f, ClassContext(),
                out EffectMagnitude layer));
            Assert.AreEqual(EffectMagnitude.Solid, layer);
        }

        // Without an owning class the fraction keeps the 100-point reading, so creature effects never move:
        // 0.5 / 100 = 0.005 and Hexer's 0.3 / 100 = 0.003, both Light
        [TestCase(AttributeType.PercentArmor, .5f)]
        [TestCase(AttributeType.Vulnerability, .3f)]
        public void AddedDamageFraction_WithoutClass_KeepsTheHundredPointReading(AttributeType type, float value)
        {
            Assert.AreEqual(EffectMagnitude.Light, AddedMagnitude(type, value, EffectContext.Default,
                out EffectMagnitude layer));
            Assert.AreEqual(EffectMagnitude.Light, layer);
        }

        // A stat the class has is sized against its base value: the Sacred Tome's +10 HealPower on a HealPower
        // 25 Cleric is 10 / 25 = 0.40, exactly SolidMagnitudeMax, and the bound is inclusive so it stays Solid;
        // +10.5 is 0.42 and turns Heavy, +2 is 0.08 and stays Light
        [TestCase(10f, EffectMagnitude.Solid)]
        [TestCase(10.5f, EffectMagnitude.Heavy)]
        [TestCase(2f, EffectMagnitude.Light)]
        public void AddedClassStat_SizedAgainstTheClassBase_SolidBoundIsInclusive(float value, EffectMagnitude expected)
        {
            Assert.AreEqual(expected, AddedMagnitude(AttributeType.HealPower, value, ClassContext(),
                out EffectMagnitude layer));
            Assert.AreEqual(expected, layer);
        }

        [Test]
        public void IsDamageFraction_OnlyTheDamageTakenFractions()
        {
            Assert.IsTrue(EffectDerivation.IsDamageFraction(AttributeType.PercentArmor));
            Assert.IsTrue(EffectDerivation.IsDamageFraction(AttributeType.Vulnerability));
            Assert.IsFalse(EffectDerivation.IsDamageFraction(AttributeType.FlatArmor));
            Assert.IsFalse(EffectDerivation.IsDamageFraction(AttributeType.HealPower));
        }
    }
}
