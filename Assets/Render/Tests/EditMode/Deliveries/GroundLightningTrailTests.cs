using HealerLike.Render.Grass;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Deliveries
{
    public class GroundLightningTrailTests
    {
        [Test]
        public void Contact_ContinuesFromPreviousTargetUntilTheProjectileIsReused()
        {
            using (Ground ground = new Ground())
            {
                GameObject target = new GameObject("Chain target");
                try
                {
                    GroundLightningTrail trail = new GroundLightningTrail();
                    target.transform.position = Vector3.right;
                    trail.Contact(ground, Vector3.zero, target);
                    Assert.IsTrue(ground.Find(ground.vocabulary.scorch, out Vector2 from, out Vector2 to,
                        out _, out _));
                    Assert.AreEqual(Vector2.zero, from);
                    Assert.AreEqual(Vector2.right, to);
                    ground.Clear();
                    target.transform.position = Vector3.forward;
                    trail.Contact(ground, Vector3.one * 20f, target);
                    Assert.IsTrue(ground.Find(ground.vocabulary.scorch, out from, out to, out _, out _));
                    Assert.AreEqual(Vector2.right, from);
                    Assert.AreEqual(Vector2.up, to);
                    ground.Clear();
                    trail.Clear();
                    trail.Contact(ground, Vector3.zero, target);
                    Assert.IsTrue(ground.Find(ground.vocabulary.scorch, out from, out _, out _, out _));
                    Assert.AreEqual(Vector2.zero, from);
                }
                finally
                {
                    Object.DestroyImmediate(target);
                }
            }
        }
    }
}
