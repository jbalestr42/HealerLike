using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Studio
{

public class SpellPresetValidatorTests
{
    readonly List<Object> _objects = new List<Object>();
    SpellStudioPreset _preset;
    EffectVocabulary _vocabulary;

    [SetUp]
    public void SetUp()
    {
        _vocabulary = CreateTracked<EffectVocabulary>();
        _vocabulary.palette = CreateTracked<LookPalette>();
        _vocabulary.elements[EffectElement.Rise] = StudioTestAssets.CreateRise();
        _preset = CreateTracked<SpellStudioPreset>();
        _preset.vocabulary = _vocabulary;
        _preset.element = EffectElement.Rise;
        _preset.family = EffectFamily.Heal;
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

    T CreateTracked<T>() where T : ScriptableObject
    {
        T instance = ScriptableObject.CreateInstance<T>();
        _objects.Add(instance);
        return instance;
    }

    // A handler whose flat modifier was added in the inspector and never filled
    BuffHandlerFactory CreateHalfAuthoredHandler()
    {
        FlatModifierFactory modifier = CreateTracked<FlatModifierFactory>();
        modifier.data = null;
        BuffHandlerFactory handler = CreateTracked<BuffHandlerFactory>();
        handler.data = new BuffHandlerData { durationType = DurationType.Duration, isPeriodic = true,
            periodDuration = 2f, buffFactoryList = new List<ABuffFactory> { modifier } };
        return handler;
    }

    [Test]
    public void Validate_CompletePreset_ReturnsNoWarning()
    {
        string[] warnings = SpellPresetValidator.Validate(_preset);

        Assert.IsEmpty(warnings);
    }

    [Test]
    public void Validate_NoVocabulary_AsksForOne()
    {
        _preset.vocabulary = null;

        string[] warnings = SpellPresetValidator.Validate(_preset);

        StringAssert.Contains("vocabulary", string.Join(" ", warnings));
    }

    [Test]
    public void Validate_NoPalette_AsksForOneUntilTheColourIsAuthored()
    {
        _vocabulary.palette = null;
        StringAssert.Contains("palette", string.Join(" ", SpellPresetValidator.Validate(_preset)));

        _preset.overrideColour = true;
        _preset.colour = Color.cyan;

        Assert.IsEmpty(SpellPresetValidator.Validate(_preset));
    }

    [Test]
    public void Validate_MalformedParts_AsksForRepair()
    {
        _preset.CaptureEntry();
        _preset.entry.parts[0].size = new Vector3(-1f, float.NaN, 1000f);
        _preset.entry.stackBeads = null;

        string[] warnings = SpellPresetValidator.Validate(_preset);

        StringAssert.Contains("Part data needs repair", string.Join(" ", warnings));
    }

    [Test]
    public void Validate_EntryWithoutParts_WarnsItIsInvisible()
    {
        _preset.overrideEntry = true;
        _preset.entry = new ElementEntry { parts = null, stackBeads = null, criticalRings = null, sideRim = null };

        string[] warnings = SpellPresetValidator.Validate(_preset);

        StringAssert.Contains("invisible", string.Join(" ", warnings));
    }

    [Test]
    public void Validate_OutOfRangeContext_WarnsItIsBounded()
    {
        _preset.stacks = int.MaxValue;
        _preset.colour = new Color(float.NaN, 0f, 0f);
        _preset.overrideColour = true;

        string text = string.Join(" ", SpellPresetValidator.Validate(_preset));

        StringAssert.Contains("Numeric values", text);
        StringAssert.Contains("colour", text);
    }

    [Test]
    public void Validate_HandlerModeWithoutHandler_AsksForOne()
    {
        _preset.mode = SpellStudioMode.GameplayHandler;

        string[] warnings = SpellPresetValidator.Validate(_preset);

        StringAssert.Contains("gameplay buff handler", string.Join(" ", warnings));
    }

    [Test]
    public void Validate_HalfAuthoredHandler_ReportsTheMissingBuffData()
    {
        _preset.mode = SpellStudioMode.GameplayHandler;
        _preset.sourceHandler = CreateHalfAuthoredHandler();

        string[] warnings = SpellPresetValidator.Validate(_preset);

        StringAssert.Contains("missing buff data", string.Join(" ", warnings));
    }

    [Test]
    public void Validate_NativeRowOverHalfAuthoredHandler_ReturnsNoWarning()
    {
        BuffHandlerFactory handler = CreateHalfAuthoredHandler();
        SpellLooks looks = CreateTracked<SpellLooks>();
        looks.buffs[handler] = new SpellLook { element = EffectElement.Rise, family = EffectFamily.Heal,
            tempo = EffectTempo.PerPeriod };
        _preset.mode = SpellStudioMode.GameplayHandler;
        _preset.sourceHandler = handler;
        _preset.spellLooks = looks;

        string[] warnings = SpellPresetValidator.Validate(_preset);

        Assert.IsEmpty(warnings); // the row skips the derivation, as SpellVisualSink does
        Assert.NotNull(_preset.Compose());
    }

    [Test]
    public void IsDerivable_BuffWithoutData_ReturnsFalse()
    {
        Assert.IsFalse(SpellPresetValidator.IsDerivable(CreateHalfAuthoredHandler()));
    }

    [Test]
    public void IsDerivable_EveryShippedHandler_ReturnsTrue()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:ABuffHandlerFactory"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ABuffHandlerFactory handler = AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>(path);

            Assert.IsTrue(SpellPresetValidator.IsDerivable(handler), path);
        }
    }
}

}
