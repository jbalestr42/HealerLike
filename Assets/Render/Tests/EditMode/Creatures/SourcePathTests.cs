using System;
using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Grammar;
using HealerLike.Render.Deliveries;
using HealerLike.Render.Spells;
using HealerLike.Render.Stones;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Creatures
{
    public class SourcePathHost : ARigHost
    {
        public void Build(CreatureRecipe recipe) => BuildRig(recipe, transform, RenderTestAssets.LoadLookMaterial(),
            RenderTestAssets.LoadLookMaterial(), RenderTestAssets.LoadMeshes(), RenderTestAssets.LoadDeliveryVocabulary(), 1f);
        public void Release() => ReleaseRig();
        public override void OnHealthResolved(GameObject target, float value, bool critical) { }
    }

    public class SourcePathTests
    {
        GameObject _owner, _target, _shotObject, _effectObject;
        SourcePathHost _host;
        CreatureRecipe _recipe;
        Projectile _projectile;

        [SetUp]
        public void SetUp()
        {
            _owner = new GameObject("source");
            _target = new GameObject("target");
            _target.transform.position = Vector3.right * 12f;
            _recipe = LookComposer.Compose(RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.Arch, CountBand.Many),
                RenderTestAssets.LoadLookVocabulary());
            _host = _owner.AddComponent<SourcePathHost>();
            _host.Build(_recipe);
            _shotObject = new GameObject("logical projectile");
            _projectile = _shotObject.AddComponent<Projectile>();
            _projectile.source = _owner;
            _projectile.targetPoint = _target;
            TestHelpers.SetPrivateField(_projectile, "_target", _target);
        }

        [TearDown]
        public void TearDown()
        {
            FreeShot free = _shotObject ? _shotObject.GetComponent<FreeShot>() : null;
            if (free) TestHelpers.InvokePrivate(free, "OnDestroy");
            _host.Release();
            Object.DestroyImmediate(_effectObject);
            Object.DestroyImmediate(_shotObject);
            Object.DestroyImmediate(_owner);
            Object.DestroyImmediate(_target);
            Object.DestroyImmediate(_recipe);
        }

        [Test]
        public void FreeShot_StartAndLiveCompensationUseHeldOutlet_WithoutMovingGameplayObjects()
        {
            FreeShot free = _shotObject.AddComponent<FreeShot>();
            Vector3 ownerStart = _owner.transform.position;
            Assert.IsTrue(free.Init(_projectile, DeliveryStyle.Direct, RenderTestAssets.LoadDeliveryVocabulary(),
                RenderTestAssets.LoadMeshes()));
            string id = CreatureSources.Select(_host.rig, 0);
            CreatureSources.Resolve(_host.rig, id, out Vector3 outlet);
            Assert.Less(Vector3.Distance(outlet, free.visualPosition), 0.00001f);
            Assert.AreEqual(Vector3.zero, _projectile.transform.position);
            _projectile.transform.position = Vector3.right * 3f;
            _host.rig.Tick(0.3f, 0.3f, new FootFrame(Vector3.forward * 0.3f, Vector3.up, 1f));
            TestHelpers.InvokePrivate(free, "LateUpdate");
            CreatureSources.Resolve(_host.rig, id, out outlet);
            Assert.Less(Vector3.Distance(Vector3.right * 3f + outlet * 0.75f, free.visualPosition), 0.00001f);
            Assert.AreEqual(Vector3.right * 3f, _projectile.transform.position);
            Assert.AreEqual(ownerStart, _owner.transform.position);
            free.Contact();
            TestHelpers.InvokePrivate(free, "LateUpdate");
            Assert.AreEqual(_projectile.transform.position, free.visualPosition);
        }

        [Test]
        public void FreeShot_RemovedOutletStopsCosmeticEmissionWithoutTeleportingOrMovingProjectile()
        {
            FreeShot free = _shotObject.AddComponent<FreeShot>();
            Assert.IsTrue(free.Init(_projectile, DeliveryStyle.Direct, RenderTestAssets.LoadDeliveryVocabulary(),
                RenderTestAssets.LoadMeshes()));
            TestHelpers.InvokePrivate(free, "LateUpdate");
            Vector3 last = free.visualPosition;
            int index = Array.FindIndex(_recipe.parts, p => p.sourceId == CreatureSources.Select(_host.rig, 0));
            _recipe.parts[index].isSource = false;
            Assert.IsTrue(_host.rig.Recompose(_recipe, RenderTestAssets.LoadLookMaterial(),
                RenderTestAssets.LoadLookMaterial(), RenderTestAssets.LoadMeshes()));
            _projectile.transform.position = Vector3.right * 2f;
            TestHelpers.InvokePrivate(free, "LateUpdate");
            Assert.AreEqual(last, free.visualPosition);
            Assert.AreEqual(Vector3.right * 2f, _projectile.transform.position);
            foreach (Renderer renderer in GameObject.Find("FreeShot").GetComponentsInChildren<Renderer>(true))
                Assert.IsFalse(renderer.gameObject.activeInHierarchy && renderer.enabled);
        }

        [Test]
        public void NonProjectileLink_HoldsSourceThroughRecompose_AndDisposesIfItDisappears()
        {
            EffectRecipe effectRecipe = EffectComposer.Link(RenderTestAssets.LoadEffectVocabulary(), EffectFamily.Renew);
            SpellEffect effect = SpellEffect.Create(effectRecipe, null, RenderTestAssets.LoadMeshes(),
                RenderTestAssets.LoadLookMaterial(), null);
            _effectObject = effect.gameObject;
            effect.SetEndpoints(Vector3.zero, _target.transform.position, false);
            effect.SetCastSource(_owner);
            string id = CreatureSources.Select(_host.rig, 0);
            int index = Array.FindIndex(_recipe.parts, p => p.sourceId == id);
            _recipe.parts[index].dimensions *= 1.5f;
            Assert.IsTrue(_host.rig.Recompose(_recipe, RenderTestAssets.LoadLookMaterial(),
                RenderTestAssets.LoadLookMaterial(), RenderTestAssets.LoadMeshes()));
            effect.Advance(0.1f);
            CreatureSources.Resolve(_host.rig, id, out Vector3 source);
            Vector3 actual = (Vector3)typeof(SpellEffect).GetField("_linkStart",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(effect);
            Assert.Less(Vector3.Distance(actual, source), 0.00001f);
            _recipe.parts[index].isSource = false;
            Assert.IsTrue(_host.rig.Recompose(_recipe, RenderTestAssets.LoadLookMaterial(),
                RenderTestAssets.LoadLookMaterial(), RenderTestAssets.LoadMeshes()));
            effect.Advance(0.1f);
            Assert.IsTrue(!effect);
            Assert.AreEqual(Vector3.zero, _owner.transform.position);
        }

        [Test]
        public void CountChange_KeepsExistingCopiedOutletIdentityAndInvalidatesRemovedCopy()
        {
            using var kept = new CastSourceLease(_host.rig, 0);
            using var removed = new CastSourceLease(_host.rig, 4);
            CreatureRecipe fewer = LookComposer.Compose(RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.Arch),
                RenderTestAssets.LoadLookVocabulary());
            try
            {
                Assert.IsTrue(_host.rig.Recompose(fewer, RenderTestAssets.LoadLookMaterial(),
                    RenderTestAssets.LoadLookMaterial(), RenderTestAssets.LoadMeshes()));
                Assert.IsTrue(kept.TryGet(out _));
                Assert.IsFalse(removed.TryGet(out _));
            }
            finally { Object.DestroyImmediate(fewer); }
        }
    }
}
