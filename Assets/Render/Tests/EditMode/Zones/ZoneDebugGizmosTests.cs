using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Zones
{
    public class ZoneDebugGizmosTests
    {
        [Test]
        public void HealAndHostileHaveDistinctColoursAndClampedStrengthAlpha()
        {
            Zone zone = new Zone { kind = (int)ZoneKind.Heal, strength = 0.35f };

            Color heal = ZoneDebugGizmos.ColorFor(zone);
            Assert.AreEqual(1, heal.g);
            Assert.AreEqual(0.35f, heal.a);

            zone.kind = (int)ZoneKind.Hostile;
            zone.strength = 2f;
            Color hostile = ZoneDebugGizmos.ColorFor(zone);
            Assert.AreEqual(1, hostile.r);
            Assert.AreEqual(1, hostile.a);
            Assert.AreNotEqual(heal, hostile);
        }
    }
}
