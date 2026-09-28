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
    }
}
