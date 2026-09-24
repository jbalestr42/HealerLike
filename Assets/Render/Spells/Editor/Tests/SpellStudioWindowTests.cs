using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Spells.Editor.Studio;
using HealerLike.Render.Spells.Studio;

namespace HealerLike.Render.Spells.Tests
{
    public class SpellStudioWindowTests
    {
        private SpellStudioWindow window;
        private string originalDrafts;
        private bool hadDrafts;
        private string Key => "HealerLike.SpellStudio.Drafts." + Application.dataPath;

        [SetUp]
        public void SetUp()
        {
            hadDrafts = EditorPrefs.HasKey(Key);
            originalDrafts = EditorPrefs.GetString(Key);
            EditorPrefs.DeleteKey(Key);
            window = ScriptableObject.CreateInstance<SpellStudioWindow>();
        }

        [TearDown]
        public void TearDown()
        {
            if (window != null) Object.DestroyImmediate(window);
            if (hadDrafts) EditorPrefs.SetString(Key, originalDrafts);
            else EditorPrefs.DeleteKey(Key);
        }

        private T Get<T>(string name) => (T)typeof(SpellStudioWindow).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(window);
        private void Invoke(string name) => typeof(SpellStudioWindow).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,null);

        [TestCase(false)]
        [TestCase(true)]
        public void OpenPresetSelectsTheRequestedRecipe(bool saved)
        {
            var preset = ScriptableObject.CreateInstance<SpellStudioPreset>();
            string path = null;
            SpellStudioWindow opened = null;
            try
            {
                if (saved)
                {
                    path = AssetDatabase.GenerateUniqueAssetPath("Assets/__SpellStudioPresetOpenTest.asset");
                    AssetDatabase.CreateAsset(preset,path);
                }
                opened = SpellStudioWindow.OpenPreset(preset);
                var selectedField = typeof(SpellStudioWindow).GetField("selected",BindingFlags.Instance|BindingFlags.NonPublic);
                Assert.That(selectedField.GetValue(opened),Is.SameAs(preset));
            }
            finally
            {
                if (opened != null && opened != window) Object.DestroyImmediate(opened);
                if (path != null) AssetDatabase.DeleteAsset(path);
                else Object.DestroyImmediate(preset);
            }
        }

        [Test]
        public void StartsWithEveryVocabularyElementAsAnIndependentDraft()
        {
            var drafts = Get<List<SpellStudioPreset>>("drafts");
            int vocabularyCount = System.Enum.GetValues(typeof(EffectElement)).Length;
            Assert.That(drafts.Count,Is.EqualTo(vocabularyCount + 3));
            var authored = drafts.Where(draft => draft.mode == SpellStudioMode.AuthoredElement).ToList();
            Assert.That(authored.Count,Is.EqualTo(vocabularyCount));
            CollectionAssert.AreEquivalent(System.Enum.GetValues(typeof(EffectElement)), authored.Select(draft => draft.element));
            var grammar = drafts.Where(draft => draft.mode != SpellStudioMode.AuthoredElement).ToList();
            CollectionAssert.AreEquivalent(new[] { "Healing pulse", "Defence boon", "Opposing debuff" }, grammar.Select(draft => draft.displayName));
            Assert.That(grammar.Count(draft => draft.mode == SpellStudioMode.GrammarChannels), Is.EqualTo(2));
            Assert.That(grammar.Count(draft => draft.mode == SpellStudioMode.GameplayHandler), Is.EqualTo(1));
            foreach (var draft in drafts)
            {
                Assert.That(draft.Compose(), Is.Not.Null);
                Assert.That(AssetDatabase.Contains(draft),Is.False);
                Assert.That(draft.vocabulary,Is.Not.Null);
            }
        }

        [Test]
        public void DuplicateOwnsDetachedEditableShape()
        {
            var source = Get<SpellStudioPreset>("selected");
            Assert.That(source.CaptureEntry(),Is.True);
            string originalId = source.entry.parts[0].id;
            Invoke("Duplicate");
            var copy = Get<SpellStudioPreset>("selected");
            copy.entry.parts[0].id = "changed copy";
            Assert.That(copy,Is.Not.SameAs(source));
            Assert.That(source.entry.parts[0].id,Is.EqualTo(originalId));
            Assert.That(copy.vocabulary,Is.SameAs(source.vocabulary));
            Assert.That(AssetDatabase.Contains(copy),Is.False);
        }

        [Test]
        public void ReopeningKeepsTheSelectedNewDraft()
        {
            int initialCount = Get<List<SpellStudioPreset>>("drafts").Count;
            Invoke("NewDraft");
            Get<SpellStudioPreset>("selected").displayName = "Selected custom draft";
            Object.DestroyImmediate(window);
            window = ScriptableObject.CreateInstance<SpellStudioWindow>();
            Assert.That(Get<SpellStudioPreset>("selected").displayName,Is.EqualTo("Selected custom draft"));
            Assert.That(Get<List<SpellStudioPreset>>("drafts").Count,Is.EqualTo(initialCount + 1));
            Assert.That(Get<List<SpellStudioPreset>>("drafts").Count(draft => draft.mode != SpellStudioMode.AuthoredElement), Is.EqualTo(3));
        }

        [Test]
        public void EmptyNativeProjectileRowHasRepairReadoutInsteadOfThrowing()
        {
            var preset = ScriptableObject.CreateInstance<SpellStudioPreset>();
            var looks = ScriptableObject.CreateInstance<SpellLooks>();
            var projectile = new GameObject("Null-row projectile");
            try
            {
                preset.sourceProjectile = projectile;
                preset.spellLooks = looks;
                looks.projectiles[projectile] = null;
                MethodInfo readout = typeof(SpellStudioWindow).GetMethod("ProjectileReadout", BindingFlags.NonPublic | BindingFlags.Static);
                string text = null;
                Assert.DoesNotThrow(() => text = (string)readout.Invoke(null, new object[] { preset }));
                StringAssert.Contains("empty", text);
                Assert.That(looks.projectiles[projectile], Is.Null, "The inspector must not silently repair native data.");
            }
            finally
            {
                Object.DestroyImmediate(projectile);
                Object.DestroyImmediate(looks);
                Object.DestroyImmediate(preset);
            }
        }

        [Test]
        public void SavingPresetCopyDoesNotSaveOtherDirtyAssets()
        {
            string suffix = System.Guid.NewGuid().ToString("N");
            string unrelatedPath = "Assets/__SpellStudioUnrelated_" + suffix + ".asset";
            string copyPath = "Assets/__SpellStudioSavedCopy_" + suffix + ".asset";
            var unrelated = ScriptableObject.CreateInstance<SpellStudioPreset>();
            try
            {
                unrelated.displayName = "Original disk value";
                AssetDatabase.CreateAsset(unrelated, unrelatedPath);
                AssetDatabase.SaveAssetIfDirty(unrelated);
                unrelated.displayName = "Unrelated unsaved edit";
                EditorUtility.SetDirty(unrelated);
                var source = Get<SpellStudioPreset>("selected");
                MethodInfo save = typeof(SpellStudioWindow).GetMethod("CreatePresetAsset", BindingFlags.NonPublic | BindingFlags.Static);
                var copy = (SpellStudioPreset)save.Invoke(null, new object[] { source, copyPath });
                Assert.That(AssetDatabase.Contains(copy), Is.True);
                Assert.That(EditorUtility.IsDirty(copy), Is.False);
                Assert.That(EditorUtility.IsDirty(unrelated), Is.True);
                StringAssert.Contains("Original disk value", System.IO.File.ReadAllText(unrelatedPath));
                StringAssert.DoesNotContain("Unrelated unsaved edit", System.IO.File.ReadAllText(unrelatedPath));
                Assert.That(copy.displayName, Is.EqualTo(source.displayName));
            }
            finally
            {
                AssetDatabase.DeleteAsset(copyPath);
                AssetDatabase.DeleteAsset(unrelatedPath);
                if (unrelated && !AssetDatabase.Contains(unrelated)) Object.DestroyImmediate(unrelated);
            }
        }

        [Test]
        public void DraftsSurviveWindowRecreationWithVocabularyAndAuthoredParts()
        {
            var selected = Get<SpellStudioPreset>("selected");
            selected.displayName = "Persistence test";
            Assert.That(selected.CaptureEntry(),Is.True);
            selected.entry.parts[0].id = "persisted part";
            var vocabulary = selected.vocabulary;
            Object.DestroyImmediate(window);
            window = ScriptableObject.CreateInstance<SpellStudioWindow>();
            selected = Get<SpellStudioPreset>("selected");
            Assert.That(selected.displayName,Is.EqualTo("Persistence test"));
            Assert.That(selected.entry.parts[0].id,Is.EqualTo("persisted part"));
            Assert.That(selected.vocabulary,Is.SameAs(vocabulary));
        }
    }
}
