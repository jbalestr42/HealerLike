using System.Collections.Generic;
using HealerLike.Render.Grass;
using NUnit.Framework;
using HealerLike.Render.Creatures;
using UnityEngine;

namespace HealerLike.Render.Zones
{

    public class TrampleZoneTests : TrampleZoneFixture
    {
        [Test]
        public void Refresh_ObstacleMovesOrTurnsInvalid_MovesItsOneDiscAndHidesIt()
        {
            _zone.Init(_ground);
            _zone.Refresh();
            Assert.IsTrue(Disc(out _));

            _obstacle.transform.position = Vector3.forward;
            _zone.radius = 2f;
            for (int i = 0; i < 50; i++)
            {
                _zone.Refresh();
            }

            Assert.AreEqual(1, _ground.heldCount);
            Assert.IsTrue(Disc(out GroundStamp disc));
            Assert.AreEqual(new Vector2(0f, 1f), new Vector2(disc.centreRadius.x, disc.centreRadius.y));
            Assert.AreEqual(2f, disc.centreRadius.z);

            _zone.radius = float.NaN;
            _zone.Refresh();
            Assert.IsFalse(Disc(out _));

            _zone.radius = 1f;
            _zone.Refresh();
            Assert.IsTrue(Disc(out _));

            _zone.enabled = false;
            TestHelpers.InvokePrivate(_zone, "OnDisable");
            Assert.IsFalse(Disc(out _));

            _zone.enabled = true;
            _zone.Refresh();
            Assert.IsTrue(Disc(out _));

            TestHelpers.InvokePrivate(_zone, "OnDestroy");
            Assert.AreEqual(0, _ground.heldCount);
        }

        [Test]
        public void Refresh_Steady_AllocatesNothing()
        {
            _zone.Init(_ground);
            for (int i = 0; i < 100; i++)
            {
                _zone.Refresh();
            }

            long before = System.GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++)
            {
                _zone.Refresh();
            }
            long allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.AreEqual(0, allocated);
        }

        [Test]
        public void Init_AnotherGround_MovesTheObstacleThere()
        {
            _zone.Init(_ground);
            _zone.Refresh();
            using Ground replacement = new Ground();

            _zone.Init(replacement);
            _zone.Refresh();

            Assert.AreEqual(0, _ground.heldCount);
            Assert.AreEqual(1, replacement.heldCount);
        }

    }
}
