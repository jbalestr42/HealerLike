using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{

public class CreatureCompositionValidatorTests : CreatureCompositionFixture
{
    [TestCase(-1)]
    [TestCase(int.MaxValue)]
    public void Compose_UnsupportedArmCount_RejectsBeforeAllocation(int count)
    {
        _vocabulary.armCount = count;
        LogAssert.Expect(
            LogType.Error,
            "[LookComposer] Plant roots or arm count are outside the renderer's supported ranges."
        );
        Assert.IsNull(Compose(LookSide.Plant));
    }

    [Test]
    public void Check_MissingTables_CollectsTheIndependentErrorsWithoutThrowing()
    {
        _vocabulary.bodies = null;
        _vocabulary.heads = null;
        _vocabulary.stems = null;
        _vocabulary.roots = null;
        List<string> errors = new List<string>();
        CreatureCompositionValidator.Check(
            RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.Bud),
            _vocabulary,
            errors
        );
        StringAssert.Contains("mass/body entry", string.Join(" ", errors));
        StringAssert.Contains("root table", string.Join(" ", errors));
    }

    [Test]
    public void Check_SelectedNullEntry_RejectsWithoutDereferencingIt()
    {
        _vocabulary.heads[HeadKind.Bud] = null;
        Assert.IsFalse(
            CreatureCompositionValidator.TryValidate(
                RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.Bud),
                _vocabulary,
                out string error
            )
        );
        StringAssert.Contains("head entry", error);
    }

    [Test]
    public void Compose_StoneWithInvalidUnusedPlantSettings_UsesOnlyItsSelectedSide()
    {
        _vocabulary.plantScale = float.NaN;
        _vocabulary.roots = null;
        _vocabulary.armCount = -1;
        _vocabulary.layoutSettings.plantStemFoot = float.NaN;
        _vocabulary.heads[HeadKind.Bud].plantStemScale = float.NaN;
        _vocabulary.heads[HeadKind.Bud].plantStem = new LookVocabulary.PlantStemEntry { segments = -1 };
        LookVocabulary.StemEntry stem = _vocabulary.stems[StemBand.Steady];
        stem.length = float.NaN;
        stem.plantShape.radialSegments = -1;
        CreatureRecipe recipe = Compose(LookSide.Stone);
        Assert.IsNotNull(recipe);
        Assert.AreEqual(0, recipe.roots.count);
        Assert.IsEmpty(recipe.arms);
    }

    [Test]
    public void Check_InvalidUnselectedPaletteColours_AcceptsTheSelectedUnit()
    {
        _vocabulary.palette = Track(Object.Instantiate(_vocabulary.palette));
        _vocabulary.palette.plantBody = new Color(float.NaN, 0f, 0f);
        _vocabulary.palette.mushroomStem = new Color(float.NaN, 0f, 0f);
        Assert.IsTrue(
            CreatureCompositionValidator.TryValidate(
                RenderTestAssets.CreateChannels(LookSide.Stone, HeadKind.Bud),
                _vocabulary,
                out string error
            ),
            error
        );
        _vocabulary.palette.stoneBody = new Color(float.NaN, 0f, 0f);
        Assert.IsFalse(
            CreatureCompositionValidator.TryValidate(
                RenderTestAssets.CreateChannels(LookSide.Stone, HeadKind.Bud),
                _vocabulary,
                out error
            )
        );
        Assert.AreEqual("Palette colours must be finite.", error);
    }

    [Test]
    public void Check_LegacyLayoutDefault_DoesNotMutateTheVocabulary()
    {
        _vocabulary.layout = null;
        CreatureCompositionValidator.Check(
            RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.Bud),
            _vocabulary,
            new List<string>()
        );
        Assert.IsNull(_vocabulary.layout);
        Assert.AreEqual(new LookVocabulary.LayoutEntry().plantBodySink, _vocabulary.layoutSettings.plantBodySink);
    }
}
}
