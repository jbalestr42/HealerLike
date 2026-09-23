using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Grammar;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Creatures
{

public class LookVocabularyBakerTests
{
    static readonly float tolerance = 0.0001f;

    readonly List<Object> _objects = new List<Object>();
    LookVocabulary _vocabulary;

    static UnitChannels CreateChannels(LookSide side, HeadKind head, CountBand count = CountBand.One, StemBand stem = StemBand.Steady,
        MassBand mass = MassBand.Light, AccessoryKind accessory = AccessoryKind.None, HeadKind accessoryHead = HeadKind.Bud)
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
            accessoryHead = accessoryHead,
            accent = EffectFamily.Rot
        };
    }

    [SetUp]
    public void SetUp()
    {
        _vocabulary = AssetDatabase.LoadAssetAtPath<LookVocabulary>(LookVocabularyBaker.AssetPath);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object trackedObject in _objects)
        {
            Object.DestroyImmediate(trackedObject);
        }
        _objects.Clear();
    }

    void AssertSameRecipe(UnitChannels channels)
    {
        string label = $"{channels.side} {channels.head} {channels.count} {channels.stem} {channels.mass} {channels.accessory} {channels.accessoryHead}";
        CreatureRecipe code = LookComposer.Compose(channels);
        CreatureRecipe asset = LookComposer.Compose(channels, _vocabulary);
        _objects.Add(code);
        _objects.Add(asset);

        Assert.NotNull(code, label);
        Assert.NotNull(asset, label);
        Assert.AreEqual(code.parts.Length, asset.parts.Length, label);
        for (int i = 0; i < code.parts.Length; i++)
        {
            CreaturePart expected = code.parts[i];
            CreaturePart actual = asset.parts[i];
            string part = label + " part " + i + " " + expected.id;
            Assert.AreEqual(expected.id, actual.id, part);
            Assert.AreEqual(expected.parent, actual.parent, part);
            Assert.AreEqual(expected.primitive, actual.primitive, part);
            AssertNear(expected.localPosition, actual.localPosition, part + " position");
            AssertNear(expected.dimensions, actual.dimensions, part + " dimensions");
            Assert.Less(Quaternion.Angle(Quaternion.Euler(expected.localEuler), Quaternion.Euler(actual.localEuler)), 0.01f, part + " euler");
            AssertNear((Vector4)expected.colour, (Vector4)actual.colour, part + " colour");
            Assert.AreEqual(expected.glow, actual.glow, tolerance, part + " glow");
        }

        AssertNear(code.targetLocal, asset.targetLocal, label + " target");
        Assert.AreEqual(code.sourceLocal.Length, asset.sourceLocal.Length, label);
        for (int i = 0; i < code.sourceLocal.Length; i++)
        {
            AssertNear(code.sourceLocal[i], asset.sourceLocal[i], label + " source " + i);
        }

        Assert.AreEqual(code.arms.Length, asset.arms.Length, label);
        for (int i = 0; i < code.arms.Length; i++)
        {
            AssertNear(code.arms[i].rootLocal, asset.arms[i].rootLocal, label + " arm " + i);
            AssertNear((Vector4)code.arms[i].colour, (Vector4)asset.arms[i].colour, label + " arm colour " + i);
        }

        Assert.AreEqual(code.roots.count, asset.roots.count, label);
        Assert.AreEqual(code.roots.segments, asset.roots.segments, label);
        Assert.AreEqual(code.roots.footRadius, asset.roots.footRadius, tolerance, label);
        Assert.AreEqual(code.roots.thickness, asset.roots.thickness, tolerance, label);
        Assert.AreEqual(code.idle.seed, asset.idle.seed, label);
        AssertNear((Vector4)code.wiltColour, (Vector4)asset.wiltColour, label + " wilt");
    }

    static void AssertNear(Vector4 expected, Vector4 actual, string label)
    {
        Assert.Less((expected - actual).magnitude, tolerance, $"{label}: {expected} against {actual}");
    }

    [TestCase("NormalEntity")]
    [TestCase("FastShootEntity")]
    [TestCase("TripleShootEntity")]
    [TestCase("MultiShotEntity")]
    [TestCase("RandomShootEntity")]
    [TestCase("ChainLightningEntity")]
    [TestCase("ChannelingEntity")]
    [TestCase("SwarmEntity")]
    [TestCase("TestEntity")]
    [TestCase("SoldierEntity")]
    [TestCase("HitArmorBufferEntityEntity")]
    public void Compose_LiveEntityOnBothSides_AssetMatchesCode(string folder)
    {
        EntityData data = LookDerivationTests.LoadEntity(folder);

        AssertSameRecipe(LookDerivation.Channels(data, Entity.EntityType.Player));
        AssertSameRecipe(LookDerivation.Channels(data, Entity.EntityType.Computer));
    }

    [Test]
    public void Compose_EveryHeadCountAndSide_AssetMatchesCode()
    {
        foreach (LookSide side in Enum.GetValues(typeof(LookSide)))
        {
            foreach (HeadKind head in Enum.GetValues(typeof(HeadKind)))
            {
                foreach (CountBand count in Enum.GetValues(typeof(CountBand)))
                {
                    AssertSameRecipe(CreateChannels(side, head, count));
                }
            }
        }
    }

    [Test]
    public void Compose_EveryAccessoryMassAndSide_AssetMatchesCode()
    {
        foreach (LookSide side in Enum.GetValues(typeof(LookSide)))
        {
            foreach (AccessoryKind accessory in Enum.GetValues(typeof(AccessoryKind)))
            {
                foreach (MassBand mass in Enum.GetValues(typeof(MassBand)))
                {
                    AssertSameRecipe(CreateChannels(side, HeadKind.Bud, mass: mass, accessory: accessory));
                }
            }
        }
    }

    [Test]
    public void Compose_EveryMiniHeadAndSide_AssetMatchesCode()
    {
        foreach (LookSide side in Enum.GetValues(typeof(LookSide)))
        {
            foreach (HeadKind head in Enum.GetValues(typeof(HeadKind)))
            {
                AssertSameRecipe(CreateChannels(side, HeadKind.Spear, CountBand.Few, accessory: AccessoryKind.MiniHead, accessoryHead: head));
            }
        }
    }

    [Test]
    public void Compose_EveryStemAndMass_AssetMatchesCode()
    {
        foreach (LookSide side in Enum.GetValues(typeof(LookSide)))
        {
            foreach (StemBand stem in Enum.GetValues(typeof(StemBand)))
            {
                foreach (MassBand mass in Enum.GetValues(typeof(MassBand)))
                {
                    AssertSameRecipe(CreateChannels(side, HeadKind.Arch, CountBand.Many, stem, mass));
                }
            }
        }
    }
}

}
