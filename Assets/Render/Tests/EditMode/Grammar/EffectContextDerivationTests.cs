using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Grammar
{
    public class EffectContextDerivationTests
    {
        [TestCase(10f, EffectMagnitude.Heavy)]
        [TestCase(100f, EffectMagnitude.Light)]
        [TestCase(0f, EffectMagnitude.Light)]
        [TestCase(float.NaN, EffectMagnitude.Light)]
        public void AdditiveMagnitudeUsesItsOwnAttributeBaseline(float baseline, EffectMagnitude expected)
        {
            FlatModifierFactory modifier = ScriptableObject.CreateInstance<FlatModifierFactory>();
            BuffHandlerFactory handler = ScriptableObject.CreateInstance<BuffHandlerFactory>();
            try
            {
                modifier.data = new FlatModifierData { type = AttributeType.Damage,
                    modifierType = AttributeModifierType.Add, value = 5f };
                handler.data = new BuffHandlerData { buffFactoryList = new List<ABuffFactory> { modifier } };
                EffectContext context = EffectContext.Default;
                context.attributeBaselines = new Dictionary<AttributeType, float>
                {
                    { AttributeType.Damage, baseline }, { AttributeType.HealthMax, 1000f }
                };
                Assert.AreEqual(expected, EffectDerivation.Magnitude(handler, context));
                Assert.AreEqual(expected, EffectDerivation.Layers(handler, true, context)[0].magnitude);
                modifier.data.modifierType = AttributeModifierType.Multiply;
                modifier.data.value = 0.2f;
                Assert.AreEqual(EffectMagnitude.Solid, EffectDerivation.Magnitude(handler, context));
            }
            finally
            {
                Object.DestroyImmediate(handler);
                Object.DestroyImmediate(modifier);
            }
        }

        [Test]
        public void RuntimeContextSnapshotsBaseValuesAndCurrentHealthMaximum()
        {
            GameObject target = new GameObject("target");
            try
            {
                AttributeManager attributes = TestHelpers.CreateAttributeManager(target, AttributeType.Damage, 12f);
                attributes.Add(AttributeType.HealthMax, new Attribute(150f));
                Entity recipient = null;
                TestHelpers.WithLoggingDisabled(() => recipient = target.AddComponent<Entity>());
                Assert.IsNull(EffectDerivation.Context(null, target).attributeBaselines);
                recipient.attributeManager = attributes;
                EffectContext context = EffectDerivation.Context(null, target);
                attributes.Get(AttributeType.Damage).BaseValue = 99f;
                Assert.AreEqual(12f, context.attributeBaselines[AttributeType.Damage]);
                Assert.AreEqual(150f, EffectDerivation.HealthReference(context));
                Assert.AreEqual(LookDerivation.DefaultHealth, EffectDerivation.HealthReference(default));
                Assert.AreEqual(EffectOrigin.Creature, context.origin);
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        // A healer's own effect is sized as its class casts it: the class's base stats, not the target's, are the
        // modifier reference and the caster base. An entity caster keeps the target's baselines
        [Test]
        public void RuntimeContext_HealerCaster_UsesItsClassBaseStats()
        {
            GameObject healer = new GameObject("healer");
            GameObject creature = new GameObject("creature");
            GameObject target = new GameObject("target");
            CharacterData data = ScriptableObject.CreateInstance<CharacterData>();
            try
            {
                data.attributes = new Dictionary<AttributeType, float> { { AttributeType.HealPower, 25f } };
                Character character = null;
                // Character.Reset() reaches for a BuffManager Init() never wired here
                TestHelpers.WithLoggingDisabled(() => character = healer.AddComponent<Character>());
                character.data = data;
                TestHelpers.WithLoggingDisabled(() => creature.AddComponent<Entity>());
                AttributeManager attributes = TestHelpers.CreateAttributeManager(target, AttributeType.HealPower, 7f);
                attributes.Add(AttributeType.HealthMax, new Attribute(150f));
                Entity recipient = null;
                TestHelpers.WithLoggingDisabled(() => recipient = target.AddComponent<Entity>());
                recipient.attributeManager = attributes;

                EffectContext cast = EffectDerivation.Context(healer, target);
                Assert.AreEqual(25f, cast.attributeBaselines[AttributeType.HealPower]);
                Assert.AreEqual(25f, cast.casterBaselines[AttributeType.HealPower]);
                Assert.AreEqual(150f, EffectDerivation.HealthReference(cast));
                Assert.AreEqual(EffectOrigin.Healer, cast.origin);

                EffectContext creatureCast = EffectDerivation.Context(creature, target);
                Assert.AreEqual(7f, creatureCast.attributeBaselines[AttributeType.HealPower]);
                Assert.IsNull(creatureCast.casterBaselines);
            }
            finally
            {
                Object.DestroyImmediate(healer);
                Object.DestroyImmediate(creature);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(data);
            }
        }
    }
}
