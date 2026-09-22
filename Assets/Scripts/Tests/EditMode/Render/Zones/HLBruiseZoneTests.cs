using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Zones
{
    public class HLBruiseZoneTests
    {
        [Test] public void EnemyRangeFollowsPositionAndAttributeAndCleansUp()
        {
            var root = new GameObject("zones"); var actor = new GameObject("enemy");
            try
            {
                var owner = root.AddComponent<HLZoneRegistry>(); owner.Initialize(new HLZoneFakeUpload());
                var attributes = TestHelpers.CreateAttributeManager(actor, AttributeType.Range, 3);
                Entity entity = null; TestHelpers.WithLoggingDisabled(() => entity = actor.AddComponent<Entity>());
                entity.attributeManager = attributes; entity.entityType = Entity.EntityType.Computer;
                var bruise = actor.AddComponent<HLBruiseZone>(); bruise.Init(entity); owner.PublishFrame(0);
                Assert.AreEqual(4, owner.Snapshot[0].kind); Assert.AreEqual(3, owner.Snapshot[0].radius);
                actor.transform.position = Vector3.forward;
                attributes.Get(AttributeType.Range).BaseValue = 5; attributes.Get(AttributeType.Range).Update();
                bruise.Refresh(); owner.PublishFrame(0);
                Assert.AreEqual(1, owner.Count); Assert.AreEqual(5, owner.Snapshot[0].radius);
                Assert.AreEqual(Vector3.forward, owner.Snapshot[0].position);
                entity.entityType = Entity.EntityType.Player; bruise.Refresh(); Assert.AreEqual(0, owner.LiveCount);
                entity.entityType = Entity.EntityType.Computer; bruise.Refresh(); Assert.AreEqual(1, owner.LiveCount);
                bruise.enabled = false; TestHelpers.InvokePrivate(bruise, "OnDisable"); Assert.AreEqual(0, owner.LiveCount);
            }
            finally { Object.DestroyImmediate(actor); Object.DestroyImmediate(root); }
        }
    }
}
