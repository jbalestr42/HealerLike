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
        public void FullGrammar_AllHeadsCountsMassesAccessoriesAndMiniHeads_PreservesCopiesWithinBudget()
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
            LookPart[] fork = side == LookSide.Plant ? _vocabulary.heads[HeadKind.Fork].plant : _vocabulary.heads[HeadKind.Fork].stone;
            LookPart[] lobes = fork.Where(p => p.id == "ForkLobe").OrderBy(p => p.position.x).ToArray();
            Assert.AreEqual(2, lobes.Length);
            float gap = lobes[1].position.x - lobes[1].size.x * 0.5f - lobes[0].position.x - lobes[0].size.x * 0.5f;
            Assert.Greater(gap, 0.45f, "The U must remain a large empty region.");
            LookPart[] arch = side == LookSide.Plant ? _vocabulary.heads[HeadKind.Arch].plant : _vocabulary.heads[HeadKind.Arch].stone;
            LookPart pod = arch.First(p => p.role == PartRole.Tip && p.minCount == CountBand.One);
            Assert.Greater(pod.position.x, 0.7f, "A single Arch must read as an offset hanging organ.");
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
