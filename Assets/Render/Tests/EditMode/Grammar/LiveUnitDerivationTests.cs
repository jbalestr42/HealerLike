using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Grammar
{
    public class LiveUnitDerivationTests
    {
        [TestCase(AttributeType.AttackRate, .5f, 1f, AccessoryKind.SmallTorus)]
        [TestCase(AttributeType.AttackRate, 2f, 1f, AccessoryKind.ConeCrown)]
        [TestCase(AttributeType.Vulnerability, .1f, .2f, AccessoryKind.SmallTorus)]
        [TestCase(AttributeType.Damage, 20f, 10f, AccessoryKind.SmallTorus)]
        public void UpgradeAccessory_UsesTheSharedAttributePolarity(AttributeType type, float current,
            float baseline, AccessoryKind expected)
        {
            EntityData data = ScriptableObject.CreateInstance<EntityData>();
            try
            {
                var attribute = new Attribute(current) { BaseValue = baseline };
                var attributes = new Dictionary<AttributeType, Attribute> { { type, attribute } };
                UnitChannels channels = LiveUnitDerivation.Read(data, Entity.EntityType.Player, attributes);
                Assert.AreEqual(expected, channels.accessory);
                Assert.AreEqual(current, attribute.Value);
                Assert.AreEqual(baseline, attribute.BaseValue);
                Assert.AreEqual(0, data.attributes.Count);
            }
            finally { Object.DestroyImmediate(data); }
        }

        [Test]
        public void LiveBands_UseEffectiveValuesAndLeaveTheAuthoredAssetUntouched()
        {
            EntityData data = ScriptableObject.CreateInstance<EntityData>();
            try
            {
                data.attributes[AttributeType.HealthMax] = 100f;
                data.attributes[AttributeType.Range] = 100f;
                var attributes = new Dictionary<AttributeType, Attribute>
                {
                    { AttributeType.HealthMax, new Attribute(300f) },
                    { AttributeType.Range, new Attribute(900f) }
                };
                UnitChannels channels = LiveUnitDerivation.Read(data, Entity.EntityType.Computer, attributes);
                Assert.AreEqual(MassBand.Heavy, channels.mass);
                Assert.AreEqual(ReachBand.Long, channels.reach);
                Assert.AreEqual(LookSide.Stone, channels.side);
                Assert.AreEqual(100f, data.attributes[AttributeType.HealthMax]);
                Assert.AreEqual(100f, data.attributes[AttributeType.Range]);
            }
            finally { Object.DestroyImmediate(data); }
        }
    }
}
