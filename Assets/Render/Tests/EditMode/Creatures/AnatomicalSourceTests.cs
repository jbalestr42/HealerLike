using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using HealerLike.Render.Grammar;
using HealerLike.Render.Deliveries;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Creatures
{
    public class AnatomicalSourceTests
    {
        LookVocabulary _vocabulary;
        GameObject _parent;
        CreatureRig _rig;
        CreatureRecipe _recipe;
        ArmPool _pool;
        Material _material;

        [SetUp]
        public void SetUp()
        {
            _vocabulary = Object.Instantiate(RenderTestAssets.LoadLookVocabulary());
            _parent = new GameObject("source fixture");
            _material = RenderTestAssets.LoadLookMaterial();
        }

        [TearDown]
        public void TearDown()
        {
            _pool?.Dispose();
            _rig?.Dispose();
            Object.DestroyImmediate(_recipe);
            Object.DestroyImmediate(_parent);
            Object.DestroyImmediate(_vocabulary);
        }

        static System.Collections.IEnumerable Grammar()
        {
            foreach (LookSide side in Enum.GetValues(typeof(LookSide)))
                foreach (HeadKind head in Enum.GetValues(typeof(HeadKind)))
                    foreach (CountBand count in Enum.GetValues(typeof(CountBand)))
                        yield return new TestCaseData(side, head, count);
        }

        [TestCaseSource(nameof(Grammar))]
        public void SavedGrammar_PreservesEveryOutletWithoutSpendingArms(LookSide side, HeadKind head, CountBand count)
        {
            _recipe = LookComposer.Compose(RenderTestAssets.CreateChannels(side, head, count,
                accessory: AccessoryKind.MiniHead), _vocabulary);
            Assert.NotNull(_recipe);
            int copies = LookComposer.Copies(count);
            int perHead = head == HeadKind.Fork ? 2 : head == HeadKind.GiftBane ? 3
                : head == HeadKind.GiftHeal && side == LookSide.Plant ? 3 : 1;
            int expected = head == HeadKind.Arch ? copies : copies * perHead;
            Assert.AreEqual(expected, _recipe.parts.Count(p => p.isSource));
            Assert.AreEqual(side == LookSide.Plant ? _vocabulary.armCount : 0, _recipe.arms.Length);
            Assert.IsTrue(CreatureValidator.TryValidate(_recipe, out string error), error);
            _rig = RenderTestAssets.CreateRig(_recipe, _parent.transform, _material);
            Assert.IsTrue(_rig.TryGetAnchors(out EffectAnchors anchors));
            Assert.AreEqual(expected, anchors.castSources.Length);
            foreach (CreaturePart part in _rig.parts.Where(p => p.isSource))
            {
                Assert.IsTrue(CreatureSources.Resolve(_rig, part.sourceId, out Vector3 point));
                Assert.IsTrue(RenderMath.IsFinite(point));
                if (head == HeadKind.Arch) Assert.AreEqual(ShapeAnchor.Bottom, part.sourceAnchor);
            }
        }

        [Test]
        public void Reset_RestoresSourceMetadata_AndLiveAnchorReadsReuseAcceptedStorage()
        {
            GrowthStoneVocabulary.Apply(_vocabulary);
            foreach (var head in _vocabulary.heads.Values)
            {
                Assert.IsTrue(head.plant.Any(p => p.isSource));
                Assert.IsTrue(head.stone.Any(p => p.isSource));
            }
            _recipe = LookComposer.Compose(RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.GiftHeal,
                CountBand.Many), _vocabulary);
            Assert.AreEqual(15, _recipe.parts.Count(p => p.isSource));
            _rig = RenderTestAssets.CreateRig(_recipe, _parent.transform, _material);
            _rig.TryGetAnchors(out EffectAnchors first);
            _rig.TryGetAnchors(out EffectAnchors second);
            Assert.AreSame(first.castSources, second.castSources);
        }

        [Test]
        public void Migration_PreservesEveryExistingFieldAndPalette_AndIsIdempotent()
        {
            foreach (var entry in _vocabulary.heads.Values)
                foreach (LookPart[] parts in new[] { entry.plant, entry.stone })
                    for (int i = 0; i < parts.Length; i++)
                    { parts[i].isSource = false; parts[i].sourceAnchor = ShapeAnchor.Center; }
            var before = _vocabulary.heads.ToDictionary(e => e.Key,
                e => new[] { (LookPart[])e.Value.plant.Clone(), (LookPart[])e.Value.stone.Clone() });
            var palette = _vocabulary.palette;
            string colours = EditorJsonUtility.ToJson(palette);
            CreatureSourceMigration.Apply(_vocabulary);
            CreatureSourceMigration.Apply(_vocabulary);
            foreach (var entry in _vocabulary.heads)
            {
                var arrays = new[] { entry.Value.plant, entry.Value.stone };
                for (int side = 0; side < 2; side++)
                    for (int i = 0; i < arrays[side].Length; i++)
                    {
                        LookPart part = arrays[side][i];
                        part.isSource = false;
                        part.sourceAnchor = ShapeAnchor.Center;
                        Assert.AreEqual(JsonUtility.ToJson(before[entry.Key][side][i]), JsonUtility.ToJson(part));
                    }
            }
            Assert.AreSame(palette, _vocabulary.palette);
            Assert.AreEqual(colours, EditorJsonUtility.ToJson(palette));
        }

        [Test]
        public void HeldDelivery_FollowsShapedGeometryGrowthFacingAndRecompose_ThenDisposesOnRemoval()
        {
            _recipe = LookComposer.Compose(RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.Fork,
                CountBand.Many), _vocabulary);
            _rig = RenderTestAssets.CreateRig(_recipe, _parent.transform, _material);
            _pool = new ArmPool();
            _pool.Init(_rig, _material, RenderTestAssets.LoadMeshes(), RenderTestAssets.LoadDeliveryVocabulary());
            using var lease = new CastSourceLease(_rig, 0);
            using var same = new CastSourceLease(_rig, 10);
            Assert.AreEqual(lease.sourceId, same.sourceId);
            Assert.IsTrue(_pool.BeginDelivery(17, DeliveryStyle.Direct, null, Vector3.one * 4f));
            string heldId = lease.sourceId;
            _rig.BeginAppearance();
            for (int frame = 0; frame < 5; frame++)
            {
                _rig.SetPresentationForward(new Vector3(1f, 0f, 1f));
                _rig.SetReadout(frame < 2 ? Vector3.right * 4f : Vector3.back * 4f, 1f, 0f, 0f);
                Assert.AreEqual(heldId, lease.sourceId);
                _rig.Tick(frame * 0.1f, 0.1f, new FootFrame(Vector3.right * frame * 0.02f, Vector3.up, 1f));
                _pool.Tick(0.1f);
                Assert.IsTrue(lease.TryGet(out Vector3 source));
                Assert.Less(Vector3.Distance(source, _pool.GetArm(0).Joint(0)), 0.00001f);
            }
            int index = Array.FindIndex(_recipe.parts, p => p.sourceId == lease.sourceId);
            _recipe.parts[index].shape = ShapeProfile.Leaf(0.6f, 0.7f, -0.5f);
            _recipe.parts[index].localEuler = new Vector3(25f, 60f, 38f);
            _recipe.parts[index].dimensions *= 1.4f;
            Assert.IsTrue(_rig.Recompose(_recipe, _material, _material, RenderTestAssets.LoadMeshes()));
            _pool.Refresh();
            _pool.Tick(0.01f);
            Assert.IsTrue(lease.TryGet(out Vector3 edited));
            Assert.Less(Vector3.Distance(edited, _pool.GetArm(0).Joint(0)), 0.00001f);
            ShapeAnchors.TryGet(_recipe.parts[index].shape, ShapeAnchor.Top, _recipe.parts[index].variant,
                out Vector3 local);
            Assert.Less(Vector3.Distance(edited, _rig.partTransforms[index].TransformPoint(local)), 0.00001f);
            _recipe.parts[index].isSource = false;
            Assert.IsTrue(_rig.Recompose(_recipe, _material, _material, RenderTestAssets.LoadMeshes()));
            _pool.Tick(0f);
            Assert.IsFalse(lease.TryGet(out _));
            Assert.IsNull(_pool.GetArm(0));
            _recipe.parts[index].isSource = true;
            Assert.IsTrue(_rig.Recompose(_recipe, _material, _material, RenderTestAssets.LoadMeshes()));
            Assert.IsFalse(lease.TryGet(out _), "A removed lease never rebinds");
        }

        [Test]
        public void InvalidMetadataRejected_ManualFallbackAndDisposalRetained()
        {
            _recipe = RenderTestAssets.CreateRecipe();
            _rig = RenderTestAssets.CreateRig(_recipe, _parent.transform, _material);
            using var lease = new CastSourceLease(_rig);
            Assert.IsTrue(lease.TryGet(out Vector3 point));
            _rig.TryGetAnchors(out EffectAnchors anchors);
            Assert.AreEqual(anchors.castPoint, point);
            _recipe.parts[0].isSource = true;
            _recipe.parts[0].sourceId = "invalid";
            _recipe.parts[0].sourceAnchor = (ShapeAnchor)42;
            Assert.IsFalse(CreatureValidator.TryValidate(_recipe, out _));
            Assert.IsFalse(_rig.Recompose(_recipe, _material, _material, RenderTestAssets.LoadMeshes()));
            Assert.IsTrue(lease.TryGet(out _));
            _rig.Dispose();
            Assert.IsFalse(lease.TryGet(out _));
        }
    }
}
