using System.Linq;
using System;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using HealerLike.Render.Grammar;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Creatures
{

public class GrowthStoneVocabularyTests : GrowthStoneFixture
{
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
            bool mineral =
                part.shape.kind == ShapeKind.Block
                || part.shape.kind == ShapeKind.Shard
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
        {
            foreach (HeadKind head in Enum.GetValues(typeof(HeadKind)))
            {
                foreach (CountBand count in Enum.GetValues(typeof(CountBand)))
                {
                    foreach (AccessoryKind accessory in Enum.GetValues(typeof(AccessoryKind)))
                    {
                        HeadKind[] miniHeads =
                            accessory == AccessoryKind.MiniHead
                                ? (HeadKind[])Enum.GetValues(typeof(HeadKind))
                                : new[] { HeadKind.Bud };
                        foreach (HeadKind mini in miniHeads)
                        {
                            UnitChannels channels = RenderTestAssets.CreateChannels(
                                side,
                                head,
                                count,
                                StemBand.Quick,
                                MassBand.Heavy,
                                accessory
                            );
                            channels.accessoryHead = mini;
                            PartList layout = LookComposer.Layout(channels, _vocabulary);
                            int expected = head == HeadKind.Arch ? 1 : LookComposer.Copies(count);
                            Assert.AreEqual(expected, layout.headStarts.Count, channels.ToString());
                            Assert.LessOrEqual(
                                layout.count,
                                _vocabulary.maxParts,
                                side + " " + head + " " + count + " " + accessory
                            );
                            if (layout.count > maximum)
                            {
                                maximum = layout.count;
                                largest = side + " " + head + " " + count + " " + accessory + " " + mini;
                            }
                        }
                    }
                }
            }
        }

        TestContext.WriteLine("Maximum complete grammar parts: " + maximum + "; " + largest);
    }

    [Test]
    public void Bands_KeepCadenceMassAndReachMeanings()
    {
        Assert.Greater(
            _vocabulary.heads[HeadKind.Bud].plant[0].size.x * _vocabulary.bodies[MassBand.Light].scale,
            _vocabulary.bodies[MassBand.Light].plant[0].size.x * 1.4f,
            "Normal remains head-led."
        );
        Assert.Greater(_vocabulary.stems[StemBand.Quick].length, _vocabulary.stems[StemBand.Steady].length);
        Assert.Greater(_vocabulary.stems[StemBand.Steady].length, _vocabulary.stems[StemBand.Slow].length);
        Assert.Less(_vocabulary.stems[StemBand.Quick].thickness, _vocabulary.stems[StemBand.Slow].thickness);
        Assert.Greater(_vocabulary.stems[StemBand.Quick].limbLength, _vocabulary.stems[StemBand.Slow].limbLength);
        Assert.Greater(
            _vocabulary.bodies[MassBand.Sturdy].plant[0].size.x,
            _vocabulary.bodies[MassBand.Light].plant[0].size.x
        );
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
        {
            foreach (StemBand stem in Enum.GetValues(typeof(StemBand)))
            {
                foreach (HeadKind head in Enum.GetValues(typeof(HeadKind)))
                {
                    PartList layout = LookComposer.Layout(
                        RenderTestAssets.CreateChannels(LookSide.Stone, head, stem: stem, mass: mass),
                        _vocabulary
                    );
                    LookPart[] parts = Enumerable.Range(0, layout.count).Select(layout.Source).ToArray();
                    LookPart[] bodies = parts.Where(p => p.role == PartRole.Body).ToArray();
                    foreach (LookPart limb in parts.Where(p => p.role == PartRole.Limb))
                    {
                        Assert.IsTrue(
                            bodies.Any(body => Box(body).Intersects(Box(limb))),
                            mass + " " + stem + " leg"
                        );
                    }

                    Bounds baseBody = Box(bodies[0]);
                    bool headConnected = parts.Skip(layout.headStarts[0]).Any(p => Box(p).Intersects(baseBody));
                    Assert.IsTrue(headConnected, mass + " " + stem + " " + head + " head socket");
                }
            }
        }
    }

    [TestCase(CountBand.Few)]
    [TestCase(CountBand.Many)]
    public void StoneFans_KeepEveryOuterHeadAttachedToItsSupportingSlab(CountBand count)
    {
        foreach (HeadKind head in Enum.GetValues(typeof(HeadKind)))
        {
            if (_vocabulary.heads[head].carriesCount)
            {
                continue;
            }

            PartList layout = LookComposer.Layout(
                RenderTestAssets.CreateChannels(LookSide.Stone, head, count),
                _vocabulary
            );
            for (int copy = 0; copy < layout.headStarts.Count; copy++)
            {
                int first = layout.headStarts[copy];
                LookPart support = layout.Source(first);
                if (support.id != HeadFan.BranchId)
                {
                    continue; // The central copy sits on the body.
                }

                int end = copy + 1 < layout.headStarts.Count ? layout.headStarts[copy + 1] : layout.count;
                Assert.IsTrue(Box(support).Intersects(Box(layout.Source(0))), head + " support to body");
                bool headConnected = Enumerable
                    .Range(first + 1, end - first - 1)
                    .Any(i => Box(layout.Source(i)).Intersects(Box(support)));
                Assert.IsTrue(headConnected, head + " outer head to support");
            }
        }
    }

    [Test]
    public void MineralLegs_AllBandsAndMasses_RemainShortBlocks()
    {
        foreach (StemBand stem in Enum.GetValues(typeof(StemBand)))
        {
            foreach (MassBand mass in Enum.GetValues(typeof(MassBand)))
            {
                PartList layout = LookComposer.Layout(
                    RenderTestAssets.CreateChannels(LookSide.Stone, HeadKind.Bud, stem: stem, mass: mass),
                    _vocabulary
                );
                LookPart[] legs = Enumerable
                    .Range(0, layout.count)
                    .Select(layout.Source)
                    .Where(p => p.role == PartRole.Limb)
                    .ToArray();
                Assert.AreEqual(2, legs.Length);
                Assert.AreNotEqual(legs[0].size, legs[1].size, "The feet remain two unequal mineral blocks.");
                foreach (LookPart leg in legs)
                {
                    Assert.AreEqual(ShapeKind.Block, leg.shape.kind);
                    Assert.Less(leg.size.y / leg.size.x, 1.7f, stem + " " + mass);
                }
            }
        }
    }

    [Test]
    public void SharedMassProfile_KeepsHeavyArchLowerThanQuickLightArch()
    {
        PartList light = LookComposer.Layout(
            RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.Arch, stem: StemBand.Quick),
            _vocabulary
        );
        PartList heavy = LookComposer.Layout(
            RenderTestAssets.CreateChannels(
                LookSide.Plant,
                HeadKind.Arch,
                stem: StemBand.Steady,
                mass: MassBand.Heavy
            ),
            _vocabulary
        );
        float lightTop = Enumerable.Range(0, light.count).Select(i => Box(light.Source(i)).max.y).Max();
        float heavyTop = Enumerable.Range(0, heavy.count).Select(i => Box(heavy.Source(i)).max.y).Max();
        Assert.Greater(lightTop - heavyTop, 0.2f);
        Assert.AreEqual(
            _vocabulary.bodies[MassBand.Light].effectiveHeadScale,
            _vocabulary.bodies[MassBand.Heavy].effectiveHeadScale
        );
        Assert.AreEqual(2, _vocabulary.bodies[MassBand.Heavy].plant.Length);
    }

    [Test]
    public void MineralVocabulary_UsesClippedPlanesWhileLegsKeepAQuieterProfile()
    {
        foreach (LookVocabulary.HeadEntry entry in _vocabulary.heads.Values)
        {
            foreach (LookPart part in entry.stone)
            {
                if (part.shape.kind == ShapeKind.Block || part.shape.kind == ShapeKind.Shard)
                {
                    Assert.Greater(part.shape.fracture, 0.4f, part.id);
                }
            }
        }

        Assert.Greater(
            _vocabulary.bodies[MassBand.Light].stone[0].shape.fracture,
            _vocabulary.stems[StemBand.Quick].stoneLimbShape.fracture
        );
    }
}
}
