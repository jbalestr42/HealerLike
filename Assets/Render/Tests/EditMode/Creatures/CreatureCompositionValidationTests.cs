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

public class CreatureCompositionValidationTests : CreatureCompositionFixture
{
    [Test]
    public void Compose_InvalidArticulatedStem_IsRejectedInStudioAndRuntime()
    {
        _vocabulary.heads[HeadKind.Bud].plantStem = new LookVocabulary.PlantStemEntry { segments = 20 };
        CreatureGrammarPreset preset = Track(ScriptableObject.CreateInstance<CreatureGrammarPreset>());
        preset.vocabulary = _vocabulary;
        StringAssert.Contains(
            "Invalid articulated plant stem profile",
            string.Join(" ", CreatureGrammarValidator.Validate(preset))
        );
        LogAssert.Expect(LogType.Error, "[LookComposer] Invalid articulated plant stem profile.");
        Assert.IsNull(Compose(LookSide.Plant));
    }

    [TestCase(-0.1f)]
    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    public void Compose_InvalidFamilyStemScale_IsRejectedInStudioAndRuntime(float value)
    {
        _vocabulary.heads[HeadKind.Bud].plantStemScale = value;
        CreatureGrammarPreset preset = Track(ScriptableObject.CreateInstance<CreatureGrammarPreset>());
        preset.vocabulary = _vocabulary;
        StringAssert.Contains(
            "Plant family stem scale",
            string.Join(" ", CreatureGrammarValidator.Validate(preset))
        );
        LogAssert.Expect(
            LogType.Error,
            "[LookComposer] Plant family stem scale must be finite and nonnegative; zero keeps legacy length."
        );
        Assert.IsNull(Compose(LookSide.Plant));
    }

    [TestCase(true, -0.1f)]
    [TestCase(false, -0.1f)]
    [TestCase(true, float.NaN)]
    [TestCase(false, float.PositiveInfinity)]
    public void Compose_InvalidScaleOverride_RejectsTheEdit(bool head, float value)
    {
        if (head)
        {
            _vocabulary.bodies[MassBand.Light].headScale = value;
        }
        else
        {
            _vocabulary.bodies[MassBand.Light].stemScale = value;
        }

        LogAssert.Expect(
            LogType.Error,
            "[LookComposer] Invalid layout, shape profile or selected band dimensions."
        );
        Assert.IsNull(Compose(LookSide.Plant));
    }

    [Test]
    public void Compose_TooSmallBudget_RejectsTheRequestedFiveHeads()
    {
        _vocabulary.maxParts = 4;
        LogAssert.Expect(
            LogType.Error,
            "[LookComposer] DerivedPlantBud: Many requires 12 parts, exceeding the budget of 4; count is preserved."
        );
        Assert.IsNull(Compose(LookSide.Plant, CountBand.Many));
        UnitChannels channels = RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.Bud, CountBand.Many);
        Assert.AreEqual(5, LookComposer.Layout(channels, _vocabulary).headStarts.Count);
    }

    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(0f)]
    [TestCase(-1f)]
    public void Compose_InvalidLayout_RejectsBeforeConstructingGeometry(float width)
    {
        _vocabulary.layoutSettings.limbWidth = width;
        LogAssert.Expect(
            LogType.Error,
            "[LookComposer] Invalid layout, shape profile or selected band dimensions."
        );
        Assert.IsNull(Compose(LookSide.Stone));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Compose_EmptyBody_RejectsBeforeReadingItsSocket(bool nullArray)
    {
        _vocabulary.bodies[MassBand.Light].plant = nullArray ? null : Array.Empty<LookPart>();
        LogAssert.Expect(LogType.Error, "[LookComposer] The selected body fragment has no parts.");
        Assert.IsNull(Compose(LookSide.Plant));
    }

    [Test]
    public void Validate_InvalidAttachment_StopsStudioAndRuntimeComposition()
    {
        _vocabulary.heads[HeadKind.Bud].plant[0].attachTo = "Missing";
        CreatureGrammarPreset preset = Track(ScriptableObject.CreateInstance<CreatureGrammarPreset>());
        preset.vocabulary = _vocabulary;
        preset.side = LookSide.Plant;
        preset.head = HeadKind.Bud;
        StringAssert.Contains(
            "earlier unique active part",
            string.Join(" ", CreatureGrammarValidator.Validate(preset))
        );
        Assert.IsNull(preset.Compose());
        LogAssert.Expect(
            LogType.Error,
            "[LookComposer] The selected head fragment contains invalid part data: "
                + "Part 'Tip' must attach to an earlier unique active part in its fragment: 'Missing'."
        );
        Assert.IsNull(Compose(LookSide.Plant));
    }

    [Test]
    public void Validate_InvalidProfile_ReportsItInStudio()
    {
        ShapeProfile invalid = ShapeProfile.Bulb();
        invalid.fullness = float.NaN;
        _vocabulary.heads[HeadKind.Bud].plant[0].shape = invalid;
        CreatureGrammarPreset preset = Track(ScriptableObject.CreateInstance<CreatureGrammarPreset>());
        preset.vocabulary = _vocabulary;
        preset.side = LookSide.Plant;
        preset.head = HeadKind.Bud;
        StringAssert.Contains("invalid part data", string.Join(" ", CreatureGrammarValidator.Validate(preset)));
    }

    [Test]
    public void Validate_InvalidRootProportion_ReportsItInStudio()
    {
        _vocabulary.roots[ReachBand.Short].jointScale = float.NaN;
        CreatureGrammarPreset preset = Track(ScriptableObject.CreateInstance<CreatureGrammarPreset>());
        preset.vocabulary = _vocabulary;
        preset.side = LookSide.Plant;
        preset.head = HeadKind.Bud;
        preset.reach = ReachBand.Short;
        StringAssert.Contains("root profiles", string.Join(" ", CreatureGrammarValidator.Validate(preset)));
    }
}
}
