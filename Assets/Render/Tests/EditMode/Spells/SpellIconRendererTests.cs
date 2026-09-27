using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HealerLike.Render.Spells
{
    public class SpellIconRendererTests
    {
        static EffectRecipe Layer(EffectElement element = EffectElement.Rise)
        {
            return EffectComposer.Compose(RenderTestAssets.LoadEffectVocabulary(), element, EffectFamily.Heal,
                EffectTempo.Once, 0, 3, 3, .5f);
        }

        [Test]
        public void SubjectPosesPrivateCopiesWithoutChangingTheAuthoredRecipe()
        {
            EffectRecipe layer = Layer();
            string before = JsonUtility.ToJson(layer.entry);
            SpellIconRecipe recipe = new SpellIconRecipe();
            recipe.layers.Add(layer);
            using (SpellIconSubject subject = new SpellIconSubject(recipe, RenderTestAssets.LoadMeshes(),
                RenderTestAssets.LoadLookMaterial(), Vector3.zero))
            {
                SpellEffect effect = subject.root.GetComponentInChildren<SpellEffect>();
                Assert.That(effect.recipe, Is.Not.SameAs(layer));
                Assert.That(effect.recipe.entry.parts, Is.Not.SameAs(layer.entry.parts));
                Assert.That(effect.recipe.entry.presentation, Is.Not.SameAs(layer.entry.presentation));
                effect.recipe.entry.parts[0].position = Vector3.one * 100;
                Assert.That(JsonUtility.ToJson(layer.entry), Is.EqualTo(before));
            }
        }

        [Test]
        public void CompoundIconNormalizesIndependentGlyphsAndShowsOverflow()
        {
            SpellIconRecipe recipe = new SpellIconRecipe();
            for (int i = 0; i < 5; i++)
            {
                recipe.layers.Add(Layer());
            }
            using (SpellIconSubject subject = new SpellIconSubject(recipe, RenderTestAssets.LoadMeshes(),
                RenderTestAssets.LoadLookMaterial(), Vector3.zero))
            {
                Assert.That(subject.root.GetComponentsInChildren<SpellEffect>().Length, Is.EqualTo(4));
                Assert.That(subject.root.transform.Find("More effects horizontal"), Is.Not.Null);
                Assert.That(subject.root.transform.Find("Additional effect 0"), Is.Not.Null);
                foreach (SpellEffect effect in subject.root.GetComponentsInChildren<SpellEffect>())
                {
                    foreach (Renderer part in effect.GetComponentsInChildren<Renderer>())
                    {
                        Assert.That(part.bounds.min.x, Is.GreaterThan(-1.1f));
                        Assert.That(part.bounds.max.x, Is.LessThan(1.1f));
                        Assert.That(part.bounds.min.y, Is.GreaterThan(-1.1f));
                        Assert.That(part.bounds.max.y, Is.LessThan(1.1f));
                    }
                }
            }
        }

        [Test]
        public void DisposeReleasesEveryOwnedProceduralMeshAndTheSubject()
        {
            SpellIconRecipe recipe = new SpellIconRecipe();
            recipe.layers.Add(Layer(EffectElement.Burst));
            SpellIconSubject subject = new SpellIconSubject(recipe, RenderTestAssets.LoadMeshes(),
                RenderTestAssets.LoadLookMaterial(), Vector3.zero);
            HashSet<Mesh> meshes = new HashSet<Mesh>();
            foreach (MeshFilter filter in subject.root.GetComponentsInChildren<MeshFilter>(true))
            {
                meshes.Add(filter.sharedMesh);
            }
            subject.Dispose();
            subject.Dispose();
            Assert.That(subject.root == null, Is.True);
            foreach (Mesh mesh in meshes)
            {
                Assert.That(mesh == null, Is.True, "An icon-owned procedural mesh was retained.");
            }
        }

        [Test]
        public void NestedPeriodicPieceRemainsInsideItsGlyphAndContributesTempoMarks()
        {
            EffectRecipe core = Layer(EffectElement.Burst);
            EffectRecipe child = Layer(EffectElement.Stalks);
            child.tempo = EffectTempo.PerPeriod;
            core.additions = new[] { child };
            SpellIconRecipe recipe = new SpellIconRecipe();
            recipe.layers.Add(core);
            using (SpellIconSubject subject = new SpellIconSubject(recipe, RenderTestAssets.LoadMeshes(),
                RenderTestAssets.LoadLookMaterial(), Vector3.zero))
            {
                Transform glyph = subject.root.transform.Find("Spell glyph 0");
                Assert.AreEqual(2, glyph.GetComponentsInChildren<SpellEffect>().Length);
                Assert.IsNull(subject.root.transform.Find("Spell glyph 1"));
                Assert.IsNotNull(subject.root.transform.Find("Periodic beat 2"));
                Assert.AreEqual(1, core.additions.Length);
                foreach (Renderer part in glyph.GetComponentsInChildren<Renderer>())
                {
                    Assert.LessOrEqual(part.bounds.max.x, 1.1f);
                    Assert.GreaterOrEqual(part.bounds.min.x, -1.1f);
                }
            }
        }

        [Test]
        public void CaptureCameraIsTransparentAndDoesNotRunPostProcessing()
        {
            using (SpellIconRenderer renderer = new SpellIconRenderer(null, null))
            {
                TestHelpers.InvokePrivate(renderer, "InitCamera");
                Camera camera = (Camera)typeof(SpellIconRenderer).GetField("_camera",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(renderer);
                Assert.That(camera.enabled, Is.False);
                Assert.That(camera.backgroundColor, Is.EqualTo(Color.clear));
                Assert.That(camera.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing, Is.False);
                Assert.That(camera.GetComponent<UniversalAdditionalCameraData>().renderShadows, Is.False);
                Assert.That(renderer.Capture(new SpellIconRecipe()), Is.Null);
            }
        }

        [Test]
        public void ValidationRejectsCyclicAndAggregateOverBudgetInputsBeforeBuilding()
        {
            EffectRecipe layer = Layer();
            SpellIconRecipe cyclic = new SpellIconRecipe();
            cyclic.layers.Add(layer);
            layer.additions = new[] { layer };
            Assert.That(SpellIconSubject.IsValid(cyclic), Is.False);
            layer.additions = System.Array.Empty<EffectRecipe>();
            SpellIconRecipe crowded = new SpellIconRecipe();
            for (int i = 0; i < 100; i++)
            {
                crowded.layers.Add(layer);
            }
            Assert.That(SpellIconSubject.IsValid(crowded), Is.False);
        }

        [Test]
        public void GrammarMarksShowGroupReachAndPeriodicTempo()
        {
            EffectRecipe layer = Layer(EffectElement.Stalks);
            layer.tempo = EffectTempo.PerPeriod;
            SpellIconRecipe recipe = new SpellIconRecipe { reach = EffectReach.Group };
            recipe.layers.Add(layer);
            using (SpellIconSubject subject = new SpellIconSubject(recipe, RenderTestAssets.LoadMeshes(),
                RenderTestAssets.LoadLookMaterial(), Vector3.zero))
            {
                Assert.That(subject.root.transform.Find("Reach target 2"), Is.Not.Null);
                Assert.That(subject.root.transform.Find("Periodic beat 2"), Is.Not.Null);
                Assert.That(subject.root.transform.Find("Tempo inner rim"), Is.Not.Null);
            }
        }
    }
}
