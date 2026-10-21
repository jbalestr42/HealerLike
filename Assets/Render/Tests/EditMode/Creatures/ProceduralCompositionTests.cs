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

public class ProceduralCompositionTests : CreatureCompositionFixture
{
    [Test]
    public void Add_DefaultProfile_PreservesTheBakedStonePivotAndSpan()
    {
        Vector3 centre = new Vector3(1f, 2f, 3f);
        Vector3 size = new Vector3(2f, 3f, 4f);
        Vector3 euler = new Vector3(20f, 30f, 40f);
        PrimitiveMeshes.Fit(
            Primitive.Stone,
            centre,
            size,
            Quaternion.Euler(euler),
            out Vector3 dimensions,
            out Vector3 pivot
        );
        PartList parts = new PartList(1f);
        parts.Add("Legacy", Primitive.Stone, centre, size, Color.white, euler, 0f, PartRole.Body);
        Assert.AreEqual(dimensions, parts.ToArray()[0].dimensions);
        Assert.AreEqual(pivot, parts.ToArray()[0].localPosition);
        Assert.IsFalse(parts.ToArray()[0].shape.isProcedural);
    }

    [Test]
    public void Add_ProceduralBlock_UsesItsAuthoredUnitBoxAndKeepsTheProfile()
    {
        ShapeProfile profile = ShapeProfile.Block(0.3f, 0.2f, 0.1f);
        Vector3 centre = new Vector3(1f, 2f, 3f);
        Vector3 size = new Vector3(2f, 3f, 4f);
        PartList parts = new PartList(0.5f);
        parts.Add(
            "Block",
            Primitive.Stone,
            centre,
            size,
            Color.white,
            Vector3.zero,
            0f,
            PartRole.Body,
            shape: profile
        );
        CreaturePart part = parts.ToArray()[0];
        Assert.AreEqual(size * 0.5f, part.dimensions);
        Assert.AreEqual(centre * 0.5f, part.localPosition);
        Assert.AreEqual(profile, part.shape);
        Assert.AreEqual(profile, parts.Source(0).shape);
    }

    [TestCase(LookSide.Plant, CountBand.One, 1)]
    [TestCase(LookSide.Plant, CountBand.Few, 3)]
    [TestCase(LookSide.Plant, CountBand.Many, 5)]
    [TestCase(LookSide.Stone, CountBand.One, 1)]
    [TestCase(LookSide.Stone, CountBand.Few, 3)]
    [TestCase(LookSide.Stone, CountBand.Many, 5)]
    public void Compose_ProceduralBands_PreserveCountAndSideAnatomy(LookSide side, CountBand count, int tips)
    {
        CreatureRecipe recipe = Compose(side, count);
        Assert.NotNull(recipe);
        Assert.AreEqual(tips, Array.FindAll(recipe.parts, p => p.role == PartRole.Tip).Length);
        Assert.AreEqual(
            side == LookSide.Plant ? 0 : 2,
            Array.FindAll(recipe.parts, p => p.role == PartRole.Limb).Length
        );
        Assert.AreEqual(side == LookSide.Plant ? _vocabulary.rootCount : 0, recipe.roots.count);
        Assert.That(Array.TrueForAll(recipe.parts, p => p.shape.isProcedural));
    }

    [Test]
    public void Roots_LegacyEntryMissingNewFields_KeepsItsOriginalThickness()
    {
        _vocabulary.roots[ReachBand.Short] = new LookVocabulary.RootEntry
        {
            reach = 0.8f,
            thicknessScale = 0f,
            taper = 0f,
            jointScale = 0f,
        };
        RootDefinition roots = LookComposer.Roots(ReachBand.Short, _vocabulary);
        Assert.AreEqual(_vocabulary.rootThickness * 0.5f * _vocabulary.Unit(LookSide.Plant), roots.thickness);
        Assert.IsFalse(roots.segmentShape.isProcedural);
        Assert.IsFalse(roots.jointShape.isProcedural);
    }

    [TestCase(ReachBand.Short)]
    [TestCase(ReachBand.Mid)]
    [TestCase(ReachBand.Long)]
    public void Compose_ReachBand_TransmitsItsRootProfilesAndProportions(ReachBand reach)
    {
        _vocabulary.roots[reach].thicknessScale = 1.4f;
        LookVocabulary.RootEntry source = _vocabulary.roots[reach];
        RootDefinition actual = Compose(LookSide.Plant, reach: reach).roots;
        Assert.AreEqual(source.segmentShape, actual.segmentShape);
        Assert.AreEqual(source.jointShape, actual.jointShape);
        Assert.AreEqual(source.taper, actual.taper);
        Assert.AreEqual(source.jointScale, actual.jointScale);
        Assert.AreEqual(source.reach * _vocabulary.Unit(LookSide.Plant), actual.footRadius, 0.00001f);
        Assert.AreEqual(
            _vocabulary.rootThickness * 0.5f * 1.4f * _vocabulary.Unit(LookSide.Plant),
            actual.thickness,
            0.00001f
        );
    }

    [Test]
    public void Compose_AttachedBlade_UsesItsAnchorsAfterProfileEdits()
    {
        _vocabulary.heads[HeadKind.Bud].plant = new[]
        {
            new LookPart
            {
                id = "Blade",
                primitive = Primitive.Leaf,
                shape = ShapeProfile.Leaf(0.75f, 1.3f),
                pivot = ShapeAnchor.Bottom,
                position = new Vector3(0.2f, 0f, 0f),
                size = new Vector3(0.5f, 1.2f, 0.25f),
                role = PartRole.Head,
                colour = ColourRole.Body,
            },
            new LookPart
            {
                id = "Accent",
                primitive = Primitive.Sphere,
                shape = ShapeProfile.Bulb(),
                pivot = ShapeAnchor.Bottom,
                attachTo = "Blade",
                attachAt = ShapeAnchor.Top,
                size = Vector3.one * 0.15f,
                role = PartRole.Tip,
                colour = ColourRole.Accent,
            },
        };
        CreatureRecipe recipe = Compose(LookSide.Plant);
        CreaturePart blade = Array.Find(recipe.parts, p => p.id == "Blade");
        CreaturePart accent = Array.Find(recipe.parts, p => p.id == "Accent");
        Vector3 basePoint =
            recipe.parts[0].localPosition
            + blade.localPosition
            + Vector3.Scale(blade.dimensions, ProceduralShapeMeshes.Anchor(blade.shape, ShapeAnchor.Bottom));
        Vector3 expectedBase = recipe.neckLocal + Vector3.right * (0.2f * _vocabulary.Unit(LookSide.Plant));
        Vector3 topPoint =
            blade.localPosition
            + Vector3.Scale(blade.dimensions, ProceduralShapeMeshes.Anchor(blade.shape, ShapeAnchor.Top));
        Vector3 accentBase =
            accent.localPosition
            + Vector3.Scale(accent.dimensions, ProceduralShapeMeshes.Anchor(accent.shape, ShapeAnchor.Bottom));
        Assert.Less(Vector3.Distance(expectedBase, basePoint), 0.00001f);
        Assert.Less(Vector3.Distance(topPoint, accentBase), 0.00001f);
    }
}
}
