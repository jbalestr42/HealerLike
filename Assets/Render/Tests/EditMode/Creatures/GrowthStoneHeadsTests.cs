using System.Linq;
using System;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using HealerLike.Render.Grammar;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Creatures
{

public class GrowthStoneHeadsTests : GrowthStoneFixture
{
    [TestCase(LookSide.Plant)]
    [TestCase(LookSide.Stone)]
    public void Arch_CountBandsKeepExactlyOneThreeFiveHangingPods(LookSide side)
    {
        LookVocabulary.HeadEntry arch = _vocabulary.heads[HeadKind.Arch];
        LookPart[] fragment = side == LookSide.Plant ? arch.plant : arch.stone;
        foreach (CountBand count in Enum.GetValues(typeof(CountBand)))
        {
            Assert.AreEqual(
                LookComposer.Copies(count),
                fragment.Count(p => p.role == PartRole.Tip && p.minCount <= count)
            );
        }
    }

    [TestCase(LookSide.Plant, CountBand.Few)]
    [TestCase(LookSide.Plant, CountBand.Many)]
    [TestCase(LookSide.Stone, CountBand.Few)]
    [TestCase(LookSide.Stone, CountBand.Many)]
    public void Arch_CountPodsRemainSeparatedAndClearTheTrunkAcrossViewingYaws(LookSide side, CountBand count)
    {
        LookVocabulary.HeadEntry arch = _vocabulary.heads[HeadKind.Arch];
        LookPart[] parts = side == LookSide.Plant ? arch.plant : arch.stone;
        LookPart[] pods = parts.Where(p => p.role == PartRole.Tip && p.minCount <= count).ToArray();
        LookPart[] trunk = parts
            .Where(p => p.minCount == CountBand.One && (p.id == "ArchPier" || p.id == "Growth"))
            .ToArray();
        Assert.AreEqual(LookComposer.Copies(count), pods.Length);
        foreach (float yaw in new[] { -45f, -30f, 0f, 30f, 45f })
        {
            Vector3 right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            LookPart[] ordered = pods.OrderBy(p => Vector3.Dot(p.position, right)).ToArray();
            for (int i = 1; i < ordered.Length; i++)
            {
                float gap = Project(ordered[i], right).x - Project(ordered[i - 1], right).y;
                Assert.Greater(gap, 0.03f, side + " " + count + " pod separation at yaw " + yaw);
            }

            foreach (LookPart pod in pods)
            {
                foreach (LookPart support in trunk)
                {
                    Bounds bounds = Box(support);
                    if (pod.position.y <= bounds.min.y || pod.position.y >= bounds.max.y)
                    {
                        continue;
                    }

                    Vector2 organ = Project(pod, right),
                        obstacle = Project(support, right);
                    float gap = Mathf.Max(organ.x - obstacle.y, obstacle.x - organ.y);
                    Assert.Greater(gap, 0.015f, side + " " + count + " pod behind trunk at yaw " + yaw);
                }
            }
        }
    }

    [TestCase(LookSide.Plant)]
    [TestCase(LookSide.Stone)]
    public void Fork_HasTwoSeparatedLobesAndArchHasOpenSpace(LookSide side)
    {
        PartList layout = LookComposer.Layout(RenderTestAssets.CreateChannels(side, HeadKind.Fork), _vocabulary);
        LookPart[] lobes = Enumerable
            .Range(0, layout.count)
            .Select(layout.Source)
            .Where(p => p.id == "ForkLeft" || p.id == "ForkRight")
            .OrderBy(p => p.position.x)
            .ToArray();
        Assert.AreEqual(2, lobes.Length);
        float gap = lobes[1].position.x - lobes[1].size.x * 0.5f - lobes[0].position.x - lobes[0].size.x * 0.5f;
        float scale =
            _vocabulary.bodies[MassBand.Light].scale * (side == LookSide.Stone ? _vocabulary.stoneScale : 1f);
        Assert.Greater(gap, 0.3f * scale, "The U must remain a large empty region.");
        LookPart[] arch =
            side == LookSide.Plant
                ? _vocabulary.heads[HeadKind.Arch].plant
                : _vocabulary.heads[HeadKind.Arch].stone;
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
        edit.shape.bend = 0.3f;
        fragment[entry] = edit;
        PartList after = LookComposer.Layout(channels, _vocabulary);
        Assert.Less(Vector3.Distance(originalBase, Pole(after.Source(left), ShapeAnchor.Bottom)), 0.00001f);
        Vector3 lobeTip = Pole(after.Source(left), ShapeAnchor.Top);
        Vector3 accentBase = Pole(after.Source(left + 1), ShapeAnchor.Bottom);
        Assert.Less(
            Vector3.Distance(
                lobeTip + Vector3.down * (0.06f * _vocabulary.bodies[MassBand.Light].scale),
                accentBase
            ),
            0.00001f
        );
        Assert.Greater(Vector3.Distance(Pole(before.Source(left), ShapeAnchor.Top), lobeTip), 0.02f);
    }

    [Test]
    public void MineralForkAndHeal_AreBroadSeatedBlocksWithoutBotanicalBranches()
    {
        LookPart[] fork = _vocabulary.heads[HeadKind.Fork].stone;
        Assert.IsFalse(fork.Any(p => p.id == "ForkBranch"));
        foreach (LookPart slab in fork.Where(p => p.id == "ForkLeft" || p.id == "ForkRight"))
        {
            Assert.AreEqual(ShapeKind.Block, slab.shape.kind);
            Assert.LessOrEqual(slab.shape.taper, 0.2f);
            Assert.Greater(slab.size.x / slab.size.y, 0.45f);
            Assert.Less(slab.position.y, 0.05f, "The slab starts directly on the body.");
        }

        LookPart[] heal = _vocabulary.heads[HeadKind.GiftHeal].stone;
        Assert.IsFalse(heal.Any(p => p.id == "HealBranch"));
        Assert.AreEqual(3, heal.Count(p => p.role == PartRole.Tip));
        Assert.IsTrue(FragmentPlacement.TryResolve(heal, CountBand.One, 17, 0, out LookPart[] resolved, out _));
        Assert.Less(resolved.Max(p => Box(p).max.y), 1.2f, "The mineral heal crown is a compact stack.");
    }

    [Test]
    public void FamilyProportions_LetForkAndCalyxGrowNearTheBodyWhileSpearRemainsAReed()
    {
        float Stalk(HeadKind head)
        {
            UnitSockets sockets = UnitSockets.Place(
                RenderTestAssets.CreateChannels(LookSide.Plant, head),
                _vocabulary
            );
            return sockets.neck.y - sockets.stemFoot.y;
        }

        Assert.Less(Stalk(HeadKind.Fork), Stalk(HeadKind.Bud));
        Assert.Less(Stalk(HeadKind.GiftBoonDefence), Stalk(HeadKind.Bud));
        Assert.Less(Stalk(HeadKind.Bud), Stalk(HeadKind.Arch));
        Assert.Less(Stalk(HeadKind.Arch), Stalk(HeadKind.Spear));
        LookPart lance = _vocabulary.heads[HeadKind.Spear].plant.First(p => p.id == "SpearBlade");
        Assert.Greater(lance.size.y / lance.size.x, 3.5f);
        Assert.Greater(lance.shape.taper, 0.6f);
        LookPart[] growth = _vocabulary
            .heads[HeadKind.Arch]
            .plant.Where(p => p.id == "Growth" && p.minCount == CountBand.One)
            .ToArray();
        Assert.Greater(growth.Max(p => p.size.x) / growth.Min(p => p.size.x), 1.5f);
        Assert.IsTrue(growth.All(p => p.shape.fullness > 0.6f));
    }

    [Test]
    public void Arch_GrowsArticulatedFromTheBaseAndKeepsItsCrownDominant()
    {
        foreach (StemBand cadence in Enum.GetValues(typeof(StemBand)))
        {
            foreach (MassBand mass in Enum.GetValues(typeof(MassBand)))
            {
                UnitChannels channels = RenderTestAssets.CreateChannels(
                    LookSide.Plant,
                    HeadKind.Arch,
                    stem: cadence,
                    mass: mass
                );
                PartList layout = LookComposer.Layout(channels, _vocabulary);
                LookPart[] parts = Enumerable.Range(0, layout.count).Select(layout.Source).ToArray();
                Assert.IsFalse(parts.Any(p => p.id == "Stem"), "The crook must start as articulated growth.");
                LookPart[] links = parts.Where(p => p.id == "StemGrowth").ToArray();
                Assert.AreEqual(2, links.Length);
                Assert.AreEqual(1, parts.Count(p => p.id == "StemJoint"));
                Assert.IsTrue(links.All(p => p.size.x >= 0.35f));
                LookPart[] crown = parts.Skip(layout.headStarts[0]).ToArray();
                float crownSpan = crown.Max(p => Box(p).max.y) - crown.Min(p => Box(p).min.y);
                Assert.Greater(crownSpan, _vocabulary.bodies[mass].plant[0].size.y * 2f);
            }
        }
    }

    [Test]
    public void ForkAndMineralSlabs_UseStructuralCurvatureAndRidges()
    {
        foreach (
            LookPart leaf in _vocabulary
                .heads[HeadKind.Fork]
                .plant.Where(p => p.id.StartsWith("ForkL") || p.id == "ForkRight")
        )
        {
            Assert.Less(leaf.shape.bow, -0.7f);
            Assert.Greater(leaf.size.x, 0.8f);
        }

        foreach (
            LookPart slab in _vocabulary
                .heads[HeadKind.Fork]
                .stone.Where(p => p.id == "ForkLeft" || p.id == "ForkRight")
        )
        {
            Assert.Greater(slab.shape.ridge, 0.6f);
        }

        Assert.Greater(_vocabulary.heads[HeadKind.Bud].stone[0].shape.ridge, 0.5f);
        Assert.AreEqual(0f, _vocabulary.stems[StemBand.Quick].stoneLimbShape.ridge);
    }
}
}
