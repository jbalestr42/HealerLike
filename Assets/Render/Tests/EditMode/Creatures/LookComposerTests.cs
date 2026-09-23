using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Grammar;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Creatures
{

public class LookComposerTests
{
    readonly List<Object> _objects = new List<Object>();
    GameObject _parent;

    static UnitChannels Channels(LookSide side, HeadKind head, CountBand count = CountBand.One, StemBand stem = StemBand.Steady,
        MassBand mass = MassBand.Light, AccessoryKind accessory = AccessoryKind.None)
    {
        return new UnitChannels
        {
            side = side,
            head = head,
            count = count,
            stem = stem,
            mass = mass,
            reach = ReachBand.Long,
            accessory = accessory,
            accessoryHead = HeadKind.Arch,
            accent = EffectFamily.Damage
        };
    }

    [SetUp]
    public void SetUp()
    {
        _parent = new GameObject("ComposerFixture");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_parent);
        foreach (Object trackedObject in _objects)
        {
            Object.DestroyImmediate(trackedObject);
        }
        _objects.Clear();
    }

    CreatureRecipe Compose(UnitChannels channels)
    {
        CreatureRecipe recipe = LookComposer.Compose(channels);
        _objects.Add(recipe);
        return recipe;
    }

    [TestCase("NormalEntity", Entity.EntityType.Player)]
    [TestCase("FastShootEntity", Entity.EntityType.Player)]
    [TestCase("TripleShootEntity", Entity.EntityType.Player)]
    [TestCase("MultiShotEntity", Entity.EntityType.Player)]
    [TestCase("RandomShootEntity", Entity.EntityType.Player)]
    [TestCase("ChainLightningEntity", Entity.EntityType.Player)]
    [TestCase("ChannelingEntity", Entity.EntityType.Player)]
    [TestCase("SwarmEntity", Entity.EntityType.Player)]
    [TestCase("TestEntity", Entity.EntityType.Player)]
    [TestCase("SoldierEntity", Entity.EntityType.Computer)]
    [TestCase("HitArmorBufferEntityEntity", Entity.EntityType.Computer)]
    public void Compose_LiveEntity_BuildsAValidRigUnderThePartCap(string folder, Entity.EntityType entityType)
    {
        CreatureRecipe recipe = Compose(LookDerivation.Channels(LookDerivationTests.LoadEntity(folder), entityType));

        Assert.NotNull(recipe);
        Assert.LessOrEqual(recipe.parts.Length, LookComposer.MaxParts);
        Assert.IsFalse(AssetDatabase.Contains(recipe));
        Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Default.mat");
        using (CreatureRig rig = CreatureRigTests.CreateRig(recipe, _parent.transform, material))
        {
            rig.Tick(1f, 0.016f, new FootFrame(Vector3.zero, Vector3.up, 1f));
            Assert.NotNull(rig.root);
            Assert.IsNotEmpty(rig.budAnchors);
        }
    }

    [Test]
    public void Compose_EveryHeadCountAndSide_StaysValid()
    {
        foreach (LookSide side in Enum.GetValues(typeof(LookSide)))
        {
            foreach (HeadKind head in Enum.GetValues(typeof(HeadKind)))
            {
                foreach (CountBand count in Enum.GetValues(typeof(CountBand)))
                {
                    CreatureRecipe recipe = Compose(Channels(side, head, count, StemBand.Quick, MassBand.Heavy, AccessoryKind.MiniHead));

                    Assert.NotNull(recipe, $"{side} {head} {count}");
                    Assert.LessOrEqual(recipe.parts.Length, LookComposer.MaxParts, $"{side} {head} {count}");
                }
            }
        }
    }

    [Test]
    public void Compose_EveryAccessoryAndSide_StaysValid()
    {
        foreach (LookSide side in Enum.GetValues(typeof(LookSide)))
        {
            foreach (AccessoryKind accessory in Enum.GetValues(typeof(AccessoryKind)))
            {
                CreatureRecipe recipe = Compose(Channels(side, HeadKind.Bud, accessory: accessory));

                Assert.NotNull(recipe, $"{side} {accessory}");
            }
        }
    }

    [Test]
    public void Compose_Plant_GrowsJointedRootsAtThePinnedReach()
    {
        CreatureRecipe recipe = Compose(Channels(LookSide.Plant, HeadKind.Bud));

        Assert.AreEqual(LookComposer.RootCount, recipe.roots.count);
        Assert.That(recipe.roots.segments, Is.InRange(2, 3));
        Assert.AreEqual(1.3f * LookComposer.BodyUnit, recipe.roots.footRadius, 0.0001f); // pinned at 1.3 body units
        Assert.That(recipe.roots.thickness * 2f / LookComposer.BodyUnit, Is.InRange(0.12f, 0.18f)); // diameter in body units
        Assert.Less(recipe.roots.hipHeight, 0.1f);
        Assert.AreEqual(2, recipe.arms.Length);
    }

    [Test]
    public void Compose_Stone_StandsOnBouldersWithoutRootsOrLianas()
    {
        CreatureRecipe recipe = Compose(Channels(LookSide.Stone, HeadKind.Bud));

        Assert.AreEqual(0, recipe.roots.count);
        Assert.IsEmpty(recipe.arms);
        Assert.AreEqual(Primitive.Boulder, recipe.parts[0].primitive);
        Assert.AreEqual(LookComposer.StoneWilt, recipe.wiltColour);
    }

    [TestCase(ReachBand.Short)]
    [TestCase(ReachBand.Mid)]
    [TestCase(ReachBand.Long)]
    public void Reach_WhilePinned_IsOneValueForEveryBand(ReachBand band)
    {
        Assert.IsTrue(LookComposer.IsReachPinned);
        Assert.AreEqual(LookComposer.PinnedReach, LookComposer.Reach(band));
    }

    [Test]
    public void StemLength_Bands_KeepTheSpecRatio()
    {
        float slow = LookComposer.StemLength(StemBand.Slow);

        Assert.AreEqual(1.6f, LookComposer.StemLength(StemBand.Steady) / slow, 0.0001f);
        Assert.AreEqual(2.4f, LookComposer.StemLength(StemBand.Quick) / slow, 0.0001f);
    }

    [TestCase(CountBand.One, 1)]
    [TestCase(CountBand.Few, 3)]
    [TestCase(CountBand.Many, 5)]
    public void Compose_Count_DrawsOneThreeOrFiveTips(CountBand count, int tips)
    {
        CreatureRecipe recipe = Compose(Channels(LookSide.Plant, HeadKind.Bud, count));

        int found = Array.FindAll(recipe.parts, part => part.id.StartsWith("Bud", StringComparison.Ordinal)).Length;

        Assert.AreEqual(tips, found);
    }

    [TestCase(EffectFamily.Damage)]
    [TestCase(EffectFamily.Heal)]
    [TestCase(EffectFamily.Boon)]
    public void Compose_Accent_SitsOnTheTipsAndNeverOnTheBody(EffectFamily family)
    {
        UnitChannels channels = Channels(LookSide.Plant, HeadKind.Spear, CountBand.Few);
        channels.accent = family;
        Color accent = PartVocabulary.Accent(family);

        CreatureRecipe recipe = Compose(channels);

        foreach (CreaturePart part in recipe.parts)
        {
            bool isTip = part.id.StartsWith("Bud", StringComparison.Ordinal);
            Assert.AreEqual(isTip, part.colour == accent, part.id);
            Assert.AreEqual(isTip, part.glow > 0f, part.id);
        }
    }

    [Test]
    public void Compose_HeavyMass_AddsABasePartRatherThanOnlyScaling()
    {
        int light = Compose(Channels(LookSide.Plant, HeadKind.Bud, mass: MassBand.Light)).parts.Length;
        int heavy = Compose(Channels(LookSide.Plant, HeadKind.Bud, mass: MassBand.Heavy)).parts.Length;

        Assert.AreEqual(light + 1, heavy);
    }
}

}
