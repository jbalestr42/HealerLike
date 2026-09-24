using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Spells.Studio;
using HealerLike.Render.Spells.Editor.Studio;

namespace HealerLike.Render.Spells.Editor.Tests
{
    public class SpellStudioCreatureReferenceTests
    {
        [Test]
        public void ChangingReferenceRebuildsCreatureAndPlacesEffectAtItsBody()
        {
            var creature = ScriptableObject.CreateInstance<CreatureRecipe>();
            var spell = ScriptableObject.CreateInstance<SpellStudioPreset>();
            creature.parts = new[] { new CreaturePart
            {
                id = "Authored Body", parent = -1, primitive = Primitive.Sphere, role = PartRole.Body,
                localPosition = Vector3.up * 2f, dimensions = Vector3.one, colour = Color.cyan
            }};
            creature.neckLocal = Vector3.up * 2.5f;
            spell.vocabulary = AssetDatabase.LoadAssetAtPath<EffectVocabulary>("Assets/Render/Spells/Data/EffectVocabulary.asset");
            spell.element = EffectElement.Burst;
            try
            {
                using (var preview = new SpellStudioPreview())
                {
                    var original = preview.Sample(spell, .1f);
                    float originalHeight = original.transform.position.y;
                    preview.ReferenceRecipe = creature;
                    var authored = preview.Sample(spell, .1f);
                    Assert.That(original == null, Is.True);
                    Assert.That(authored.transform.position.y, Is.EqualTo(2f).Within(.1f));
                    Assert.That(creature.parts[0].localPosition, Is.EqualTo(Vector3.up * 2f));
                    preview.ReferenceRecipe = null;
                    var restored = preview.Sample(spell, .1f);
                    Assert.That(restored.transform.position.y, Is.EqualTo(originalHeight).Within(.001f));
                }
            }
            finally { Object.DestroyImmediate(creature); Object.DestroyImmediate(spell); }
        }

        [Test]
        public void InvalidReferenceDoesNotSendMalformedDataToRuntimeRig()
        {
            var creature = ScriptableObject.CreateInstance<CreatureRecipe>();
            var spell = ScriptableObject.CreateInstance<SpellStudioPreset>();
            spell.vocabulary = AssetDatabase.LoadAssetAtPath<EffectVocabulary>("Assets/Render/Spells/Data/EffectVocabulary.asset");
            try
            {
                using (var preview = new SpellStudioPreview { ReferenceRecipe = creature })
                {
                    Assert.DoesNotThrow(() => preview.Sample(spell, .1f));
                    Assert.Throws<System.InvalidOperationException>(() => preview.Capture(spell, .1f, 200, 200));
                }
            }
            finally { Object.DestroyImmediate(creature); Object.DestroyImmediate(spell); }
        }
    }
}
