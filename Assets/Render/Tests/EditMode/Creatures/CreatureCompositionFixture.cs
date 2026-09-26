using System.Collections.Generic;
using System;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine;
using HealerLike.Render.Grammar;
using HealerLike.Render.Studio;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Creatures
{

public abstract class CreatureCompositionFixture
{
    protected readonly List<Object> _objects = new List<Object>();
    protected LookVocabulary _vocabulary;

    [SetUp]
    public void SetUp()
    {
        _vocabulary = Track(ScriptableObject.CreateInstance<LookVocabulary>());
        _vocabulary.palette = RenderTestAssets.LoadLookVocabulary().palette;
        _vocabulary.bodyUnit = 0.5f;
        _vocabulary.plantScale = 1f;
        _vocabulary.stoneScale = 1f;
        _vocabulary.maxParts = CreatureValidator.MaxParts;
        foreach (MassBand band in Enum.GetValues(typeof(MassBand)))
        {
            _vocabulary.bodies[band] = new LookVocabulary.BodyEntry
            {
                plant = new[] { Part("Body", PartRole.Body, ShapeProfile.Bulb()) },
                stone = new[] { Part("Body", PartRole.Body, ShapeProfile.Block()) },
            };
        }

        foreach (StemBand band in Enum.GetValues(typeof(StemBand)))
        {
            _vocabulary.stems[band] = new LookVocabulary.StemEntry
            {
                length = 1f,
                thickness = 0.12f,
                limbLength = 0.3f,
                plantShape = ShapeProfile.Segment(),
                stoneLimbShape = ShapeProfile.Block(),
            };
        }

        foreach (ReachBand band in Enum.GetValues(typeof(ReachBand)))
        {
            _vocabulary.roots[band] = new LookVocabulary.RootEntry
            {
                reach = 0.8f + 0.3f * (int)band,
                segmentShape = ShapeProfile.Segment(0.2f + 0.1f * (int)band),
                jointShape = ShapeProfile.Bulb(),
                taper = 0.5f,
                jointScale = 2.3f,
            };
        }

        _vocabulary.heads[HeadKind.Bud] = new LookVocabulary.HeadEntry
        {
            plant = new[] { Part("Tip", PartRole.Tip, ShapeProfile.Bulb()) },
            stone = new[] { Part("Tip", PartRole.Tip, ShapeProfile.Block()) },
        };
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object value in _objects)
        {
            if (value != null)
            {
                Object.DestroyImmediate(value);
            }
        }

        _objects.Clear();
    }

    protected T Track<T>(T value)
        where T : Object
    {
        _objects.Add(value);
        return value;
    }

    protected static LookPart Part(string id, PartRole role, ShapeProfile shape)
    {
        return new LookPart
        {
            id = id,
            role = role,
            shape = shape,
            primitive = shape.kind == ShapeKind.Block ? Primitive.Stone : Primitive.Sphere,
            colour = role == PartRole.Tip ? ColourRole.Accent : ColourRole.Body,
            size = Vector3.one * 0.7f,
        };
    }

    protected CreatureRecipe Compose(
        LookSide side,
        CountBand count = CountBand.One,
        StemBand stem = StemBand.Steady,
        ReachBand reach = ReachBand.Short
    )
    {
        UnitChannels channels = RenderTestAssets.CreateChannels(side, HeadKind.Bud, count, stem);
        channels.reach = reach;
        return Track(LookComposer.Compose(channels, _vocabulary));
    }

    protected void AddMiniHead()
    {
        _vocabulary.layoutSettings.extendAccessorySupports = true;
        _vocabulary.accessories[AccessoryKind.MiniHead] = new LookVocabulary.AccessoryEntry
        {
            socket = AccessorySocket.NeckOrbit,
            miniHeadAt = Vector3.right * 0.9f,
            miniHeadScale = 0.3f,
            plant = new[]
            {
                new LookPart
                {
                    id = "MiniBranch",
                    primitive = Primitive.Capsule,
                    shape = ShapeProfile.Segment(),
                    role = PartRole.Accessory,
                    colour = ColourRole.Stem,
                    position = Vector3.right * 0.45f,
                    size = new Vector3(0.9f, 0.12f, 0.12f),
                },
            },
            stone = new[]
            {
                new LookPart
                {
                    id = "MiniSlab",
                    primitive = Primitive.Stone,
                    shape = ShapeProfile.Block(),
                    role = PartRole.Accessory,
                    colour = ColourRole.Stem,
                    position = Vector3.right * 0.45f,
                    size = new Vector3(0.9f, 0.12f, 0.12f),
                },
            },
        };
    }
}
}
