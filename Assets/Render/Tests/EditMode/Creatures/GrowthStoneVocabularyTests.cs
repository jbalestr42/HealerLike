using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Grammar;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Creatures
{
    public class GrowthStoneVocabularyTests
    {
        LookVocabulary _vocabulary;

        [SetUp]
        public void SetUp()
        {
            _vocabulary = Object.Instantiate(RenderTestAssets.LoadLookVocabulary());
            GrowthStoneVocabulary.Apply(_vocabulary);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_vocabulary);
        }

        [Test]
        public void Reset_ChangesOnlyShapeVocabularyAndRetainsPalette()
        {
            LookPalette palette = _vocabulary.palette;
            string colours = EditorJsonUtility.ToJson(palette);
            GrowthStoneVocabulary.Apply(_vocabulary);
            Assert.AreSame(palette, _vocabulary.palette);
            Assert.AreEqual(colours, EditorJsonUtility.ToJson(palette));
            Assert.AreEqual(12, _vocabulary.heads.Count);
            Assert.AreEqual(11, _vocabulary.accessories.Count);
        }

        [Test]
        public void AllFragments_HaveEditableValidProfilesAndDistinctMaterialConstruction()
        {
            foreach (LookVocabulary.HeadEntry head in _vocabulary.heads.Values)
            {
                CheckProfiles(head.plant, false);
                CheckProfiles(head.stone, true);
            }
            foreach (LookVocabulary.AccessoryEntry accessory in _vocabulary.accessories.Values)
            {
                CheckProfiles(accessory.plant, false);
                CheckProfiles(accessory.stone, true);
            }
            foreach (LookVocabulary.BodyEntry body in _vocabulary.bodies.Values)
            {
                CheckProfiles(body.plant, false);
                CheckProfiles(body.stone, true);
            }
        }

        static void CheckProfiles(LookPart[] parts, bool stone)
        {
            Assert.IsNotEmpty(parts);
            foreach (LookPart part in parts)
            {
                Assert.IsTrue(part.shape.isProcedural, part.id);
                Assert.IsTrue(part.shape.IsValid(), part.id);
                bool mineral = part.shape.kind == ShapeKind.Block || part.shape.kind == ShapeKind.Shard
                    || (part.shape.kind == ShapeKind.Ring && part.shape.faceted);
                Assert.AreEqual(stone, mineral, part.id);
            }
        }

        [Test]
        public void FullGrammar_AllHeadsCountsAccessoriesAndMiniHeadsAtHeavyMass_PreservesCopiesWithinBudget()
        {
            int maximum = 0;
            string largest = null;
            foreach (LookSide side in Enum.GetValues(typeof(LookSide)))
            foreach (HeadKind head in Enum.GetValues(typeof(HeadKind)))
            foreach (CountBand count in Enum.GetValues(typeof(CountBand)))
            foreach (AccessoryKind accessory in Enum.GetValues(typeof(AccessoryKind)))
            {
                HeadKind[] miniHeads = accessory == AccessoryKind.MiniHead
                    ? (HeadKind[])Enum.GetValues(typeof(HeadKind)) : new[] { HeadKind.Bud };
                foreach (HeadKind mini in miniHeads)
                {
                    UnitChannels channels = RenderTestAssets.CreateChannels(side, head, count, StemBand.Quick,
                        MassBand.Heavy, accessory);
                    channels.accessoryHead = mini;
                    PartList layout = LookComposer.Layout(channels, _vocabulary);
                    int expected = head == HeadKind.Arch ? 1 : LookComposer.Copies(count);
                    Assert.AreEqual(expected, layout.headStarts.Count, channels.ToString());
                    Assert.LessOrEqual(layout.count, _vocabulary.maxParts, side + " " + head + " " + count + " " + accessory);
                    if (layout.count > maximum)
                    {
                        maximum = layout.count;
                        largest = side + " " + head + " " + count + " " + accessory + " " + mini;
                    }
                }
            }
            TestContext.WriteLine("Maximum complete grammar parts: " + maximum + "; " + largest);
        }

        [TestCase(LookSide.Plant)]
        [TestCase(LookSide.Stone)]
        public void Arch_CountBandsKeepExactlyOneThreeFiveHangingPods(LookSide side)
        {
            LookVocabulary.HeadEntry arch = _vocabulary.heads[HeadKind.Arch];
            LookPart[] fragment = side == LookSide.Plant ? arch.plant : arch.stone;
            foreach (CountBand count in Enum.GetValues(typeof(CountBand)))
            {
                Assert.AreEqual(LookComposer.Copies(count), fragment.Count(p => p.role == PartRole.Tip && p.minCount <= count));
            }
        }

        [TestCase(LookSide.Plant)]
        [TestCase(LookSide.Stone)]
        public void Fork_HasTwoSeparatedLobesAndArchHasOpenSpace(LookSide side)
        {
            PartList layout = LookComposer.Layout(RenderTestAssets.CreateChannels(side, HeadKind.Fork), _vocabulary);
            LookPart[] lobes = Enumerable.Range(0, layout.count).Select(layout.Source)
                .Where(p => p.id == "ForkLeft" || p.id == "ForkRight").OrderBy(p => p.position.x).ToArray();
            Assert.AreEqual(2, lobes.Length);
            float gap = lobes[1].position.x - lobes[1].size.x * 0.5f - lobes[0].position.x - lobes[0].size.x * 0.5f;
            float scale = _vocabulary.bodies[MassBand.Light].scale * (side == LookSide.Stone ? _vocabulary.stoneScale : 1f);
            Assert.Greater(gap, 0.3f * scale, "The U must remain a large empty region.");
            LookPart[] arch = side == LookSide.Plant ? _vocabulary.heads[HeadKind.Arch].plant : _vocabulary.heads[HeadKind.Arch].stone;
            LookPart pod = arch.First(p => p.role == PartRole.Tip && p.minCount == CountBand.One);
            Assert.Greater(pod.position.x, 0.7f, "A single Arch must read as an offset hanging organ.");
        }

        [Test]
        public void Fork_ProfileBendEdit_KeepsBaseAndSeparateAccentConnected()
        {
            UnitChannels channels = RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.Fork);
            PartList before = LookComposer.Layout(channels, _vocabulary);
            int left = Enumerable.Range(0, before.count).First(i => before.Source(i).id == "ForkLeft");
            Vector3 originalBase = Pole(before.Source(left), ShapeAnchor.Bottom);
            LookPart[] fragment = _vocabulary.heads[HeadKind.Fork].plant;
            int entry = Array.FindIndex(fragment, part => part.id == "ForkLeft");
            LookPart edit = fragment[entry];
            edit.shape.bend = 0.85f;
            fragment[entry] = edit;
            PartList after = LookComposer.Layout(channels, _vocabulary);
            Assert.Less(Vector3.Distance(originalBase, Pole(after.Source(left), ShapeAnchor.Bottom)), 0.00001f);
            Vector3 lobeTip = Pole(after.Source(left), ShapeAnchor.Top);
            Vector3 accentBase = Pole(after.Source(left + 1), ShapeAnchor.Bottom);
            Assert.Less(Vector3.Distance(lobeTip + Vector3.down * (0.06f * _vocabulary.bodies[MassBand.Light].scale),
                accentBase), 0.00001f);
            Assert.Greater(Vector3.Distance(Pole(before.Source(left), ShapeAnchor.Top), lobeTip), 0.02f);
        }

        static Vector3 Pole(LookPart part, ShapeAnchor anchor)
        {
            return part.position + Quaternion.Euler(part.euler)
                * Vector3.Scale(ProceduralShapeMeshes.Anchor(part.shape, anchor), part.size);
        }

        [Test]
        public void Bands_KeepCadenceMassAndReachMeanings()
        {
            Assert.Greater(_vocabulary.stems[StemBand.Quick].length, _vocabulary.stems[StemBand.Steady].length);
            Assert.Greater(_vocabulary.stems[StemBand.Steady].length, _vocabulary.stems[StemBand.Slow].length);
            Assert.Less(_vocabulary.stems[StemBand.Quick].thickness, _vocabulary.stems[StemBand.Slow].thickness);
            Assert.Greater(_vocabulary.stems[StemBand.Quick].limbLength, _vocabulary.stems[StemBand.Slow].limbLength);
            Assert.Greater(_vocabulary.bodies[MassBand.Sturdy].plant[0].size.x, _vocabulary.bodies[MassBand.Light].plant[0].size.x);
            Assert.AreEqual(2, _vocabulary.bodies[MassBand.Heavy].plant.Length);
            Assert.AreEqual(2, _vocabulary.bodies[MassBand.Heavy].stone.Length);
            Assert.Less(_vocabulary.bodies[MassBand.Heavy].plant[1].position.y, 0f);
            Assert.Less(_vocabulary.bodies[MassBand.Heavy].stone[1].position.y, 0f);
            Assert.Greater(_vocabulary.Reach(ReachBand.Long), _vocabulary.Reach(ReachBand.Mid));
            Assert.Greater(_vocabulary.Reach(ReachBand.Mid), _vocabulary.Reach(ReachBand.Short));
        }

        [Test]
        public void StoneBodies_AllMassesAndCadences_ConnectBothLegsAndEveryHeadBase()
        {
            foreach (MassBand mass in Enum.GetValues(typeof(MassBand)))
            foreach (StemBand stem in Enum.GetValues(typeof(StemBand)))
            foreach (HeadKind head in Enum.GetValues(typeof(HeadKind)))
            {
                PartList layout = LookComposer.Layout(RenderTestAssets.CreateChannels(LookSide.Stone, head,
                    stem: stem, mass: mass), _vocabulary);
                LookPart[] parts = Enumerable.Range(0, layout.count).Select(layout.Source).ToArray();
                LookPart[] bodies = parts.Where(p => p.role == PartRole.Body).ToArray();
                foreach (LookPart limb in parts.Where(p => p.role == PartRole.Limb))
                {
                    Assert.IsTrue(bodies.Any(body => Box(body).Intersects(Box(limb))), mass + " " + stem + " leg");
                }
                Bounds baseBody = Box(bodies[0]);
                bool headConnected = parts.Skip(layout.headStarts[0]).Any(p => Box(p).Intersects(baseBody));
                Assert.IsTrue(headConnected, mass + " " + stem + " " + head + " head socket");
            }
        }

        [TestCase(CountBand.Few)]
        [TestCase(CountBand.Many)]
        public void StoneFans_KeepEveryOuterHeadAttachedToItsSupportingSlab(CountBand count)
        {
            foreach (HeadKind head in Enum.GetValues(typeof(HeadKind)))
            {
                if (_vocabulary.heads[head].carriesCount) continue;
                PartList layout = LookComposer.Layout(RenderTestAssets.CreateChannels(LookSide.Stone, head, count), _vocabulary);
                for (int copy = 0; copy < layout.headStarts.Count; copy++)
                {
                    int first = layout.headStarts[copy];
                    LookPart support = layout.Source(first);
                    if (support.id != HeadFan.BranchId) continue; // The central copy sits on the body.
                    int end = copy + 1 < layout.headStarts.Count ? layout.headStarts[copy + 1] : layout.count;
                    Assert.IsTrue(Box(support).Intersects(Box(layout.Source(0))), head + " support to body");
                    bool headConnected = Enumerable.Range(first + 1, end - first - 1)
                        .Any(i => Box(layout.Source(i)).Intersects(Box(support)));
                    Assert.IsTrue(headConnected, head + " outer head to support");
                }
            }
        }

        [Test]
        public void ReferenceCollarsCrownsAndPairedSeeds_AreCenteredWhileSideShootsRemainAsymmetric()
        {
            AccessoryKind[] centered = { AccessoryKind.TierRings, AccessoryKind.SmallTorus,
                AccessoryKind.ThornCollar, AccessoryKind.ConeCrown, AccessoryKind.TwinSeeds, AccessoryKind.ShardBarbs };
            foreach (AccessoryKind accessory in Enum.GetValues(typeof(AccessoryKind)))
            {
                if (accessory == AccessoryKind.None) continue;
                LookVocabulary.AccessoryEntry entry = _vocabulary.accessories[accessory];
                Assert.AreEqual(centered.Contains(accessory), entry.isCentered, accessory.ToString());
                if (entry.isCentered)
                {
                    Assert.AreEqual(0f, entry.plant.Sum(p => p.position.x), 0.001f, accessory.ToString());
                    Assert.AreEqual(0f, entry.stone.Sum(p => p.position.x), 0.001f, accessory.ToString());
                }
            }
            Assert.AreEqual(3, _vocabulary.accessories[AccessoryKind.TierRings].plant.Length);
            Assert.AreEqual(2, _vocabulary.accessories[AccessoryKind.TwinSeeds].plant.Count(p => p.id == "TwinSeed"));
        }

        static Bounds Box(LookPart part)
        {
            Quaternion rotation = Quaternion.Euler(part.euler);
            Bounds bounds = new Bounds(part.position, Vector3.zero);
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 corner = Vector3.Scale(part.size * 0.5f, new Vector3(x, y, z));
                bounds.Encapsulate(part.position + rotation * corner);
            }
            return bounds;
        }

        [TestCase(Entity.EntityType.Player)]
        [TestCase(Entity.EntityType.Computer)]
        public void FullRealRoster_ComposesAllSourcesWithUnchangedDerivationAndSeparateProfiles(Entity.EntityType side)
        {
            string[] guids = AssetDatabase.FindAssets("t:EntityData", new[] { "Assets" });
            Assert.AreEqual(12, guids.Length);
            foreach (string guid in guids)
            {
                EntityData source = AssetDatabase.LoadAssetAtPath<EntityData>(AssetDatabase.GUIDToAssetPath(guid));
                string before = EditorJsonUtility.ToJson(source);
                UnitChannels channels = LookDerivation.Channels(source, side);
                CreatureRecipe recipe = LookComposer.Compose(channels, _vocabulary);
                try
                {
                    Assert.NotNull(recipe, source.name);
                    Assert.IsTrue(recipe.parts.All(p => p.shape.isProcedural), source.name);
                    Assert.AreEqual(side == Entity.EntityType.Player ? _vocabulary.rootCount : 0, recipe.roots.count);
                    Assert.AreEqual(side == Entity.EntityType.Computer ? 2 : 0,
                        recipe.parts.Count(p => p.role == PartRole.Limb), source.name);
                    Assert.AreEqual(channels, LookDerivation.Channels(source, side));
                    Assert.AreEqual(before, EditorJsonUtility.ToJson(source));
                }
                finally
                {
                    if (recipe) Object.DestroyImmediate(recipe);
                }
            }
        }
    }
}
