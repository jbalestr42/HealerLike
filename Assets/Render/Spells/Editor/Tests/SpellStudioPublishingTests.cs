using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Spells.Studio.Editor.Tests
{
    public class SpellStudioPublishingTests
    {
        SpellStudioPreset preset;
        EffectVocabulary vocabulary;

        [SetUp]
        public void SetUp()
        {
            vocabulary = ScriptableObject.CreateInstance<EffectVocabulary>();
            preset = ScriptableObject.CreateInstance<SpellStudioPreset>();
            preset.vocabulary = vocabulary;
            preset.element = EffectElement.Burst;
            preset.overrideEntry = true;
            preset.overrideColour = true; // This fixture publishes geometry without a palette.
            preset.entry = new ElementEntry
            {
                cycleSeconds = 1.2f,
                motion = EffectMotionKind.Rise,
                parts = new[] { new LookPart { id = "Authored", size = Vector3.one } }
            };
        }

        [TearDown]
        public void TearDown()
        {
            Undo.ClearUndo(vocabulary);
            Object.DestroyImmediate(preset);
            Object.DestroyImmediate(vocabulary);
        }

        [Test]
        public void PublishEntry_CopiesShapeWithoutAliasingPreset()
        {
            Assert.IsTrue(SpellStudioPublishing.PublishEntry(preset));
            var published = vocabulary.elements[EffectElement.Burst];
            Assert.AreEqual(EffectMotionKind.Rise, published.motion);
            Assert.AreEqual(1.2f, published.cycleSeconds);
            Assert.AreNotSame(preset.entry, published);
            preset.entry.parts[0].size = Vector3.zero;
            Assert.AreEqual(Vector3.one, published.parts[0].size);
        }

        [Test]
        public void PublishEntry_DoesNotReplaceOtherElements()
        {
            var other = new ElementEntry { cycleSeconds = 7f };
            vocabulary.elements[EffectElement.Orbit] = other;
            Assert.IsTrue(SpellStudioPublishing.PublishEntry(preset));
            Assert.AreSame(other, vocabulary.elements[EffectElement.Orbit]);
        }

        [Test]
        public void PublishEntry_MissingDestination_ReturnsFalse()
        {
            preset.vocabulary = null;
            Assert.IsFalse(SpellStudioPublishing.PublishEntry(preset));
            Assert.IsFalse(SpellStudioPublishing.PublishEntry(null));
        }

        [Test]
        public void PublishEntry_CanUndoSharedDictionaryChange()
        {
            vocabulary.elements[EffectElement.Burst] = new ElementEntry { cycleSeconds = 8f };
            // Odin's dictionary is stored in its serialized backing data before registering the snapshot.
            ((ISerializationCallbackReceiver)vocabulary).OnBeforeSerialize();
            Undo.IncrementCurrentGroup();
            SpellStudioPublishing.PublishEntry(preset);
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            Assert.AreEqual(8f, vocabulary.elements[EffectElement.Burst].cycleSeconds);
        }
    }
}
