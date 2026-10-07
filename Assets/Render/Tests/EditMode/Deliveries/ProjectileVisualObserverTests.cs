using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Deliveries
{
    public class ProjectileVisualObserverTests
    {
        readonly ProjectileScene _scene = new ProjectileScene();
        [SetUp] public void SetUp() => _scene.Create();
        [TearDown] public void TearDown() => _scene.Destroy();

        [TestCase(DeliveryStyle.Direct)]
        [TestCase(DeliveryStyle.Arc)]
        [TestCase(DeliveryStyle.Rigid)]
        [TestCase(DeliveryStyle.Swarm)]
        [TestCase(DeliveryStyle.Thrown)]
        public void EveryProjectileOwnsItsVisualEvenWhenACreatureWouldClaimIt(DeliveryStyle style)
        {
            _scene.observer.Init(_scene.manager, style);
            Vector3 position = _scene.projectile.transform.position;
            _scene.observer.Init(_scene.source);
            Assert.IsTrue(_scene.IsFree());
            Assert.AreEqual(0, _scene.probe.token, "Creature geometry must never claim a spell.");
            Assert.AreEqual(style, _scene.projectileObject.GetComponent<FreeShot>().tip.style);
            Assert.IsFalse(_scene.projectileObject.GetComponent<LineRenderer>().enabled);
            Assert.AreEqual(position, _scene.projectile.transform.position);
        }

        [TestCase(10f)]
        [TestCase(-5f)]
        public void PayloadColoursTheDetachedSpellWithoutTintingCreatureArms(float amount)
        {
            _scene.Shoot(_scene.CreateConsumer(amount));
            FreeShot shot = _scene.projectileObject.GetComponent<FreeShot>();
            TestHelpers.InvokePrivate(shot, "LateUpdate");
            Renderer renderer = GameObject.Find("FreeShot").GetComponentInChildren<Renderer>();
            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            Color expected = amount > 0 ? RenderTestAssets.LoadPalette().damage : RenderTestAssets.LoadPalette().heal;
            Color actual = properties.GetColor(RenderObjects.BaseColorId);
            Assert.That(Vector4.Distance(expected, actual), Is.LessThan(.00001f));
            Assert.AreEqual(0, _scene.probe.accents);
        }

        [Test]
        public void SourceViewAcceptanceAndRemovalDoNotChangeSpellPresentation()
        {
            _scene.probe.accepts = false;
            _scene.observer.Init(_scene.source);
            FreeShot shot = _scene.projectileObject.GetComponent<FreeShot>();
            _scene.probe.accepts = true;
            _scene.probe.enabled = false;
            TestHelpers.InvokePrivate(_scene.observer, "LateUpdate");
            Assert.IsTrue(shot.enabled);
            Assert.AreEqual(0, _scene.probe.ends);
            _scene.observer.enabled = false;
            TestHelpers.InvokePrivate(_scene.observer, "OnDisable");
            Assert.IsTrue(_scene.projectileObject.GetComponent<LineRenderer>().enabled);
            Assert.IsFalse(shot.enabled);
        }

        [Test]
        public void ItemBounceSelectsSpellStyleAndKeepsItsShotAfterContact()
        {
            _scene.projectileObject.AddComponent<BounceProjectileBehaviour>();
            _scene.observer.Init(_scene.source);
            _scene.projectile.OnHit.Invoke(new OnHitData { target = _scene.first });
            Assert.AreEqual(DeliveryStyle.Bounce, _scene.observer.deliveryStyle);
            Assert.IsTrue(_scene.IsFree());
            Assert.AreEqual(0, _scene.probe.contacts);
        }

        [Test]
        public void SynchronousChainContactsBelongToSpellPathInOriginalOrder()
        {
            _scene.observer.Init(_scene.manager, DeliveryStyle.ChainSync);
            _scene.observer.Init(_scene.source);
            _scene.projectile.OnHit.Invoke(new OnHitData { target = _scene.first });
            _scene.projectile.OnHit.Invoke(new OnHitData { target = _scene.second });
            _scene.projectile.SetTarget(null);
            Assert.AreEqual(2, _scene.observer.contacts.Count);
            Assert.AreSame(_scene.first, _scene.observer.contacts[0].target);
            Assert.AreSame(_scene.second, _scene.observer.contacts[1].target);
            Assert.AreEqual(2, _scene.observer.path.contactCount);
            Assert.AreEqual(0, _scene.probe.contacts);
            Assert.IsFalse(_scene.IsFree());
        }

        [Test]
        public void RetargetingAndReinitializationDoNotDuplicateContactsOrMoveGameplay()
        {
            Vector3 position = _scene.projectile.transform.position;
            _scene.observer.Init(_scene.source);
            _scene.observer.Init(_scene.source);
            _scene.projectile.OnHit.AddListener(_ => _scene.projectile.SetTarget(_scene.second));
            _scene.projectile.OnHit.Invoke(new OnHitData { target = _scene.first });
            Assert.AreEqual(1, _scene.observer.contacts.Count);
            Assert.AreSame(_scene.first, _scene.observer.contacts[0].target);
            Assert.AreSame(_scene.second, _scene.projectile.target);
            Assert.AreEqual(position, _scene.projectile.transform.position);
            TestHelpers.InvokePrivate(_scene.observer, "OnDisable");
            _scene.projectile.OnHit.Invoke(new OnHitData { target = _scene.second });
            Assert.AreEqual(1, _scene.observer.contacts.Count);
        }

        [Test]
        public void AreaPayloadDropsExactlyOneSpellPod()
        {
            _scene.projectileObject.AddComponent<AreaOfEffectProjectileBehaviour>();
            _scene.observer.Init(_scene.source);
            _scene.projectile.OnHit.Invoke(new OnHitData { target = _scene.first });
            _scene.projectile.OnHit.Invoke(new OnHitData { target = _scene.second });
            TipDrop[] drops = Object.FindObjectsByType<TipDrop>();
            foreach (TipDrop drop in drops)
            {
                Object.DestroyImmediate(drop.gameObject);
            }

            Assert.AreEqual(1, drops.Length);
        }
    }
}
