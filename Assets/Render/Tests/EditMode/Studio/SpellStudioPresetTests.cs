using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Spells;

using HealerLike.Render.Studio.Editor;

namespace HealerLike.Render.Studio
{
    public class SpellStudioPresetTests
    {
        SpellStudioPreset preset;
        EffectVocabulary vocabulary;
        LookPalette palette;

        [SetUp]
        public void SetUp()
        {
            preset = ScriptableObject.CreateInstance<SpellStudioPreset>();
            vocabulary = ScriptableObject.CreateInstance<EffectVocabulary>();
            palette = ScriptableObject.CreateInstance<LookPalette>();
            palette.heal = Color.green;
            vocabulary.palette = palette;
            vocabulary.elements[EffectElement.Rise] = new ElementEntry
            {
                parts = new[] { Part("First"), Part("Second"), Part("Third"), Part("Fourth") },
                stackBeads = new[] { Part("Bead") }, criticalRings = new[] { Part("Critical") },
                sideRim = new[] { Part("Side") }, motion = EffectMotionKind.Rise, socket = EffectSocket.Feet,
                count = EffectCount.Amount, minCount = 1, cycleSeconds = 0.75f
            };
            preset.vocabulary = vocabulary;
            preset.element = EffectElement.Rise;
            preset.family = EffectFamily.Heal;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(preset);
            Object.DestroyImmediate(vocabulary);
            Object.DestroyImmediate(palette);
        }

        static LookPart Part(string id) => new LookPart { id = id, size = Vector3.one, colour = ColourRole.Accent };

        [Test]
        public void Compose_UsesRuntimeCountColourAndPeriodSemantics()
        {
            preset.tempo = EffectTempo.PerPeriod;
            preset.periodSeconds = 1.75f;
            preset.amount = 0.5f;
            EffectRecipe actual = preset.Compose();
            EffectRecipe expected = EffectComposer.Compose(vocabulary, preset.element, preset.family, preset.tempo,
                preset.periodSeconds, preset.stacks, preset.charges, preset.amount);
            Assert.AreEqual(expected.count, actual.count);
            Assert.AreEqual(expected.colour, actual.colour);
            Assert.AreEqual(expected.cycleSeconds, actual.cycleSeconds);
            Assert.AreEqual(expected.motion, actual.motion);
            Assert.AreEqual(expected.socket, actual.socket);
        }

        [Test]
        public void Compose_ReturnsIndependentEntriesAcrossAllLayers()
        {
            ElementEntry source = vocabulary.elements[preset.element];
            EffectRecipe first = preset.Compose();
            EffectRecipe second = preset.Compose();
            first.entry.parts[0].position = Vector3.up;
            first.entry.stackBeads[0].id = "Changed";
            first.entry.criticalRings[0].size = Vector3.zero;
            first.entry.sideRim[0].glow = 10f;
            Assert.AreEqual(Vector3.zero, source.parts[0].position);
            Assert.AreEqual(Vector3.zero, second.entry.parts[0].position);
            Assert.AreEqual("Bead", source.stackBeads[0].id);
            Assert.AreEqual(Vector3.one, source.criticalRings[0].size);
            Assert.AreEqual(0f, source.sideRim[0].glow);
            Assert.AreNotSame(source, first.entry);
            Assert.AreNotSame(first.entry, second.entry);
        }

        [Test]
        public void CaptureEntry_CopiesThenComposesEditedGeometryWithoutVocabularyMutation()
        {
            Assert.IsTrue(preset.CaptureEntry());
            Assert.IsTrue(preset.overrideEntry);
            preset.entry.parts[0].position = new Vector3(2f, 3f, 4f);
            preset.entry.motion = EffectMotionKind.Orbit;
            preset.entry.count = EffectCount.Stacks;
            preset.stacks = 2;
            EffectRecipe result = preset.Compose();
            Assert.AreEqual(new Vector3(2f, 3f, 4f), result.entry.parts[0].position);
            Assert.AreEqual(EffectMotionKind.Orbit, result.motion);
            Assert.AreEqual(2, result.count);
            Assert.AreEqual(Vector3.zero, vocabulary.elements[preset.element].parts[0].position);
        }

        [Test]
        public void MissingSources_ReturnNullAndDiagnosticsWithoutErrors()
        {
            preset.vocabulary = null;
            Assert.IsNull(preset.Compose());
            Assert.IsFalse(preset.CaptureEntry());
            Assert.IsNotEmpty(preset.Validate());
            preset.vocabulary = vocabulary;
            preset.element = EffectElement.Beam;
            Assert.IsNull(preset.Compose());
            Assert.IsFalse(preset.CaptureEntry());
            preset.overrideEntry = true;
            preset.entry = null;
            Assert.IsNull(preset.Compose());
        }

        [Test]
        public void AuthoredEntry_CanComposeWithoutVocabulary()
        {
            preset.CaptureEntry();
            preset.vocabulary = null;
            preset.overrideColour = true;
            preset.colour = Color.magenta;
            EffectRecipe result = preset.Compose();
            Assert.NotNull(result);
            Assert.AreEqual(Color.magenta, result.colour);
            Assert.IsNull(result.palette);
        }

        [Test]
        public void MissingPalette_ReturnsDiagnosticUnlessColourIsExplicitlyAuthored()
        {
            vocabulary.palette = null;
            Assert.IsNull(preset.Compose());
            Assert.That(string.Join(" ", preset.Validate()), Does.Contain("palette"));
            preset.overrideColour = true;
            preset.colour = Color.cyan;
            EffectRecipe result = preset.Compose();
            Assert.NotNull(result);
            Assert.IsNull(result.palette);
            Assert.AreEqual(Color.cyan, result.colour);
            Assert.IsEmpty(preset.Validate());
        }

        [Test]
        public void Compose_SanitizesMalformedDataWithoutEditingAuthoringFields()
        {
            preset.CaptureEntry();
            preset.entry.parts[0].position = new Vector3(float.NaN, float.PositiveInfinity, 1000f);
            preset.entry.parts[0].size = new Vector3(-1f, float.NaN, 1000f);
            preset.entry.parts[0].primitive = (Primitive)999;
            preset.entry.stackBeads = null;
            preset.entry.cycleSeconds = float.NaN;
            preset.entry.minCount = int.MaxValue;
            preset.entry.count = EffectCount.Stacks;
            preset.entry.motion = (EffectMotionKind)999;
            preset.stacks = int.MaxValue;
            preset.charges = float.PositiveInfinity;
            preset.amount = float.NaN;
            preset.scale = float.NaN;
            preset.side = (Entity.EntityType)999;
            preset.family = (EffectFamily)999;
            preset.overrideColour = true;
            preset.colour = new Color(float.NaN, float.PositiveInfinity, -2f, 10f);
            EffectRecipe result = preset.Compose();
            Assert.AreEqual(new Vector3(0f, 0f, 50f), result.entry.parts[0].position);
            Assert.AreEqual(new Vector3(0.001f, 0.1f, 20f), result.entry.parts[0].size);
            Assert.AreEqual(4, result.count);
            Assert.AreEqual(0.6f, result.cycleSeconds);
            Assert.AreEqual(EffectMotionKind.Burst, result.motion);
            Assert.AreEqual(EffectFamily.Damage, result.family);
            Assert.AreEqual(new Color(1f, 1f, 0f, 1f), result.colour);
            Assert.IsEmpty(result.entry.stackBeads);
            Assert.AreEqual(1f, preset.SafeScale);
            Assert.AreEqual(Entity.EntityType.Player, preset.SafeSide);
            Assert.IsTrue(float.IsNaN(preset.entry.parts[0].position.x));
            Assert.IsNull(preset.entry.stackBeads);
            Assert.AreEqual(int.MaxValue, preset.stacks);
            Assert.IsNotEmpty(preset.Validate());
        }

        [Test]
        public void Compose_NullPartArraysProduceSafeEmptyRecipe()
        {
            preset.overrideEntry = true;
            preset.entry = new ElementEntry { parts = null, stackBeads = null, criticalRings = null, sideRim = null };
            EffectRecipe result = preset.Compose();
            Assert.AreEqual(0, result.count);
            Assert.IsEmpty(result.entry.parts);
            Assert.IsNotEmpty(preset.Validate());
        }

        [Test]
        public void Compose_ExcessiveLayersAreBounded()
        {
            preset.CaptureEntry();
            preset.entry.parts = new LookPart[SpellStudioPreset.MaxParts + 20];
            preset.entry.count = EffectCount.Fixed;
            Assert.AreEqual(SpellStudioPreset.MaxParts, preset.Compose().count);
            Assert.AreEqual(SpellStudioPreset.MaxParts + 20, preset.entry.parts.Length);
        }

        [Test]
        public void PreviewDuration_UsesOneCycleForImpactsAndDurationForStatuses()
        {
            preset.durationSeconds = 7f;
            Assert.AreEqual(0.75f, preset.PreviewDuration);
            preset.tempo = EffectTempo.ForDuration;
            Assert.AreEqual(7f, preset.PreviewDuration);
            preset.durationSeconds = float.NaN;
            Assert.AreEqual(4f, preset.PreviewDuration);
            preset.tempo = EffectTempo.Once;
            preset.CaptureEntry();
            preset.entry.cycleSeconds = -1f;
            Assert.AreEqual(0.01f, preset.PreviewDuration);
        }

        [Test]
        public void ValidPreset_HasNoWarningsAndCompositionLeavesRandomStateUnchanged()
        {
            Assert.IsEmpty(preset.Validate());
            Random.State before = Random.state;
            EffectRecipe a = preset.Compose();
            EffectRecipe b = preset.Compose();
            Assert.AreEqual(before, Random.state);
            Assert.AreEqual(a.count, b.count);
            Assert.AreEqual(a.colour, b.colour);
            Assert.AreEqual(a.entry.parts[2].position, b.entry.parts[2].position);
        }

        [Test]
        public void GameplayHandler_MissingNestedModifierDataReturnsDiagnosticWithoutThrowing()
        {
            var handler = ScriptableObject.CreateInstance<BuffHandlerFactory>();
            var modifier = ScriptableObject.CreateInstance<FlatModifierFactory>();
            try
            {
                modifier.data = null;
                handler.data = new BuffHandlerData { durationType = DurationType.Duration,
                    buffFactoryList = new System.Collections.Generic.List<ABuffFactory> { modifier } };
                preset.mode = SpellStudioMode.GameplayHandler; preset.sourceHandler = handler;
                Assert.IsFalse(preset.TryResolve(out _, out _));
                Assert.IsNull(preset.Compose());
                Assert.That(string.Join(" ", preset.Validate()), Does.Contain("missing buff data"));
                Assert.DoesNotThrow(() => { var channels = preset.ResolvedChannels; var duration = preset.PreviewDuration; });
            }
            finally { Object.DestroyImmediate(handler); Object.DestroyImmediate(modifier); }
        }

        [Test]
        public void GameplayOverride_BypassesUnneededNestedBuffDerivationLikeRuntime()
        {
            var handler = ScriptableObject.CreateInstance<BuffHandlerFactory>();
            var modifier = ScriptableObject.CreateInstance<FlatModifierFactory>();
            var looks = ScriptableObject.CreateInstance<SpellLooks>();
            try
            {
                modifier.data = null;
                handler.data = new BuffHandlerData { durationType = DurationType.Duration, isPeriodic = true,
                    periodDuration = 2f, buffFactoryList = new System.Collections.Generic.List<ABuffFactory> { modifier } };
                looks.buffs[handler] = new SpellLook { element = EffectElement.Rise, family = EffectFamily.Heal, tempo = EffectTempo.PerPeriod };
                preset.mode = SpellStudioMode.GameplayHandler; preset.sourceHandler = handler; preset.spellLooks = looks;
                Assert.IsTrue(preset.TryResolve(out _, out _));
                EffectRecipe actual = preset.Compose();
                EffectRecipe expected = EffectComposer.Compose(vocabulary, EffectElement.Rise, EffectFamily.Heal,
                    EffectTempo.PerPeriod, EffectDerivation.Period(handler), preset.stacks, preset.charges, preset.amount);
                Assert.NotNull(actual); Assert.AreEqual(expected.element, actual.element);
                Assert.AreEqual(expected.family, actual.family); Assert.AreEqual(expected.cycleSeconds, actual.cycleSeconds);
                Assert.IsEmpty(preset.Validate());
            }
            finally { Object.DestroyImmediate(looks); Object.DestroyImmediate(handler); Object.DestroyImmediate(modifier); }
        }

        [Test]
        public void SavedAsset_ReloadsAuthoredPartsSettingsAndColour()
        {
            string path = "Assets/__SpellStudioPresetTest_" + System.Guid.NewGuid().ToString("N") + ".asset";
            SpellStudioPreset saved = null;
            try
            {
                saved = Object.Instantiate(preset);
                saved.CaptureEntry();
                // Only reference shipped/persistent assets from a persistent preset. This test's vocabulary is transient.
                saved.vocabulary = null;
                saved.displayName = "Persistent test spell";
                saved.description = "A deliberately authored visual";
                saved.tempo = EffectTempo.PerPeriod;
                saved.periodSeconds = 2.25f;
                saved.durationSeconds = 9f;
                saved.stacks = 3;
                saved.charges = 2.5f;
                saved.amount = 0.4f;
                saved.scale = 1.6f;
                saved.critical = true;
                saved.side = Entity.EntityType.Computer;
                saved.overrideColour = true;
                saved.colour = new Color(0.2f, 0.4f, 0.8f, 1f);
                saved.entry.parts[0].position = new Vector3(2f, 3f, 4f);
                saved.entry.stackBeads[0].glow = 2f;
                saved.entry.criticalRings[0].id = "Saved ring";
                saved.entry.sideRim[0].size = Vector3.one * 2f;
                AssetDatabase.CreateAsset(saved, path);
                AssetDatabase.SaveAssetIfDirty(saved);
                Resources.UnloadAsset(saved);
                saved = null;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                saved = AssetDatabase.LoadAssetAtPath<SpellStudioPreset>(path);
                Assert.NotNull(saved);
                Assert.AreEqual("Persistent test spell", saved.displayName);
                Assert.AreEqual("A deliberately authored visual", saved.description);
                Assert.AreEqual(EffectTempo.PerPeriod, saved.tempo);
                Assert.AreEqual(2.25f, saved.periodSeconds);
                Assert.AreEqual(9f, saved.durationSeconds);
                Assert.AreEqual(3, saved.stacks);
                Assert.AreEqual(2.5f, saved.charges);
                Assert.AreEqual(0.4f, saved.amount);
                Assert.AreEqual(1.6f, saved.scale);
                Assert.IsTrue(saved.critical);
                Assert.AreEqual(Entity.EntityType.Computer, saved.side);
                Assert.IsTrue(saved.overrideEntry);
                Assert.IsTrue(saved.overrideColour);
                Assert.AreEqual(new Color(0.2f, 0.4f, 0.8f, 1f), saved.colour);
                Assert.AreEqual(new Vector3(2f, 3f, 4f), saved.entry.parts[0].position);
                Assert.AreEqual(2f, saved.entry.stackBeads[0].glow);
                Assert.AreEqual("Saved ring", saved.entry.criticalRings[0].id);
                Assert.AreEqual(Vector3.one * 2f, saved.entry.sideRim[0].size);
                Assert.AreEqual(saved.colour, saved.Compose().colour);
                Assert.AreEqual(2.25f, saved.Compose().cycleSeconds);
                Assert.AreEqual(Vector3.zero, vocabulary.elements[preset.element].parts[0].position);
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
                if (saved != null && !EditorUtility.IsPersistent(saved)) Object.DestroyImmediate(saved);
            }
        }

        [Test]
        public void CloneEntry_HandlesNullAndNormalizesMissingArrays()
        {
            Assert.IsNull(SpellStudioPreset.CloneEntry(null));
            ElementEntry cloned = SpellStudioPreset.CloneEntry(new ElementEntry { parts = null });
            Assert.IsEmpty(cloned.parts);
        }
    }
}
