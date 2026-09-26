using System;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Grass
{
    public class GroundLifetimeTests
    {
        class RemovingBody : IGroundBody
        {
            public Ground ground;
            public int AppendCapsules(BodyCapsule[] into, int start)
            {
                ground.RemoveBody(this);
                return 0;
            }
        }

        class StandingBody : IGroundBody
        {
            public int AppendCapsules(BodyCapsule[] into, int start)
            {
                into[start] = new BodyCapsule
                {
                    start = Vector3.zero, end = Vector3.right, radius = 0.2f, press = 1f
                };
                return 1;
            }
        }

        [Test]
        public void Collect_BodyRetiresWhileSynchronizing_DoesNotSkipTheFollowingBody()
        {
            using (Ground ground = new Ground())
            {
                ground.AddBody(new RemovingBody { ground = ground });
                ground.AddBody(new StandingBody());
                int count = ground.Collect(new GroundStamp[4], 0, ReadOnlySpan<Zones.Zone>.Empty,
                    new BodyCapsule[4], 0f, 1f);
                Assert.AreEqual(1, count);
                Assert.AreEqual(1, ground.bodyCount);
            }
        }

        [Test]
        public void Dispose_RevokesHandlesAndPreventsFurtherPublicationWithoutDestroyingBorrowedVocabulary()
        {
            GroundVocabulary vocabulary = GroundVocabulary.CreateDefault();
            Ground ground = new Ground(vocabulary);
            try
            {
                GroundHandle handle = ground.Hold(vocabulary.boost);
                handle.Show(Vector3.zero, 1f, 1f);
                ground.Play(vocabulary.hit, Vector3.zero, 1f);
                ground.Dispose();
                ground.Dispose();
                Assert.IsTrue(vocabulary);
                Assert.IsTrue(handle.isReleased);
                handle.Show(Vector3.zero, 1f, 1f);
                Assert.IsFalse(handle.isShown);
                Assert.IsTrue(ground.Hold(vocabulary.boost).isReleased);
                ground.Play(vocabulary.hit, Vector3.zero, 1f);
                Assert.AreEqual(0, ground.oneShotCount);
                Assert.AreEqual(0, ground.heldCount);
                Assert.AreEqual(0, ground.Collect(new GroundStamp[4], 0,
                    ReadOnlySpan<Zones.Zone>.Empty, null, 0f, 1f));
            }
            finally
            {
                ground.Dispose();
                Object.DestroyImmediate(vocabulary);
            }
        }
    }
}
