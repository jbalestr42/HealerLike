using System.Collections.Generic;
using HealerLike.Render.Grass;
using NUnit.Framework;
using HealerLike.Render.Creatures;
using UnityEngine;

namespace HealerLike.Render.Zones
{

    public class TrampleLandingTests : TrampleZoneFixture
    {
        [Test]
        public void Refresh_FirstStand_ThrowsARingOutOfEveryFootAndOneRoundTheBody()
        {
            BuildRootedCreature();

            _zone.Refresh();

            Assert.AreEqual(1, _zone.landings);
            Assert.AreEqual(6, _ground.Playing(_ground.vocabulary.footRing));
            Assert.AreEqual(1, _ground.Playing(_ground.vocabulary.bodyRing));
            _zone.Refresh();
            Assert.AreEqual(1, _zone.landings, "Standing still lands once.");
        }

        [Test]
        public void Refresh_Held_BrushesTheGrassAndLandsWhenLetGo()
        {
            BuildRootedCreature();
            GameObject grip = new GameObject("collider");
            grip.transform.SetParent(_obstacle.transform, false);
            Collider collider = grip.AddComponent<BoxCollider>();
            TestHelpers.SetPrivateField(_zone, "_hold", collider);
            _zone.Refresh();

            grip.layer = Layers.IgnoreRaycast;
            _obstacle.transform.position = new Vector3(3f, 0f, 0f);
            _zone.Refresh();

            Assert.Greater(_zone.AppendCapsules(new BodyCapsule[TrampleZone.MaxCapsules], 0), 0,
                "Dragged over the grass, it leaves a trail.");
            Assert.AreEqual(1, _zone.landings, "Hopping from cell to cell while held lands nowhere.");

            grip.layer = 0;
            _zone.Refresh();

            Assert.AreEqual(2, _zone.landings);
            Assert.Greater(_zone.AppendCapsules(new BodyCapsule[TrampleZone.MaxCapsules], 0), 0);
        }

        [Test]
        public void Refresh_JumpIntoPlace_LandsButAStepDoesNot()
        {
            BuildRootedCreature();
            _zone.Refresh();

            _obstacle.transform.position += new Vector3(0.1f, 0f, 0f);
            _zone.Refresh();
            Assert.AreEqual(1, _zone.landings, "A walking step is no landing.");

            _obstacle.transform.position += new Vector3(1f, 0f, 0f);
            _zone.Refresh();
            Assert.AreEqual(2, _zone.landings, "A swap moves it a whole cell at once.");
        }

        [Test]
        public void Feet_Roots_RestAtTheFootRadiusAroundTheRoot()
        {
            GameObject root = new GameObject("root");
            try
            {
                root.transform.position = new Vector3(1f, 0f, 2f);
                RootDefinition roots = new RootDefinition { count = 4, footRadius = 0.5f };
                List<Vector3> feet = new List<Vector3>();

                TrampleZone.Feet(root.transform, roots, 2f, feet);

                Assert.AreEqual(4, feet.Count);
                Assert.That(Vector3.Distance(new Vector3(2f, 0f, 2f), feet[0]), Is.LessThan(1e-5f));
                Assert.That(Vector3.Distance(new Vector3(1f, 0f, 3f), feet[1]), Is.LessThan(1e-5f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Refresh_InitWithGround_ShowsOneDiscAndLandsOnce()
        {
            _zone.Init(_ground);
            _zone.Refresh();
            _zone.Refresh();

            Assert.AreEqual(1, CountRings(), "An obstacle lands once with a ring round itself.");
            Assert.IsTrue(Disc(out _));
        }

        [Test]
        public void Refresh_InitWithoutGround_AddsNothing()
        {
            _zone.Init(null);
            _zone.Refresh();

            Assert.AreEqual(0, _ground.heldCount);
            Assert.AreEqual(0, _zone.landings);
        }
    }
}
