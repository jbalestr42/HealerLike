using System.Collections.Generic;
using System;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
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
                    UnitChannels channels = RenderTestAssets.CreateChannels(
                        side,
                        head,
                        count,
                        StemBand.Quick,
                        MassBand.Heavy
                    );
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
                CreatureRecipe recipe = Compose(
                    RenderTestAssets.CreateChannels(side, HeadKind.Bud, accessory: accessory)
                );
                Assert.NotNull(recipe, $"{side} {accessory}");
            }
        }
    }

    [Test]
    public void Compose_Accessories_UseTheirAuthoredCenteredOrRightPlacement()
    {
        foreach (AccessoryKind accessory in Enum.GetValues(typeof(AccessoryKind)))
        {
            if (accessory == AccessoryKind.None)
            {
                continue;
            }

            CreatureRecipe recipe = Compose(
                RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.Bud, accessory: accessory)
            );
            float sum = 0f;
            foreach (CreaturePart part in FindAll(recipe, PartRole.Accessory))
            {
                sum += part.localPosition.x;
            }

            if (_vocabulary.accessories[accessory].isCentered)
            {
                Assert.AreEqual(0f, sum, 0.001f, accessory.ToString());
            }
            else
            {
                Assert.Greater(sum, 0f, accessory.ToString());
            }
        }
    }

    [Test]
    public void Compose_Plant_GrowsJointedRootsAtTheDerivedReach()
    {
        CreatureRecipe recipe = Compose(RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.Bud));
        Assert.AreEqual(_vocabulary.rootCount, recipe.roots.count);
        Assert.That(recipe.roots.segments, Is.InRange(2, 3));
        float unit = _vocabulary.Unit(LookSide.Plant);
        Assert.AreEqual(_vocabulary.Reach(ReachBand.Long) * unit, recipe.roots.footRadius, 0.0001f);
        Assert.GreaterOrEqual(recipe.roots.thickness * 2f / unit, 0.12f);
        Assert.Less(recipe.roots.thickness * 2f, recipe.parts[0].dimensions.x * 0.35f);
        Assert.Greater(
            recipe.roots.hipHeight,
            recipe.parts[0].localPosition.y - recipe.parts[0].dimensions.y * 0.5f
        );
        // The roots emerge inside the base.
        Assert.Less(recipe.roots.hipHeight, recipe.parts[0].localPosition.y + recipe.parts[0].dimensions.y * 0.5f);
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
        CreatureRecipe recipe = Compose(
            RenderTestAssets.CreateChannels(LookSide.Stone, HeadKind.Bud, mass: MassBand.Sturdy)
        );
        float meshWidth = recipe.parts[0].shape.isProcedural ? 1f : 2f;
        float width = recipe.parts[0].dimensions.x * meshWidth / _vocabulary.bodyUnit;
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
    public void Compose_MassUsesItsAuthoredHeadScale()
    {
        CreaturePart light = FindAll(
            Compose(RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.Bud, mass: MassBand.Light)),
            PartRole.Tip
        )[0];
        CreaturePart heavy = FindAll(
            Compose(RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.Bud, mass: MassBand.Heavy)),
            PartRole.Tip
        )[0];
        float massRatio =
            _vocabulary.bodies[MassBand.Heavy].effectiveHeadScale
            / _vocabulary.bodies[MassBand.Light].effectiveHeadScale;
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
        int light = Compose(
            RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.Bud, mass: MassBand.Light)
        ).parts.Length;
        int heavy = Compose(
            RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.Bud, mass: MassBand.Heavy)
        ).parts.Length;
        Assert.AreEqual(light + 1, heavy);
    }

    [Test]
    public void Compose_NoVocabulary_LogsAndReturnsNull()
    {
        UnityEngine.TestTools.LogAssert.Expect(LogType.Error, "[LookComposer] Needs a vocabulary with a palette.");
        CreatureRecipe recipe = LookComposer.Compose(
            RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.Bud),
            null
        );
        Assert.IsNull(recipe);
    }
}
}
