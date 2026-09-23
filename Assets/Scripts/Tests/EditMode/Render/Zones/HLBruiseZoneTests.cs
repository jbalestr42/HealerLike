using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Zones
{
    public class HLBruiseZoneTests
    {
        [Test]
        public void EnemyRangeFollowsPositionAndAttributeAndCleansUp()
        {
            GameObject root = new GameObject("zones");
            GameObject actor = new GameObject("enemy");
            try
            {
                HLZoneRegistry owner = root.AddComponent<HLZoneRegistry>();
                owner.Initialize(new HLZoneFakeUpload());
                AttributeManager attributes = TestHelpers.CreateAttributeManager(actor, AttributeType.Range, 3);
                Entity entity = null;
                TestHelpers.WithLoggingDisabled(() => entity = actor.AddComponent<Entity>());
                entity.attributeManager = attributes;
                entity.entityType = Entity.EntityType.Computer;

                HLBruiseZone bruise = actor.AddComponent<HLBruiseZone>();
                bruise.Init(entity);
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
    }
}
