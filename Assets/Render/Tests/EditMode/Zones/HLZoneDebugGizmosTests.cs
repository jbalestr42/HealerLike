using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Zones
{
    public class HLZoneDebugGizmosTests
    {
        [Test]
        public void HealAndHostileHaveDistinctColoursAndClampedStrengthAlpha()
        {
            HLZone zone = new HLZone { kind = (int)HLZoneKind.Heal, strength = 0.35f };

            Color heal = HLZoneDebugGizmos.ColorFor(zone);
            Assert.AreEqual(1, heal.g);
            Assert.AreEqual(0.35f, heal.a);

            zone.kind = (int)HLZoneKind.Hostile;
            zone.strength = 2f;
            Color hostile = HLZoneDebugGizmos.ColorFor(zone);
            Assert.AreEqual(1, hostile.r);
            Assert.AreEqual(1, hostile.a);
            Assert.AreNotEqual(heal, hostile);
        }
    }
}
