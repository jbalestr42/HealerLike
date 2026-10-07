using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Grass
{
    public class GroundAnnulusTests
    {
        [Test]
        public void Annulus_ColoursOnlyTheTravellingFrontAndAppliesNoForce()
        {
            GroundStamp state = GroundStamp.Annulus(Vector2.zero, 2f, 0.3f, 0.5f, -0.2f, -1f, 0.1f);
            Assert.AreEqual(new Vector4(0.5f, -0.2f, -1f, 0.1f), state.State(Vector2.right * 2f));
            Assert.AreEqual(Vector4.zero, state.State(Vector2.zero));
            Assert.AreEqual(Vector4.zero, state.State(Vector2.right * 3f));
            Assert.AreEqual(Vector3.zero, state.Target(Vector2.right * 2f));
            Assert.AreEqual(Vector2.zero, state.Force(Vector2.right * 2f));
            state.Bounds(out Vector2 centre, out float reach);
            Assert.AreEqual(Vector2.zero, centre);
            Assert.AreEqual(2.3f, reach, 0.0001f);
        }

        [Test]
        public void Ring_StateAndMotionShareFrontAndReleaseTogether()
        {
            GroundEffect effect = new GroundEffect { shape = GroundShape.Ring, grow = 0.4f, release = 0.2f,
                lifetime = 1f, minBand = 0.1f, band = 0.1f, kick = 20f, light = 1f };
            GroundStamp[] stamps = new GroundStamp[3];
            Assert.AreEqual(2, effect.Write(stamps, 0, Vector2.zero, Vector2.zero, 4f, 1f, 0.2f, false));
            Assert.AreEqual(GroundStampKind.Front, stamps[0].kind);
            Assert.AreEqual(GroundStampKind.Annulus, stamps[1].kind);
            Assert.AreEqual(stamps[0].centreRadius, stamps[1].centreRadius);
            Assert.AreEqual(1f, stamps[1].State(Vector2.right * 2f).z);
            Assert.AreEqual(0f, stamps[1].State(Vector2.zero).z);
            Assert.AreEqual(2, effect.Write(stamps, 0, Vector2.zero, Vector2.zero, 4f, 1f, 0.5f, false));
            Assert.AreEqual(0.5f, stamps[1].State(Vector2.right * 4f).z, 0.0001f);
            Assert.AreEqual(0, effect.Write(stamps, 0, Vector2.zero, Vector2.zero, 4f, 1f, 0.7f, false));
        }

        [Test]
        public void StateOnlyRing_WritesWithinAvailableCapacity()
        {
            GroundEffect effect = new GroundEffect { shape = GroundShape.Ring, grow = 0.4f, release = 0.2f, ash = 1f };
            GroundStamp[] stamps = new GroundStamp[1];
            Assert.AreEqual(1, effect.Write(stamps, 0, Vector2.zero, Vector2.zero, 2f, 1f, 0.2f, false));
            Assert.AreEqual(GroundStampKind.Annulus, stamps[0].kind);
            Assert.AreEqual(0, effect.Write(stamps, 1, Vector2.zero, Vector2.zero, 2f, 1f, 0.2f, false));
        }
    }
}
