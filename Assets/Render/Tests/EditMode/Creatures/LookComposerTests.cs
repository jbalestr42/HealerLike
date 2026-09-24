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
    LookVocabulary _vocabulary;

    [SetUp]
    public void SetUp()
    {
        _parent = new GameObject("ComposerFixture");
        _vocabulary = RenderTestAssets.LoadLookVocabulary();
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
        CreatureRecipe recipe = LookComposer.Compose(channels, _vocabulary);
        _objects.Add(recipe);
        return recipe;
    }

    static CreaturePart[] FindAll(CreatureRecipe recipe, PartRole role)
    {
        return Array.FindAll(recipe.parts, part => part.role == role);
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
        UnitChannels channels = LookDerivation.Channels(RenderTestAssets.LoadEntity(folder), entityType);

        CreatureRecipe recipe = Compose(channels);

        Assert.NotNull(recipe);
        Assert.LessOrEqual(recipe.parts.Length, _vocabulary.maxParts);
        Assert.IsFalse(AssetDatabase.Contains(recipe));
        Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Default.mat");
        using (CreatureRig rig = RenderTestAssets.CreateRig(recipe, _parent.transform, material))
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
                    UnitChannels channels = RenderTestAssets.CreateChannels(side, head, count, StemBand.Quick, MassBand.Heavy);

                    CreatureRecipe recipe = Compose(channels);

                    Assert.NotNull(recipe, $"{side} {head} {count}");
                    Assert.LessOrEqual(recipe.parts.Length, _vocabulary.maxParts, $"{side} {head} {count}");
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
                CreatureRecipe recipe = Compose(RenderTestAssets.CreateChannels(side, HeadKind.Bud, accessory: accessory));

                Assert.NotNull(recipe, $"{side} {accessory}");
            }
        }
    }

    [Test]
    public void AccessoryReach_EveryAccessoryMassStemAndSide_BreaksTheOutline()
    {
        foreach (LookSide side in Enum.GetValues(typeof(LookSide)))
        {
            float needed = side == LookSide.Plant ? LookComposer.PlantAccessoryReach : LookComposer.StoneAccessoryReach;
            foreach (AccessoryKind accessory in Enum.GetValues(typeof(AccessoryKind)))
            {
                if (accessory == AccessoryKind.None)
                {
                    continue;
                }

                foreach (MassBand mass in Enum.GetValues(typeof(MassBand)))
                {
                    foreach (StemBand stem in Enum.GetValues(typeof(StemBand)))
                    {
                        UnitChannels channels = RenderTestAssets.CreateChannels(side, HeadKind.Bud, stem: stem, mass: mass, accessory: accessory);

                        float reach = LookComposer.AccessoryReach(channels, _vocabulary);

                        Assert.GreaterOrEqual(reach, needed, $"{side} {accessory} {mass} {stem}"); // in cells past body and head
                    }
                }
            }
        }
    }

    [Test]
    public void Compose_EveryAccessory_SitsOnTheUnitsRight()
    {
        foreach (AccessoryKind accessory in Enum.GetValues(typeof(AccessoryKind)))
        {
            if (accessory == AccessoryKind.None)
            {
                continue;
            }

            CreatureRecipe recipe = Compose(RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.Bud, accessory: accessory));

            float sum = 0f;
            foreach (CreaturePart part in FindAll(recipe, PartRole.Accessory))
            {
                sum += part.localPosition.x;
            }
            Assert.Greater(sum, 0f, accessory.ToString());
        }
    }

    [Test]
    public void Compose_Plant_GrowsJointedRootsAtThePinnedReach()
    {
        CreatureRecipe recipe = Compose(RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.Bud));

        Assert.AreEqual(_vocabulary.rootCount, recipe.roots.count);
        Assert.That(recipe.roots.segments, Is.InRange(2, 3));
        float unit = _vocabulary.Unit(LookSide.Plant);
        Assert.AreEqual(_vocabulary.pinnedReach * unit, recipe.roots.footRadius, 0.0001f);
        Assert.That(recipe.roots.thickness * 2f / unit, Is.InRange(0.12f, 0.18f)); // diameter in plant body units
        Assert.Less(recipe.roots.hipHeight / unit, 0.2f); // the roots leave the body at its base
        Assert.AreEqual(2, recipe.arms.Length);
    }

    [Test]
    public void Compose_Stone_StandsOnSeededStonesWithoutRootsOrLianas()
    {
        CreatureRecipe recipe = Compose(RenderTestAssets.CreateChannels(LookSide.Stone, HeadKind.Bud));

        Assert.AreEqual(0, recipe.roots.count);
        Assert.IsEmpty(recipe.arms);
        Assert.AreEqual(Primitive.Stone, recipe.parts[0].primitive);
        Assert.AreEqual(2, FindAll(recipe, PartRole.Limb).Length);
        Assert.AreEqual(_vocabulary.palette.stoneWilt, recipe.wiltColour);
        HashSet<int> variants = new HashSet<int>();
        foreach (CreaturePart part in recipe.parts)
        {
            Assert.AreNotEqual(Primitive.Boulder, part.primitive, part.id);
            if (part.primitive == Primitive.Stone)
            {
                variants.Add(part.variant);
            }
        }
        Assert.Greater(variants.Count, 1);
    }

    [Test]
    public void Compose_SturdyStone_BodyIsTwoPointTwoBodyUnitsAcross()
    {
        CreatureRecipe recipe = Compose(RenderTestAssets.CreateChannels(LookSide.Stone, HeadKind.Bud, mass: MassBand.Sturdy));

        float width = recipe.parts[0].dimensions.x * 2f / _vocabulary.bodyUnit; // the stone mesh spans two units across

        Assert.AreEqual(2.2f, width, 0.001f);
    }

    [TestCase(MassBand.Light, 0.35f)]
    [TestCase(MassBand.Sturdy, 0.5f)]
    public void Compose_PlantMass_BodyRadiusInCellsIsNearTheTarget(MassBand mass, float target)
    {
        CreatureRecipe recipe = Compose(RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.Bud, mass: mass));

        float radius = recipe.parts[0].dimensions.x * 0.5f; // the sphere mesh is one unit across

        Assert.AreEqual(PartRole.Body, recipe.parts[0].role);
        Assert.AreEqual(target, radius, target * 0.1f); // a Sturdy body about one cell across, Light about 0.7
    }

    [Test]
    public void HeadSpan_EveryPlantHeadAtLightMass_CoversAThirdOfACell()
    {
        foreach (HeadKind head in Enum.GetValues(typeof(HeadKind)))
        {
            UnitChannels channels = RenderTestAssets.CreateChannels(LookSide.Plant, head, mass: MassBand.Light);

            float span = LookComposer.HeadSpan(channels, _vocabulary);

            Assert.GreaterOrEqual(span, 0.35f, head.ToString()); // the smaller side of its screen box, in cells
        }
    }

    [TestCase(CountBand.Few)]
    [TestCase(CountBand.Many)]
    public void HeadGap_EveryFannedPlantHead_KeepsNeighboursApart(CountBand count)
    {
        foreach (HeadKind head in Enum.GetValues(typeof(HeadKind)))
        {
            if (_vocabulary.heads[head].carriesCount)
            {
                continue;
            }

            UnitChannels channels = RenderTestAssets.CreateChannels(LookSide.Plant, head, count, mass: MassBand.Light);

            float gap = LookComposer.HeadGap(channels, _vocabulary);

            Assert.GreaterOrEqual(gap, 0f, $"{head} {count}"); // in cells between two neighbouring copies
        }
    }

    [Test]
    public void Compose_HeavierMass_GrowsTheHeadWithItsSocket()
    {
        CreaturePart light = FindAll(Compose(RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.Bud, mass: MassBand.Light)), PartRole.Tip)[0];
        CreaturePart heavy = FindAll(Compose(RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.Bud, mass: MassBand.Heavy)), PartRole.Tip)[0];

        float massRatio = _vocabulary.bodies[MassBand.Heavy].scale / _vocabulary.bodies[MassBand.Light].scale;
        Assert.AreEqual(massRatio, heavy.dimensions.x / light.dimensions.x, 0.001f);
    }

    [TestCase(CountBand.One, 1)]
    [TestCase(CountBand.Few, 3)]
    [TestCase(CountBand.Many, 5)]
    public void Compose_Count_DrawsOneThreeOrFiveTips(CountBand count, int tips)
    {
        CreatureRecipe recipe = Compose(RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.Bud, count));

        int found = FindAll(recipe, PartRole.Tip).Length;

        Assert.AreEqual(tips, found);
    }

    [TestCase(EffectFamily.Damage)]
    [TestCase(EffectFamily.Heal)]
    [TestCase(EffectFamily.Boon)]
    public void Compose_Accent_SitsOnTheTipsAndNeverOnTheBody(EffectFamily family)
    {
        UnitChannels channels = RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.Spear, CountBand.Few);
        channels.accent = family;
        Color accent = _vocabulary.palette.Accent(family);

        CreatureRecipe recipe = Compose(channels);

        foreach (CreaturePart part in recipe.parts)
        {
            bool isTip = part.role == PartRole.Tip;
            Assert.AreEqual(isTip, part.colour == accent, part.id);
            Assert.AreEqual(isTip, part.glow > 0f, part.id);
        }
    }

    [Test]
    public void Compose_Plant_GivesTheLianaTipTheAccent()
    {
        UnitChannels channels = RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.Bud);

        CreatureRecipe recipe = Compose(channels);

        Assert.AreEqual(_vocabulary.palette.Accent(channels.accent), recipe.arms[0].tipColour);
    }

    [Test]
    public void Compose_HeavyMass_AddsABasePartRatherThanOnlyScaling()
    {
        int light = Compose(RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.Bud, mass: MassBand.Light)).parts.Length;
        int heavy = Compose(RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.Bud, mass: MassBand.Heavy)).parts.Length;

        Assert.AreEqual(light + 1, heavy);
    }

    [Test]
    public void Compose_NoVocabulary_LogsAndReturnsNull()
    {
        UnityEngine.TestTools.LogAssert.Expect(LogType.Error, "[LookComposer] Needs a vocabulary with a palette.");

        CreatureRecipe recipe = LookComposer.Compose(RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.Bud), null);

        Assert.IsNull(recipe);
    }
}

}
