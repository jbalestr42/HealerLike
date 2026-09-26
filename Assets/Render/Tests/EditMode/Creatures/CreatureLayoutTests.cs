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

public class CreatureLayoutTests : CreatureCompositionFixture
{
    [Test]
    public void Compose_ZeroFamilyStemScale_MatchesAnExplicitLegacyLength()
    {
        CreatureRecipe original = Compose(LookSide.Plant);
        Assert.AreEqual(0f, _vocabulary.heads[HeadKind.Bud].plantStemScale);
        _vocabulary.heads[HeadKind.Bud].plantStemScale = 1f;
        CreatureRecipe explicitLegacy = Compose(LookSide.Plant);
        CollectionAssert.AreEqual(original.parts, explicitLegacy.parts);
        Assert.AreEqual(original.neckLocal, explicitLegacy.neckLocal);
    }

    [TestCase(0.4f)]
    [TestCase(1.8f)]
    public void Compose_FamilyStemScale_KeepsTheStalkLinkedAndTheCrownSizeUnchanged(float multiplier)
    {
        UnitChannels channels = RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.Bud);
        CreatureRecipe before = Track(LookComposer.Compose(channels, _vocabulary));
        _vocabulary.heads[HeadKind.Bud].plantStemScale = multiplier;
        CreatureRecipe after = Track(LookComposer.Compose(channels, _vocabulary));
        PartList parts = LookComposer.Layout(channels, _vocabulary);
        UnitSockets sockets = UnitSockets.Place(channels, _vocabulary);
        LookPart stalk = parts.Source(1);
        float length = _vocabulary.stems[channels.stem].length * multiplier;
        Assert.AreEqual(length, Vector3.Distance(sockets.stemFoot, sockets.neck), 0.00001f);
        Assert.Less(Vector3.Distance((sockets.stemFoot + sockets.neck) * 0.5f, stalk.position), 0.00001f);
        Assert.AreEqual(length + _vocabulary.stems[channels.stem].thickness, stalk.size.y, 0.00001f);
        Quaternion rotation = Quaternion.Euler(stalk.euler);
        Vector3 bottom =
            stalk.position
            + rotation * Vector3.Scale(stalk.size, ProceduralShapeMeshes.Anchor(stalk.shape, ShapeAnchor.Bottom));
        Vector3 top =
            stalk.position
            + rotation * Vector3.Scale(stalk.size, ProceduralShapeMeshes.Anchor(stalk.shape, ShapeAnchor.Top));
        Vector3 overlap = Vector3.up * (_vocabulary.stems[channels.stem].thickness * 0.5f);
        Assert.Less(Vector3.Distance(bottom, sockets.stemFoot - overlap), 0.00001f);
        Assert.Less(Vector3.Distance(top, sockets.neck + overlap), 0.00001f);
        Assert.AreEqual(sockets.neck, parts.Source(parts.headStarts[0]).position);
        Assert.AreEqual(before.parts[0].dimensions, after.parts[0].dimensions);
        Assert.AreEqual(
            Array.Find(before.parts, p => p.role == PartRole.Tip).dimensions,
            Array.Find(after.parts, p => p.role == PartRole.Tip).dimensions
        );
        Assert.AreNotEqual(before.neckLocal, after.neckLocal);
    }

    [Test]
    public void Compose_FamilyStemScale_LeavesStoneLegsAndCrownUnchanged()
    {
        CreatureRecipe before = Compose(LookSide.Stone);
        _vocabulary.heads[HeadKind.Bud].plantStemScale = 0.25f;
        CreatureRecipe after = Compose(LookSide.Stone);
        CollectionAssert.AreEqual(before.parts, after.parts);
        Assert.AreEqual(before.neckLocal, after.neckLocal);
    }

    [TestCase(0f)]
    [TestCase(0.7f)]
    public void Layout_ArticulatedStem_KeepsEveryProfileEndpointOnItsGrowthPath(float bend)
    {
        UnitChannels channels = RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.Bud);
        CreatureRecipe stoneBefore = Compose(LookSide.Stone);
        UnitSockets sockets = UnitSockets.Place(channels, _vocabulary);
        LookVocabulary.PlantStemEntry growth = new LookVocabulary.PlantStemEntry
        {
            segments = 3,
            thicknessScale = 2.4f,
            bow = 0.17f,
            jointScale = 0.8f,
            segmentShape = ShapeProfile.Segment(0.2f, 0.75f, bend),
            jointShape = ShapeProfile.Bulb(),
        };
        _vocabulary.heads[HeadKind.Bud].plantStem = growth;
        PartList parts = LookComposer.Layout(channels, _vocabulary);
        Assert.AreEqual(1 + growth.segments * 2 - 1, parts.headStarts[0]);
        Assert.AreEqual(sockets.neck, parts.Source(parts.headStarts[0]).position);
        float width = _vocabulary.stems[channels.stem].thickness * growth.thicknessScale;
        Vector3 start = sockets.stemFoot;
        for (int i = 0; i < growth.segments; i++)
        {
            float t = (i + 1f) / growth.segments;
            Vector3 end =
                Vector3.Lerp(sockets.stemFoot, sockets.neck, t)
                + Vector3.right * (growth.bow * Mathf.Sin(Mathf.PI * t));
            LookPart link = parts.Source(1 + i * 2);
            Quaternion rotation = Quaternion.Euler(link.euler);
            Vector3 bottom =
                link.position
                + rotation * Vector3.Scale(link.size, ProceduralShapeMeshes.Anchor(link.shape, ShapeAnchor.Bottom));
            Vector3 top =
                link.position
                + rotation * Vector3.Scale(link.size, ProceduralShapeMeshes.Anchor(link.shape, ShapeAnchor.Top));
            Vector3 overlap = (end - start).normalized * (width * 0.5f);
            Assert.Less(Vector3.Distance(bottom, start - overlap), 0.00001f);
            Assert.Less(Vector3.Distance(top, end + overlap), 0.00001f);
            if (i + 1 < growth.segments)
            {
                Assert.Less(Vector3.Distance(end, parts.Source(2 + i * 2).position), 0.00001f);
            }

            start = end;
        }

        CreatureRecipe stoneAfter = Compose(LookSide.Stone);
        CollectionAssert.AreEqual(stoneBefore.parts, stoneAfter.parts);
    }

    [Test]
    public void Layout_FamilyStemScale_PreservesCadenceOrderingWithinTheFamily()
    {
        _vocabulary.heads[HeadKind.Bud].plantStemScale = 0.4f;
        _vocabulary.stems[StemBand.Quick].length = 1.2f;
        _vocabulary.stems[StemBand.Steady].length = 0.8f;
        _vocabulary.stems[StemBand.Slow].length = 0.5f;
        float[] lengths = new float[3];
        for (int i = 0; i < 3; i++)
        {
            StemBand band = new[] { StemBand.Quick, StemBand.Steady, StemBand.Slow }[i];
            UnitChannels channels = RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.Bud, stem: band);
            UnitSockets sockets = UnitSockets.Place(channels, _vocabulary);
            lengths[i] = Vector3.Distance(sockets.stemFoot, sockets.neck);
        }

        Assert.Greater(lengths[0], lengths[1]);
        Assert.Greater(lengths[1], lengths[2]);
        Assert.AreEqual(2.4f, lengths[0] / lengths[2], 0.00001f);
        Assert.AreEqual(1.6f, lengths[1] / lengths[2], 0.00001f);
    }

    [TestCase(LookSide.Plant)]
    [TestCase(LookSide.Stone)]
    public void Compose_ZeroScaleOverrides_PreserveLegacyMassScaling(LookSide side)
    {
        LookVocabulary.BodyEntry body = _vocabulary.bodies[MassBand.Light];
        body.scale = 1.6f;
        UnitChannels channels = RenderTestAssets.CreateChannels(side, HeadKind.Bud);
        UnitSockets sockets = UnitSockets.Place(channels, _vocabulary);
        CreatureRecipe recipe = Compose(side);
        CreaturePart tip = Array.Find(recipe.parts, p => p.role == PartRole.Tip);
        Assert.AreEqual(0f, body.headScale);
        Assert.AreEqual(0f, body.stemScale);
        Assert.AreEqual(1.6f, sockets.headScale, 0.00001f);
        Assert.AreEqual(side == LookSide.Plant ? 1f : 1.6f, sockets.stemScale, 0.00001f);
        Assert.AreEqual(0.7f * 1.6f * _vocabulary.Unit(side), tip.dimensions.x, 0.00001f);
        if (side == LookSide.Plant)
        {
            Assert.AreEqual(
                _vocabulary.stems[channels.stem].length,
                Vector3.Distance(sockets.stemFoot, sockets.neck),
                0.00001f
            );
        }
    }

    [TestCase(LookSide.Plant)]
    [TestCase(LookSide.Stone)]
    public void Compose_ExplicitHeadScale_ChangesBothHeadsWithoutChangingBodyOrLegWidth(LookSide side)
    {
        AddMiniHead();
        _vocabulary.layoutSettings.extendAccessorySupports = false;
        // This test isolates scale from support extension. Keep its authored branch outside even the legs.
        LookVocabulary.AccessoryEntry mini = _vocabulary.accessories[AccessoryKind.MiniHead];
        mini.miniHeadAt = Vector3.right * 1.6f;
        foreach (LookPart[] fragment in new[] { mini.plant, mini.stone })
        {
            fragment[0].position = Vector3.right * 0.8f;
            fragment[0].size = new Vector3(1.6f, 0.12f, 0.12f);
        }

        UnitChannels channels = RenderTestAssets.CreateChannels(
            side,
            HeadKind.Bud,
            accessory: AccessoryKind.MiniHead
        );
        channels.accessoryHead = HeadKind.Bud;
        Assert.GreaterOrEqual(
            LookMeasure.AccessoryReach(channels, _vocabulary),
            LookComposer.AccessoryClearance(side, _vocabulary)
        );
        CreatureRecipe before = Track(LookComposer.Compose(channels, _vocabulary));
        _vocabulary.bodies[MassBand.Light].headScale = 0.5f;
        Assert.GreaterOrEqual(
            LookMeasure.AccessoryReach(channels, _vocabulary),
            LookComposer.AccessoryClearance(side, _vocabulary)
        );
        CreatureRecipe after = Track(LookComposer.Compose(channels, _vocabulary));
        CreaturePart[] beforeTips = Array.FindAll(before.parts, p => p.role == PartRole.Tip);
        CreaturePart[] afterTips = Array.FindAll(after.parts, p => p.role == PartRole.Tip);
        Assert.AreEqual(2, afterTips.Length);
        for (int i = 0; i < afterTips.Length; i++)
        {
            Assert.AreEqual(beforeTips[i].dimensions * 0.5f, afterTips[i].dimensions);
        }

        for (int i = 0; i < before.parts.Length; i++)
        {
            if (before.parts[i].role == PartRole.Tip)
            {
                continue;
            }

            Assert.AreEqual(before.parts[i].dimensions, after.parts[i].dimensions, before.parts[i].id);
            Assert.AreEqual(before.parts[i].localPosition, after.parts[i].localPosition, before.parts[i].id);
        }

        Assert.AreEqual(before.neckLocal, after.neckLocal);
    }

    [TestCase(LookSide.Plant)]
    [TestCase(LookSide.Stone)]
    public void Compose_ExplicitStemScale_ChangesCadenceLengthWithoutChangingHeadOrBodySize(LookSide side)
    {
        _vocabulary.bodies[MassBand.Light].scale = 1.6f;
        CreatureRecipe before = Compose(side);
        _vocabulary.bodies[MassBand.Light].stemScale = 0.45f;
        CreatureRecipe after = Compose(side);
        PartRole supportRole = side == LookSide.Plant ? PartRole.Stem : PartRole.Limb;
        CreaturePart supportBefore = Array.Find(before.parts, p => p.role == supportRole);
        CreaturePart supportAfter = Array.Find(after.parts, p => p.role == supportRole);
        Assert.Less(supportAfter.dimensions.y, supportBefore.dimensions.y);
        Assert.AreEqual(supportBefore.dimensions.x, supportAfter.dimensions.x);
        Assert.AreEqual(supportBefore.dimensions.z, supportAfter.dimensions.z);
        Assert.AreEqual(before.parts[0].dimensions, after.parts[0].dimensions);
        Assert.AreEqual(
            Array.Find(before.parts, p => p.role == PartRole.Tip).dimensions,
            Array.Find(after.parts, p => p.role == PartRole.Tip).dimensions
        );
        UnitChannels channels = RenderTestAssets.CreateChannels(side, HeadKind.Bud);
        Assert.AreEqual(0.45f, UnitSockets.Place(channels, _vocabulary).stemScale, 0.00001f);
    }

    [Test]
    public void Compose_StemBandProfileEdit_ChangesOnlyItsSelectedBand()
    {
        ShapeProfile quick = ShapeProfile.Segment(0.7f, 0.4f, 0.2f);
        _vocabulary.stems[StemBand.Quick].plantShape = quick;
        CreatureRecipe selected = Compose(LookSide.Plant, stem: StemBand.Quick);
        CreatureRecipe other = Compose(LookSide.Plant, stem: StemBand.Steady);
        Assert.AreEqual(quick, Array.Find(selected.parts, p => p.id == "Stem").shape);
        Assert.AreEqual(ShapeProfile.Segment(), Array.Find(other.parts, p => p.id == "Stem").shape);
    }
}
}
