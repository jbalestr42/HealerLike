using System;
using System.Collections.Generic;
using System.Linq;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells.Studio;
using HealerLike.Render.Spells.Editor.Studio;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Spells.Editor.Tests
{
    public sealed class SpellStudioGrammarTests
    {
        SpellStudioPreset _preset;
        EffectVocabulary _vocabulary;
        readonly List<Object> _owned = new List<Object>();
        const string HandlerPath = "Assets/Data/CharacterSkills/MultiTargetReduceDamage/BuffHandlerFactory.asset";

        [SetUp]
        public void SetUp()
        {
            _preset = Own(ScriptableObject.CreateInstance<SpellStudioPreset>());
            _vocabulary = AssetDatabase.LoadAssetAtPath<EffectVocabulary>(SpellStudioSamples.VocabularyPath);
            Assert.That(_vocabulary, Is.Not.Null);
            _preset.vocabulary = _vocabulary;
        }
        T Own<T>(T item) where T : Object { _owned.Add(item); return item; }
        [TearDown]
        public void TearDown()
        {
            foreach (Object item in _owned) if (item) Object.DestroyImmediate(item);
            _owned.Clear();
        }

        static IEnumerable<object[]> ChannelCases()
        {
            foreach (EffectFamily family in Enum.GetValues(typeof(EffectFamily)))
                foreach (AttributeGroup group in Enum.GetValues(typeof(AttributeGroup)))
                    foreach (EffectTempo tempo in Enum.GetValues(typeof(EffectTempo)))
                        yield return new object[] { family, group, tempo };
        }

        [TestCaseSource(nameof(ChannelCases))]
        public void GrammarChannelsMatchRuntimeComposer(EffectFamily family, AttributeGroup group, EffectTempo tempo)
        {
            _preset.mode = SpellStudioMode.GrammarChannels;
            _preset.family = family;
            _preset.attributeGroup = group;
            _preset.tempo = tempo;
            _preset.periodSeconds = 1.37f;
            _preset.element = EffectElement.Beam; // A stale manual element must not influence grammar.
            var channels = new EffectChannels { family = family, group = group, tempo = tempo, periodSeconds = 1.37f };
            EffectRecipe expected = EffectComposer.Compose(_vocabulary, EffectComposer.Element(channels), channels.family, channels.tempo, channels.periodSeconds, _preset.stacks, _preset.charges, _preset.amount);
            AssertRecipe(_preset.Compose(), expected);
            Assert.That(_preset.ResolvedElement, Is.EqualTo(EffectComposer.Element(channels)));
            Assert.That(_preset.element, Is.EqualTo(EffectElement.Beam));
        }

        [Test]
        public void EveryGameplayHandlerMatchesRuntimeGrammarForBothSides()
        {
            _preset.mode = SpellStudioMode.GameplayHandler;
            _preset.useGameplayOverrides = false;
            string[] guids = AssetDatabase.FindAssets("t:ABuffHandlerFactory");
            Assert.That(guids.Length, Is.GreaterThan(0));
            foreach (string guid in guids)
            {
                _preset.sourceHandler = AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (bool sameSide in new[] { true, false })
                {
                    _preset.isSameSide = sameSide;
                    EffectChannels channels = EffectDerivation.Channels(_preset.sourceHandler, sameSide);
                    EffectRecipe expected = EffectComposer.Compose(_vocabulary, EffectComposer.Element(channels), channels.family, channels.tempo, channels.periodSeconds, _preset.stacks, _preset.charges, _preset.amount);
                    AssertRecipe(_preset.Compose(), expected);
                    Assert.That(_preset.ResolvedChannels.group, Is.EqualTo(channels.group));
                }
            }
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(2.75f)]
        public void PeriodFallbackExactlyMatchesRuntime(float period)
        {
            _preset.mode = SpellStudioMode.GrammarChannels;
            _preset.family = EffectFamily.Renew;
            _preset.tempo = EffectTempo.PerPeriod;
            _preset.periodSeconds = period;
            EffectRecipe expected = EffectComposer.Compose(_vocabulary, _preset.ResolvedChannels, 1, 1f);
            Assert.That(_preset.Compose().cycleSeconds, Is.EqualTo(expected.cycleSeconds));
        }

        [Test]
        public void NativeHandlerOverrideWinsAndUsesHandlersPeriodRatherThanManualPeriod()
        {
            var handler = Own(ScriptableObject.CreateInstance<BuffHandlerFactory>());
            handler.data = new BuffHandlerData { durationType = DurationType.Duration, isPeriodic = true, periodDuration = 2.4f, buffFactoryList = new List<ABuffFactory>() };
            var looks = Own(ScriptableObject.CreateInstance<SpellLooks>());
            var row = new SpellLook { element = EffectElement.ManaDown, family = EffectFamily.Bane, tempo = EffectTempo.PerPeriod };
            looks.buffs[handler] = row;
            _preset.mode = SpellStudioMode.GameplayHandler;
            _preset.sourceHandler = handler;
            _preset.spellLooks = looks;
            _preset.periodSeconds = 17f;
            EffectRecipe expected = EffectComposer.Compose(_vocabulary, row.element, row.family, row.tempo,
                EffectDerivation.Period(handler), _preset.stacks, _preset.charges, _preset.amount);
            Assert.That(_preset.UsesGameplayOverride, Is.True);
            AssertRecipe(_preset.Compose(), expected);
            _preset.useGameplayOverrides = false;
            Assert.That(_preset.UsesGameplayOverride, Is.False);
            Assert.That(_preset.Compose().element, Is.EqualTo(EffectComposer.Element(EffectDerivation.Channels(handler, true))));
            Assert.That(looks.buffs[handler], Is.SameAs(row));
            Assert.That(handler.data.periodDuration, Is.EqualTo(2.4f));
        }

        [Test]
        public void NativePeriodicOverrideOnNonperiodicHandlerFallsBackToEntryCycle()
        {
            _preset.mode = SpellStudioMode.GameplayHandler;
            _preset.sourceHandler = AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>(HandlerPath);
            var looks = Own(ScriptableObject.CreateInstance<SpellLooks>());
            looks.buffs[_preset.sourceHandler] = new SpellLook { element = EffectElement.Rise, family = EffectFamily.Heal, tempo = EffectTempo.PerPeriod };
            _preset.spellLooks = looks;
            _preset.periodSeconds = 99f;
            Assert.That(EffectDerivation.Period(_preset.sourceHandler), Is.Zero);
            Assert.That(_preset.Compose().cycleSeconds, Is.EqualTo(_vocabulary.elements[EffectElement.Rise].cycleSeconds));
        }

        [Test]
        public void CaptureAndPreviewDurationUseResolvedElementAndTempoWithoutMutatingSources()
        {
            _preset.mode = SpellStudioMode.GrammarChannels;
            _preset.family = EffectFamily.Boon;
            _preset.attributeGroup = AttributeGroup.Prevention;
            _preset.element = EffectElement.Burst;
            _preset.tempo = EffectTempo.Once;
            ElementEntry source = _vocabulary.elements[EffectElement.Bud];
            Vector3 original = source.parts[0].size;
            Assert.That(_preset.PreviewDuration, Is.EqualTo(source.cycleSeconds));
            Assert.That(_preset.CaptureEntry(), Is.True);
            Assert.That(_preset.entry.parts, Is.Not.SameAs(source.parts));
            _preset.entry.parts[0].size = Vector3.one * 2f;
            Assert.That(_preset.Compose().element, Is.EqualTo(EffectElement.Bud));
            Assert.That(source.parts[0].size, Is.EqualTo(original));
            _preset.mode = SpellStudioMode.GameplayHandler;
            _preset.sourceHandler = AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>(HandlerPath);
            _preset.durationSeconds = 8f;
            Assert.That(_preset.ResolvedChannels.tempo, Is.EqualTo(EffectTempo.ForDuration));
            Assert.That(_preset.PreviewDuration, Is.EqualTo(8f));
        }

        [Test]
        public void MissingHandlerDoesNotFabricateAnEffectAndManualModeIsBackwardsCompatible()
        {
            Assert.That(_preset.mode, Is.EqualTo(SpellStudioMode.AuthoredElement));
            _preset.element = EffectElement.ManaUp;
            Assert.That(_preset.Compose().element, Is.EqualTo(EffectElement.ManaUp));
            _preset.mode = SpellStudioMode.GameplayHandler;
            Assert.That(_preset.Compose(), Is.Null);
            Assert.That(_preset.CaptureEntry(), Is.False);
            Assert.That(_preset.Validate().Any(warning => warning.Contains("gameplay buff handler")), Is.True);
        }

        [TestCase(6, SpellStudioMode.GrammarChannels, EffectElement.Stalks)]
        [TestCase(7, SpellStudioMode.GrammarChannels, EffectElement.Plates)]
        [TestCase(8, SpellStudioMode.GameplayHandler, EffectElement.Press)]
        public void GrammarStarterPresetsStayLinkedToNativeVocabulary(int index, SpellStudioMode mode, EffectElement element)
        {
            SpellStudioPreset sample = Own(SpellStudioSamples.Build(_vocabulary, index));
            Assert.That(sample, Is.Not.Null);
            Assert.That(sample.mode, Is.EqualTo(mode));
            Assert.That(sample.overrideEntry, Is.False);
            Assert.That(sample.overrideColour, Is.False);
            Assert.That(sample.Compose().element, Is.EqualTo(element));
        }

        [Test]
        public void AssetRoundtripKeepsGrammarModeChannelsAndGameplayReferences()
        {
            string path = "Assets/__SpellStudioGrammar_" + Guid.NewGuid().ToString("N") + ".asset";
            _preset.mode = SpellStudioMode.GameplayHandler;
            _preset.sourceHandler = AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>(HandlerPath);
            _preset.spellLooks = AssetDatabase.LoadAssetAtPath<SpellLooks>("Assets/Render/Spells/Data/SpellLooks.asset");
            _preset.isSameSide = false;
            _preset.useGameplayOverrides = false;
            _preset.attributeGroup = AttributeGroup.Prevention;
            try
            {
                var saved = Object.Instantiate(_preset);
                AssetDatabase.CreateAsset(saved, path);
                AssetDatabase.SaveAssetIfDirty(saved);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var loaded = AssetDatabase.LoadAssetAtPath<SpellStudioPreset>(path);
                Assert.That(loaded.mode, Is.EqualTo(SpellStudioMode.GameplayHandler));
                Assert.That(loaded.sourceHandler, Is.EqualTo(_preset.sourceHandler));
                Assert.That(loaded.spellLooks, Is.EqualTo(_preset.spellLooks));
                Assert.That(loaded.vocabulary, Is.EqualTo(_vocabulary));
                Assert.That(loaded.isSameSide, Is.False);
                Assert.That(loaded.useGameplayOverrides, Is.False);
                Assert.That(loaded.attributeGroup, Is.EqualTo(AttributeGroup.Prevention));
                AssertRecipe(loaded.Compose(), _preset.Compose());
            }
            finally { AssetDatabase.DeleteAsset(path); }
        }

        static void AssertRecipe(EffectRecipe actual, EffectRecipe expected)
        {
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual.element, Is.EqualTo(expected.element));
            Assert.That(actual.family, Is.EqualTo(expected.family));
            Assert.That(actual.tempo, Is.EqualTo(expected.tempo));
            Assert.That(actual.cycleSeconds, Is.EqualTo(expected.cycleSeconds));
            Assert.That(actual.colour, Is.EqualTo(expected.colour));
            Assert.That(actual.count, Is.EqualTo(expected.count));
            Assert.That(actual.motion, Is.EqualTo(expected.motion));
            Assert.That(actual.socket, Is.EqualTo(expected.socket));
        }
    }
}
