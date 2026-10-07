using HealerLike.Render.Grammar;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Deliveries
{
    public class DeliveryPathViewTests
    {
        [Test]
        public void ContactsTrackTargetsAndReleaseWithoutOwningTheirMaterial()
        {
            GameObject source = new GameObject("source");
            GameObject target = new GameObject("target");
            DeliveryVocabulary vocabulary = RenderTestAssets.LoadDeliveryVocabulary();
            DeliveryPathView path = DeliveryPathView.Create(source,
                new DeliveryChannels { path = DeliveryPathKind.Chain }, vocabulary, 0);
            try
            {
                target.transform.position = Vector3.right * 4;
                path.Contact(target, target.transform.position);
                LineRenderer line = path.GetComponentInChildren<LineRenderer>();
                Assert.AreEqual(target.transform.position, line.GetPosition(line.positionCount - 1));
                target.transform.position += Vector3.forward;
                path.Advance(.1f);
                Assert.AreEqual(target.transform.position, line.GetPosition(line.positionCount - 1));
                float width = line.widthMultiplier;
                path.Release();
                path.Advance(vocabulary.GetPath(DeliveryPathKind.Chain).releaseSeconds * .5f);
                Assert.Less(line.widthMultiplier, width);
                path.Advance(3f);
                Assert.IsTrue(path == null);
                Assert.IsTrue(vocabulary.material);
            }
            finally
            {
                if (path)
                {
                    path.Dispose();
                }

                Object.DestroyImmediate(source);
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void HeldModeComesFromProjectileBehaviourAndHasAnIndependentAuthoredPath()
        {
            GameObject go = new GameObject("spell");
            try
            {
                ChainLightningProjectile projectile = go.AddComponent<ChainLightningProjectile>();
                Assert.AreEqual(DeliveryPathKind.Chain, DeliveryDerivation.Read(projectile, DeliveryStyle.Direct).path);
                TestHelpers.SetPrivateField(projectile, "_effectMode", ChainLightningProjectile.EffectMode.AttackRateDuration);
                Assert.AreEqual(DeliveryPathKind.Beam, DeliveryDerivation.Read(projectile, DeliveryStyle.Direct).path);
                DeliveryVocabulary vocabulary = RenderTestAssets.LoadDeliveryVocabulary();
                Assert.Less(vocabulary.GetPath(DeliveryPathKind.Beam).deviation,
                    vocabulary.GetPath(DeliveryPathKind.Chain).deviation);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
