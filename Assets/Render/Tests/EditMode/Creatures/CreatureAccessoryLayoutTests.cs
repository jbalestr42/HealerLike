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

public class CreatureAccessoryLayoutTests : CreatureCompositionFixture
{
    [TestCase(LookSide.Plant)]
    [TestCase(LookSide.Stone)]
    public void Layout_ManyMiniHead_ExtendsAnAttachedSupportBeyondTheCrown(LookSide side)
    {
        AddMiniHead();
        UnitChannels channels = RenderTestAssets.CreateChannels(
            side,
            HeadKind.Bud,
            CountBand.Many,
            accessory: AccessoryKind.MiniHead
        );
        channels.accessoryHead = HeadKind.Bud;
        PartList parts = LookComposer.Layout(channels, _vocabulary);
        LookPart connector = parts.Source(parts.count - 1);
        LookPart shiftedFirst = parts.Source(parts.accessoryStart);
        Vector3 socket = UnitSockets.Place(channels, _vocabulary).neck;
        float shift = shiftedFirst.position.x - socket.x - 0.45f;
        Assert.Greater(shift, 0f);
        Assert.AreEqual("AccessorySupport", connector.id);
        Assert.AreEqual(socket + Vector3.right * shift * 0.5f, connector.position);
        Assert.GreaterOrEqual(connector.size.y, shift);
        Assert.AreEqual(side == LookSide.Plant ? ShapeKind.Segment : ShapeKind.Block, connector.shape.kind);
        Assert.GreaterOrEqual(
            LookMeasure.OutlineReach(parts, _vocabulary.Unit(side)),
            LookComposer.AccessoryClearance(side, _vocabulary)
        );
        Assert.AreEqual(5, parts.headStarts.Count);
    }

    [TestCase(LookSide.Plant)]
    [TestCase(LookSide.Stone)]
    public void Layout_SimpleMiniHead_KeepsItsAuthoredPlacement(LookSide side)
    {
        AddMiniHead();
        // Its authored miniature already clears either side's outline.
        _vocabulary.accessories[AccessoryKind.MiniHead].miniHeadAt = Vector3.right * 1.2f;
        UnitChannels channels = RenderTestAssets.CreateChannels(
            side,
            HeadKind.Bud,
            accessory: AccessoryKind.MiniHead
        );
        channels.accessoryHead = HeadKind.Bud;
        PartList parts = LookComposer.Layout(channels, _vocabulary);
        Vector3 socket = UnitSockets.Place(channels, _vocabulary).neck;
        Assert.AreEqual(socket + Vector3.right * 0.45f, parts.Source(parts.accessoryStart).position);
        Assert.IsFalse(Array.Exists(parts.ToArray(), p => p.id == "AccessorySupport"));
    }

    [TestCase(LookSide.Plant)]
    [TestCase(LookSide.Stone)]
    public void Compose_CenteredRing_RemainsOnItsSocketWithoutARightSupport(LookSide side)
    {
        _vocabulary.layoutSettings.extendAccessorySupports = true;
        LookPart ring = Part("Collar", PartRole.Accessory, ShapeProfile.Ring());
        ring.primitive = Primitive.Torus;
        ring.size = new Vector3(0.9f, 0.13f, 0.9f);
        _vocabulary.accessories[AccessoryKind.SmallTorus] = new LookVocabulary.AccessoryEntry
        {
            socket = AccessorySocket.NeckOrbit,
            isCentered = true,
            plant = new[] { ring },
            stone = new[] { ring },
        };
        CreatureGrammarPreset preset = Track(ScriptableObject.CreateInstance<CreatureGrammarPreset>());
        preset.vocabulary = _vocabulary;
        preset.side = side;
        preset.accessory = AccessoryKind.SmallTorus;
        UnitChannels channels = preset.Channels();
        PartList parts = LookComposer.Layout(channels, _vocabulary);
        Assert.AreEqual(UnitSockets.Place(channels, _vocabulary).neck, parts.Source(parts.accessoryStart).position);
        Assert.IsFalse(Array.Exists(parts.ToArray(), p => p.id == "AccessorySupport"));
        Assert.IsEmpty(CreatureGrammarValidator.Validate(preset));
        Assert.NotNull(Track(preset.Compose()));
        // The collar has an open ring profile with positive visible dimensions, not a hidden marker.
        Assert.AreEqual(ShapeKind.Ring, parts.Source(parts.accessoryStart).shape.kind);
        Assert.Greater(
            parts.Source(parts.accessoryStart).size.x,
            _vocabulary.stems[StemBand.Steady].thickness * 2f
        );
    }
}
}
