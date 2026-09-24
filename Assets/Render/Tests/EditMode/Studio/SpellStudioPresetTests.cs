using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Studio
{

public class SpellStudioPresetTests
{
    readonly List<Object> _objects = new List<Object>();
    readonly List<string> _paths = new List<string>();
    SpellStudioPreset _preset;
    EffectVocabulary _vocabulary;

    // A four-shape Rise entry counted by amount, on its own palette
    [SetUp]
    public void SetUp()
    {
        _vocabulary = CreateTracked<EffectVocabulary>();
        _vocabulary.palette = CreateTracked<LookPalette>();
        _vocabulary.palette.heal = Color.green;
        _vocabulary.elements[EffectElement.Rise] = StudioTestAssets.CreateRise();
        _preset = CreateTracked<SpellStudioPreset>();
        _preset.vocabulary = _vocabulary;
        _preset.element = EffectElement.Rise;
        _preset.family = EffectFamily.Heal;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (string path in _paths)
        {
            AssetDatabase.DeleteAsset(path);
        }

        foreach (Object trackedObject in _objects)
        {
            Object.DestroyImmediate(trackedObject);
        }
        _paths.Clear();
        _objects.Clear();
    }

    T CreateTracked<T>() where T : ScriptableObject
    {
        T instance = ScriptableObject.CreateInstance<T>();
        _objects.Add(instance);
        return instance;
    }

    [Test]
    public void Compose_PerPeriodHalfAmount_MatchesTheRuntimeComposer()
    {
        _preset.tempo = EffectTempo.PerPeriod;
        _preset.periodSeconds = 1.75f;
        _preset.amount = 0.5f;

        EffectRecipe actual = _preset.Compose();

        StudioTestAssets.AssertRecipe(EffectComposer.Compose(_vocabulary, EffectElement.Rise, EffectFamily.Heal,
            EffectTempo.PerPeriod, 1.75f, _preset.stacks, _preset.charges, 0.5f), actual);
    }

    [Test]
    public void CaptureEntry_EditedCopy_ComposesWithoutTouchingTheVocabulary()
    {
        Assert.IsTrue(_preset.CaptureEntry());
        _preset.entry.parts[0].position = new Vector3(2f, 3f, 4f);
        _preset.entry.motion = EffectMotionKind.Orbit;
        _preset.entry.count = EffectCount.Stacks;
        _preset.stacks = 2;

        EffectRecipe result = _preset.Compose();

        Assert.IsTrue(_preset.overrideEntry);
        Assert.AreEqual(new Vector3(2f, 3f, 4f), result.entry.parts[0].position);
        Assert.AreEqual(EffectMotionKind.Orbit, result.motion);
        Assert.AreEqual(2, result.count);
        Assert.AreEqual(Vector3.zero, _vocabulary.elements[EffectElement.Rise].parts[0].position);
    }

    [Test]
    public void Compose_MissingVocabularyElementOrEntry_ReturnsNull()
    {
        _preset.element = EffectElement.Beam;
        Assert.IsNull(_preset.Compose());
        Assert.IsFalse(_preset.CaptureEntry());

        _preset.vocabulary = null;
        Assert.IsNull(_preset.Compose());

        _preset.overrideEntry = true;
        _preset.entry = null;
        Assert.IsNull(_preset.Compose());
    }

    [Test]
    public void Compose_AuthoredColourWithoutPalette_DrawsTheColour()
    {
        _preset.CaptureEntry();
        _vocabulary.palette = null;
        Assert.IsNull(_preset.Compose()); // no palette and no colour of its own

        _preset.vocabulary = null;
        _preset.overrideColour = true;
        _preset.colour = Color.magenta;
        EffectRecipe result = _preset.Compose();

        Assert.AreEqual(Color.magenta, result.colour);
        Assert.IsNull(result.palette);
    }

    [Test]
    public void PreviewDuration_ImpactThenStatus_FollowsTheCycleThenTheLength()
    {
        _preset.durationSeconds = 7f;
        Assert.AreEqual(0.75f, _preset.previewDuration);

        _preset.tempo = EffectTempo.ForDuration;
        Assert.AreEqual(7f, _preset.previewDuration);

        _preset.durationSeconds = float.NaN;
        Assert.AreEqual(4f, _preset.previewDuration); // not finite, the default length
    }

    [Test]
    public void TryResolve_HandlerWithoutModifierData_ReturnsFalse()
    {
        BuffHandlerFactory handler = CreateTracked<BuffHandlerFactory>();
        FlatModifierFactory modifier = CreateTracked<FlatModifierFactory>();
        modifier.data = null;
        handler.data = new BuffHandlerData { durationType = DurationType.Duration,
            buffFactoryList = new List<ABuffFactory> { modifier } };
        _preset.mode = SpellStudioMode.GameplayHandler;
        _preset.sourceHandler = handler;

        EffectChannels channels;
        EffectElement element;
        bool isResolved = _preset.TryResolve(out channels, out element);

        Assert.IsFalse(isResolved);
        Assert.IsNull(_preset.Compose());
        Assert.AreEqual(0.6f, _preset.previewDuration); // no channels read as Once, with no entry the default cycle
    }

    [Test]
    public void Compose_NativeRowOverHandler_TakesTheRowAndTheHandlersPeriod()
    {
        BuffHandlerFactory handler = CreateTracked<BuffHandlerFactory>();
        handler.data = new BuffHandlerData { durationType = DurationType.Duration, isPeriodic = true,
            periodDuration = 2.4f, buffFactoryList = new List<ABuffFactory>() };
        SpellLooks looks = CreateTracked<SpellLooks>();
        looks.buffs[handler] = new SpellLook { element = EffectElement.Rise, family = EffectFamily.Heal,
            tempo = EffectTempo.PerPeriod };
        _preset.mode = SpellStudioMode.GameplayHandler;
        _preset.sourceHandler = handler;
        _preset.spellLooks = looks;
        _preset.periodSeconds = 17f;

        EffectRecipe actual = _preset.Compose();

        Assert.IsTrue(_preset.usesGameplayOverride);
        StudioTestAssets.AssertRecipe(EffectComposer.Compose(_vocabulary, EffectElement.Rise, EffectFamily.Heal,
            EffectTempo.PerPeriod, 2.4f, _preset.stacks, _preset.charges, _preset.amount), actual);
        _preset.useGameplayOverrides = false;
        Assert.IsFalse(_preset.usesGameplayOverride);
    }

    [TestCaseSource(typeof(StudioTestAssets), nameof(StudioTestAssets.ChannelCases))]
    public void Compose_GrammarChannels_MatchesTheRuntimeComposer(EffectFamily family, AttributeGroup group,
        EffectTempo tempo)
    {
        EffectVocabulary shipped = RenderTestAssets.LoadEffectVocabulary();
        _preset.vocabulary = shipped;
        _preset.mode = SpellStudioMode.GrammarChannels;
        _preset.family = family;
        _preset.attributeGroup = group;
        _preset.tempo = tempo;
        _preset.periodSeconds = 1.37f;
        _preset.element = EffectElement.Beam; // a stale manual element the grammar ignores

        EffectChannels channels = new EffectChannels { family = family, group = group, tempo = tempo,
            periodSeconds = 1.37f };
        EffectRecipe expected = EffectComposer.Compose(shipped, EffectComposer.Element(channels), family, tempo, 1.37f,
            _preset.stacks, _preset.charges, _preset.amount);

        StudioTestAssets.AssertRecipe(expected, _preset.Compose());
        Assert.AreEqual(EffectElement.Beam, _preset.element);
    }

    [Test]
    public void Compose_EveryGameplayHandler_MatchesTheRuntimeGrammarOnBothSides()
    {
        EffectVocabulary shipped = RenderTestAssets.LoadEffectVocabulary();
        _preset.vocabulary = shipped;
        _preset.mode = SpellStudioMode.GameplayHandler;
        _preset.useGameplayOverrides = false;
        string[] guids = AssetDatabase.FindAssets("t:ABuffHandlerFactory");

        Assert.Greater(guids.Length, 0);
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            _preset.sourceHandler = AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>(path);
            foreach (bool isSameSide in new bool[] { true, false })
            {
                _preset.isSameSide = isSameSide;
                EffectChannels channels = EffectDerivation.Channels(_preset.sourceHandler, isSameSide);
                EffectRecipe expected = EffectComposer.Compose(shipped, EffectComposer.Element(channels),
                    channels.family, channels.tempo, channels.periodSeconds, _preset.stacks, _preset.charges,
                    _preset.amount);
                StudioTestAssets.AssertRecipe(expected, _preset.Compose());
            }
        }
    }

    [TestCase(0f)]
    [TestCase(-1f)]
    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(2.75f)]
    public void Compose_TickingPeriod_FallsBackAsTheRuntimeDoes(float period)
    {
        _preset.vocabulary = RenderTestAssets.LoadEffectVocabulary();
        _preset.mode = SpellStudioMode.GrammarChannels;
        _preset.family = EffectFamily.Renew;
        _preset.tempo = EffectTempo.PerPeriod;
        _preset.periodSeconds = period;

        EffectRecipe expected = EffectComposer.Compose(_preset.vocabulary, _preset.resolvedChannels, 1, 1f);

        Assert.AreEqual(expected.cycleSeconds, _preset.Compose().cycleSeconds);
    }

    [Test]
    public void CaptureEntry_GrammarMode_CopiesTheResolvedElement()
    {
        EffectVocabulary shipped = RenderTestAssets.LoadEffectVocabulary();
        _preset.vocabulary = shipped;
        _preset.mode = SpellStudioMode.GrammarChannels;
        _preset.family = EffectFamily.Boon;
        _preset.attributeGroup = AttributeGroup.Prevention; // boon and prevention grow a bud
        _preset.tempo = EffectTempo.Once;
        Vector3 original = shipped.elements[EffectElement.Bud].parts[0].size;

        Assert.AreEqual(shipped.elements[EffectElement.Bud].cycleSeconds, _preset.previewDuration);
        Assert.IsTrue(_preset.CaptureEntry());
        _preset.entry.parts[0].size = Vector3.one * 2f;

        Assert.AreEqual(EffectElement.Bud, _preset.Compose().element);
        Assert.AreEqual(original, shipped.elements[EffectElement.Bud].parts[0].size);
    }

    [Test]
    public void SavedAsset_HandlerModeReloaded_KeepsModeChannelsAndReferences()
    {
        string path = "Assets/__SpellStudioPreset_" + Guid.NewGuid().ToString("N") + ".asset";
        _paths.Add(path);
        SpellStudioPreset saved = Object.Instantiate(_preset);
        saved.vocabulary = RenderTestAssets.LoadEffectVocabulary();
        saved.mode = SpellStudioMode.GameplayHandler;
        saved.sourceHandler = AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>(StudioTestAssets.OpposingHandlerPath);
        saved.isSameSide = false;
        saved.attributeGroup = AttributeGroup.Prevention;
        saved.colour = new Color(0.2f, 0.4f, 0.8f, 1f);
        AssetDatabase.CreateAsset(saved, path);
        AssetDatabase.SaveAssetIfDirty(saved);
        Resources.UnloadAsset(saved);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        SpellStudioPreset loaded = AssetDatabase.LoadAssetAtPath<SpellStudioPreset>(path);

        Assert.AreEqual(SpellStudioMode.GameplayHandler, loaded.mode);
        Assert.AreEqual(AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>(StudioTestAssets.OpposingHandlerPath),
            loaded.sourceHandler);
        Assert.IsFalse(loaded.isSameSide);
        Assert.AreEqual(AttributeGroup.Prevention, loaded.attributeGroup);
        Assert.AreEqual(new Color(0.2f, 0.4f, 0.8f, 1f), loaded.colour);
        Assert.NotNull(loaded.Compose());
    }
}

}
