using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Zones
{
    public class BruiseZoneTests
    {
        static Entity CreateEnemy(GameObject go, float range)
        {
            AttributeManager attributes = TestHelpers.CreateAttributeManager(go, AttributeType.Range, range);
            Entity entity = null;
            TestHelpers.WithLoggingDisabled(() => entity = go.AddComponent<Entity>());
            entity.attributeManager = attributes;
            entity.entityType = Entity.EntityType.Computer;
            return entity;
        }

        [Test]
        public void EnemyRangeFollowsPositionAndAttributeAndCleansUp()
        {
            GameObject root = new GameObject("zones");
            GameObject actor = new GameObject("enemy");
            try
            {
                ZoneRegistry owner = root.AddComponent<ZoneRegistry>();
                owner.Init(new ZoneFakeUpload());
                AttributeManager attributes = TestHelpers.CreateAttributeManager(actor, AttributeType.Range, 3);
                Entity entity = null;
                TestHelpers.WithLoggingDisabled(() => entity = actor.AddComponent<Entity>());
                entity.attributeManager = attributes;
                entity.entityType = Entity.EntityType.Computer;

                BruiseZone bruise = actor.AddComponent<BruiseZone>();
                bruise.Init(entity, owner);
                owner.PublishFrame(0f);

                Assert.AreEqual(4, owner.snapshot[0].kind);
                Assert.AreEqual(3, owner.snapshot[0].radius);

                actor.transform.position = Vector3.forward;
                attributes.Get(AttributeType.Range).BaseValue = 5;
                attributes.Get(AttributeType.Range).Update();
                bruise.Refresh();
                owner.PublishFrame(0f);

                Assert.AreEqual(1, owner.count);
                Assert.AreEqual(5, owner.snapshot[0].radius);
                Assert.AreEqual(Vector3.forward, owner.snapshot[0].position);

                entity.entityType = Entity.EntityType.Player;
                bruise.Refresh();
                Assert.AreEqual(0, owner.liveCount);

                entity.entityType = Entity.EntityType.Computer;
                bruise.Refresh();
                Assert.AreEqual(1, owner.liveCount);

                bruise.enabled = false;
                TestHelpers.InvokePrivate(bruise, "OnDisable");
                Assert.AreEqual(0, owner.liveCount);
            }
            finally
            {
                Object.DestroyImmediate(actor);
                Object.DestroyImmediate(root);
            }
        }


        [Test]
        public void Init_WithZones_AddsTheBruiseThere()
        {
            GameObject root = new GameObject("zones");
            GameObject actor = new GameObject("enemy");
            ZoneRegistry owner = root.AddComponent<ZoneRegistry>();
            owner.Init(new ZoneFakeUpload());
            Entity entity = CreateEnemy(actor, 3f);
            BruiseZone bruise = actor.AddComponent<BruiseZone>();

            bruise.Init(entity, owner);
            owner.PublishFrame(0f);

            Assert.AreEqual((int)ZoneKind.Bruise, owner.snapshot[0].kind);
            Assert.AreEqual(3f, owner.snapshot[0].radius);

            Object.DestroyImmediate(actor);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void Init_WithoutZones_IgnoresTheStaticRegistry()
        {
            GameObject root = new GameObject("zones");
            GameObject actor = new GameObject("enemy");
            ZoneRegistry owner = root.AddComponent<ZoneRegistry>();
            owner.Init(new ZoneFakeUpload());
            Entity entity = CreateEnemy(actor, 3f);
            BruiseZone bruise = actor.AddComponent<BruiseZone>();

            bruise.Init(entity, (ZoneRegistry)null);
            bruise.Refresh();

            Assert.AreEqual(0, owner.liveCount);

            Object.DestroyImmediate(actor);
            Object.DestroyImmediate(root);
        }

        [TestCase(3f, true)]
        [TestCase(15.9f, true)]
        [TestCase(16f, false)]
        [TestCase(100f, false)]
        [TestCase(0f, false)]
        public void Bruises_Range_OnlyUnderTheBoardWidth(float range, bool expected)
        {
            Assert.AreEqual(expected, BruiseZone.Bruises(range));
        }

        [Test]
        public void Refresh_BoardWideRange_AddsNoBruise()
        {
            GameObject root = new GameObject("zones");
            GameObject actor = new GameObject("enemy");
            ZoneRegistry owner = root.AddComponent<ZoneRegistry>();
            owner.Init(new ZoneFakeUpload());
            Entity entity = CreateEnemy(actor, 100f);
            BruiseZone bruise = actor.AddComponent<BruiseZone>();

            bruise.Init(entity, owner);

            Assert.AreEqual(0, owner.liveCount); // the Soldier's 100 would cover every cell
            Object.DestroyImmediate(actor);
            Object.DestroyImmediate(root);
        }
    }
}
