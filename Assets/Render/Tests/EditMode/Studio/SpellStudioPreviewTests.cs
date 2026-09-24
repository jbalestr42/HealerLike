using System;
using System.Collections.Generic;
using System.Linq;
using HealerLike.Render.Grammar;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using HealerLike.Render.Creatures;
using HealerLike.Render.Spells;

using HealerLike.Render.Studio.Editor;

namespace HealerLike.Render.Studio
{
    public sealed class SpellStudioPreviewTests
    {
        SpellStudioPreset _preset;
        SpellStudioPreview _preview;

        static IEnumerable<EffectElement> Elements => Enum.GetValues(typeof(EffectElement)).Cast<EffectElement>();

        [SetUp]
        public void SetUp()
        {
            _preset = ScriptableObject.CreateInstance<SpellStudioPreset>();
            _preset.hideFlags = HideFlags.HideAndDontSave;
            _preset.vocabulary = AssetDatabase.LoadAssetAtPath<EffectVocabulary>("Assets/Render/Spells/Data/EffectVocabulary.asset");
            Assert.That(_preset.vocabulary, Is.Not.Null, "The real renderer vocabulary must be installed.");
            _preview = new SpellStudioPreview();
        }

        [TearDown]
        public void TearDown()
        {
            _preview?.Dispose();
            if (_preset) Object.DestroyImmediate(_preset);
        }

        [TestCaseSource(nameof(Elements))]
        public void SamplesEveryRuntimeElementWithoutGameplayUpdates(EffectElement element)
        {
            _preset.element = element;
            _preset.critical = true;
            SpellEffect effect = _preview.Sample(_preset, _preset.PreviewDuration * 0.35f);
            Assert.That(effect, Is.Not.Null);
            Assert.That(effect.element, Is.EqualTo(element));
            Assert.That(effect.enabled, Is.False, "Timeline sampling must be the only source of time.");
            Assert.That(effect.parts.Count, Is.GreaterThan(0));
            Assert.That(EditorSceneManager.IsPreviewScene(effect.gameObject.scene), Is.True);
            Assert.That(effect.recipe.entry, Is.Not.SameAs(_preset.vocabulary.elements[element]));
            Assert.That(effect.recipe.entry.parts, Is.Not.SameAs(_preset.vocabulary.elements[element].parts));
            foreach (Transform part in effect.parts)
            {
                Assert.That(part.GetComponent<MeshFilter>().sharedMesh, Is.Not.Null, part.name);
                Assert.That(part.GetComponent<Renderer>().sharedMaterial.shader.name, Is.EqualTo("HL/Look/Primitive"));
                AssertFinite(part.position);
                AssertFinite(part.localScale);
            }
        }

        [TestCaseSource(nameof(Elements))]
        public void ReverseSeekMatchesFirstSampleForEveryElement(EffectElement element)
        {
            _preset.element = element;
            _preset.tempo = EffectTempo.ForDuration;
            SpellEffect first = _preview.Sample(_preset, 0.37f);
            Pose[] expected = Snapshot(first);
            _preview.Sample(_preset, 3.2f);
            SpellEffect rewound = _preview.Sample(_preset, 0.37f);
            Assert.That(rewound, Is.Not.SameAs(first), "Rewinding rebuilds the effect rather than applying a negative delta.");
            AssertPose(Snapshot(rewound), expected);
            _preview.Refresh();
            AssertPose(Snapshot(_preview.Sample(_preset, 0.37f)), expected);
        }

        [Test]
        public void PeriodicStatusWaitsForFirstTickAndRewindsToInvisible()
        {
            _preset.element = EffectElement.Rise;
            _preset.tempo = EffectTempo.PerPeriod;
            _preset.periodSeconds = 1f;
            SpellEffect before = _preview.Sample(_preset, 0.4f);
            Assert.That(before.shapes.All(shape => shape.localScale == Vector3.zero), Is.True);
            SpellEffect after = _preview.Sample(_preset, 1.3f);
            Assert.That(after.shapes.Any(shape => shape.gameObject.activeSelf && shape.localScale.sqrMagnitude > 0.001f), Is.True);
            SpellEffect rewound = _preview.Sample(_preset, 0.4f);
            Assert.That(rewound.shapes.All(shape => shape.localScale == Vector3.zero), Is.True);
        }

        [Test]
        public void CleanupRestoresPreviewSceneCountAndLeavesOpenSceneUnchanged()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();
            bool dirty = scene.isDirty;
            int sceneCount = EditorSceneManager.previewSceneCount;
            int rootCount = CountObjects("Spell Studio Preview");
            int materials = Resources.FindObjectsOfTypeAll<Material>().Count(material => material.name.StartsWith("Spell Studio ") && material.name.EndsWith(" Material"));

            for (int i = 0; i < 3; i++)
            {
                using (var preview = new SpellStudioPreview())
                {
                    preview.Sample(_preset, 0.2f);
                    preview.Refresh();
                    preview.Sample(_preset, 0.1f);
                }
            }

            Assert.That(EditorSceneManager.previewSceneCount, Is.EqualTo(sceneCount));
            Assert.That(CountObjects("Spell Studio Preview"), Is.EqualTo(rootCount));
            Assert.That(Resources.FindObjectsOfTypeAll<Material>().Count(material => material.name.StartsWith("Spell Studio ") && material.name.EndsWith(" Material")), Is.EqualTo(materials));
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(scene));
            Assert.That(scene.isDirty, Is.EqualTo(dirty));
            CollectionAssert.AreEquivalent(roots, scene.GetRootGameObjects());
        }

        [Test]
        public void BakedStoneReferenceAutomaticallySelectsStoneUnlessExplicitlyOverridden()
        {
            var reference = ScriptableObject.CreateInstance<HealerLike.Render.Creatures.CreatureRecipe>();
            try
            {
                reference.parts = new[] { new HealerLike.Render.Creatures.CreaturePart
                {
                    primitive = HealerLike.Render.Creatures.Primitive.Stone,
                    role = HealerLike.Render.Creatures.PartRole.Body
                } };
                _preview.ReferenceRecipe = reference;
                Assert.That(_preview.AutomaticTargetSide, Is.True);
                Assert.That(_preview.TargetSide, Is.EqualTo(LookSide.Stone));
                _preview.TargetSide = LookSide.Plant;
                Assert.That(_preview.AutomaticTargetSide, Is.False);
                Assert.That(_preview.TargetSide, Is.EqualTo(LookSide.Plant));
                _preview.AutomaticTargetSide = true;
                Assert.That(_preview.TargetSide, Is.EqualTo(LookSide.Stone));
            }
            finally { Object.DestroyImmediate(reference); }
        }

        [Test]
        public void ReferenceUsesProductionBodyMaterialAndTargetSideColoursEffects()
        {
            _preset.overrideEntry = true;
            _preset.entry = new ElementEntry
            {
                parts = new[]
                {
                    new HealerLike.Render.Creatures.LookPart
                    {
                        id = "Body colour", primitive = HealerLike.Render.Creatures.Primitive.Sphere,
                        role = HealerLike.Render.Creatures.PartRole.Body, colour = ColourRole.Body,
                        size = Vector3.one * .2f
                    }
                },
                sideRim = new[]
                {
                    new HealerLike.Render.Creatures.LookPart
                    {
                        id = "Caster rim", primitive = HealerLike.Render.Creatures.Primitive.Sphere,
                        role = HealerLike.Render.Creatures.PartRole.Body, colour = ColourRole.Accent,
                        size = Vector3.one * .1f
                    }
                },
                cycleSeconds = 1f
            };
            _preset.side = Entity.EntityType.Player;
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            Material sourceBody = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Body.mat");
            Material sourceStone = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Stone.mat");
            SpellEffect plant = _preview.Sample(_preset, .2f);
            var rig = (HealerLike.Render.Creatures.CreatureRig)typeof(SpellStudioPreview).GetField("_rig", flags).GetValue(_preview);
            bool foundBody = false;
            for (int i = 0; i < rig.partTransforms.Count; i++)
            {
                Material material = rig.partTransforms[i].GetComponent<Renderer>().sharedMaterial;
                if (rig.recipe.parts[i].role == HealerLike.Render.Creatures.PartRole.Body)
                {
                    foundBody = true;
                    Assert.That(material.GetFloat("_HLToonThresholdOffset"), Is.EqualTo(sourceBody.GetFloat("_HLToonThresholdOffset")));
                    Assert.That(material.GetColor("_HLShadeTint"), Is.EqualTo(sourceBody.GetColor("_HLShadeTint")));
                }
                else Assert.That(material.name, Is.EqualTo("Spell Studio Preview Material"));
            }
            Assert.That(foundBody, Is.True);
            var block = new MaterialPropertyBlock();
            plant.parts[0].GetComponent<Renderer>().GetPropertyBlock(block);
            Color plantColour = block.GetColor("_BaseColor");
            Assert.That(Vector4.Distance(plantColour, _preset.vocabulary.palette.Colour(ColourRole.Body, _preset.family, LookSide.Plant)), Is.LessThan(.0001f));
            _preview.TargetSide = LookSide.Stone;
            SpellEffect stone = _preview.Sample(_preset, .2f);
            stone.parts[0].GetComponent<Renderer>().GetPropertyBlock(block);
            Color stoneColour = block.GetColor("_BaseColor");
            Assert.That(Vector4.Distance(stoneColour, _preset.vocabulary.palette.Colour(ColourRole.Body, _preset.family, LookSide.Stone)), Is.LessThan(.0001f));
            Assert.That(Vector4.Distance(plantColour, stoneColour), Is.GreaterThan(.05f), "Changing target side must change the body's palette colour.");
            stone.parts.Find(part => part.name == "Caster rim").GetComponent<Renderer>().GetPropertyBlock(block);
            Assert.That(Vector4.Distance(block.GetColor("_BaseColor"), _preset.vocabulary.palette.Colour(ColourRole.Rim, _preset.family, LookSide.Plant)), Is.LessThan(.0001f), "Caster rim stays the player's colour on a stone target.");
            rig = (HealerLike.Render.Creatures.CreatureRig)typeof(SpellStudioPreview).GetField("_rig", flags).GetValue(_preview);
            foreach (Transform part in rig.partTransforms)
            {
                Material material = part.GetComponent<Renderer>().sharedMaterial;
                Assert.That(material.name, Is.EqualTo("Spell Studio Stone Material"));
                Assert.That(material.shader, Is.EqualTo(sourceStone.shader));
            }
        }

        [Test]
        public void CaptureRendersSpellPixelsIntoCallerOwnedTexture()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Image verification requires a graphics device.");
            _preset.element = EffectElement.Rise;
            _preset.family = EffectFamily.Heal;
            _preview.ShowGround = false;
            _preview.ShowReference = false;
            Texture2D capture = null;
            try
            {
                capture = _preview.Capture(_preset, _preset.PreviewDuration * 0.4f, 320, 240);
                Assert.That(capture, Is.Not.Null);
                Assert.That(capture.width, Is.EqualTo(320));
                Assert.That(capture.height, Is.EqualTo(240));
                Color32[] pixels = capture.GetPixels32();
                Color32 background = pixels[0];
                int different = pixels.Count(pixel => System.Math.Abs(pixel.r - background.r) +
                    System.Math.Abs(pixel.g - background.g) + System.Math.Abs(pixel.b - background.b) > 25);
                Assert.That(different, Is.GreaterThan(20), "The isolated spell must produce visible pixels through the URP shader.");
                _preview.Dispose();
                Assert.That(capture != null, Is.True, "The exported texture belongs to the caller after preview disposal.");
            }
            finally
            {
                if (capture) Object.DestroyImmediate(capture);
            }
        }

        [Test]
        public void CaptureRestoresShaderGlobalsAfterStageCallbackAndFitsPortrait()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Image verification requires a graphics device.");
            string[] names = { "_HLLookApplied", "_HLInkStrength", "_HLFogStart", "_HLFogEnd" };
            float[] before = names.Select(Shader.GetGlobalFloat).ToArray();
            Vector4 tint = Shader.GetGlobalVector("_HLShadowTint");
            Texture2D capture = null;
            void StageCallback(UnityEngine.Rendering.ScriptableRenderContext context, Camera[] cameras)
            {
                Shader.SetGlobalFloat("_HLInkStrength", .987f);
                Shader.SetGlobalFloat("_HLFogStart", .01f);
            }
            try
            {
                UnityEngine.Rendering.RenderPipelineManager.beginFrameRendering += StageCallback;
                capture = _preview.Capture(_preset, .2f, 480, 700);
                for (int i = 0; i < names.Length; i++) Assert.That(Shader.GetGlobalFloat(names[i]), Is.EqualTo(before[i]), names[i]);
                Assert.That(Shader.GetGlobalVector("_HLShadowTint"), Is.EqualTo(tint));
                Color32[] pixels = capture.GetPixels32();
                Color32 background = pixels[pixels.Length - 1];
                // The top row is clear background: the complete creature crown fits with breathing room.
                int upperBandSubject = 0;
                for (int y = 690; y < 700; y++)
                    for (int x = 100; x < 380; x++)
                    {
                        Color32 pixel = pixels[y * 480 + x];
                        if (System.Math.Abs(pixel.r - background.r) + System.Math.Abs(pixel.g - background.g) +
                            System.Math.Abs(pixel.b - background.b) > 35) upperBandSubject++;
                    }
                Assert.That(upperBandSubject, Is.EqualTo(0), "The crown must not intersect the upper frame.");
                int greenPixels = pixels.Count(pixel => pixel.g > pixel.r * 1.15f && pixel.g > pixel.b * 1.2f && pixel.g > 65);
                Assert.That(greenPixels, Is.GreaterThan(100), "The healer must retain its authored greens under preview lighting.");
            }
            finally
            {
                UnityEngine.Rendering.RenderPipelineManager.beginFrameRendering -= StageCallback;
                for (int i = 0; i < names.Length; i++) Shader.SetGlobalFloat(names[i], before[i]);
                Shader.SetGlobalVector("_HLShadowTint", tint);
                if (capture) Object.DestroyImmediate(capture);
            }
        }

        [Test]
        public void CapturePreservesEditedFramingAndRestoresExactCameraPose()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Image verification requires a graphics device.");
            _preview.Sample(_preset, .2f);
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            Type type = typeof(SpellStudioPreview);
            type.GetMethod("ConfigureCamera", flags).Invoke(_preview, new object[] { 600f, 600f, false });
            Vector3 editedTarget = new Vector3(.6f, 1.2f, -.2f);
            type.GetField("_target", flags).SetValue(_preview, editedTarget);
            type.GetField("_distance", flags).SetValue(_preview, 7.25f);
            type.GetMethod("ConfigureCamera", flags).Invoke(_preview, new object[] { 600f, 600f, false });
            var utility = (PreviewRenderUtility)type.GetField("_preview", flags).GetValue(_preview);
            Camera camera = utility.camera;
            Vector3 position = camera.transform.position;
            Quaternion rotation = camera.transform.rotation;
            float aspect = camera.aspect, fieldOfView = camera.fieldOfView;
            Vector3 renderPosition = Vector3.zero;
            void Observe(UnityEngine.Rendering.ScriptableRenderContext context, Camera rendered)
            { if (rendered == camera) renderPosition = rendered.transform.position; }
            Texture2D capture = null;
            try
            {
                UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += Observe;
                capture = _preview.Capture(_preset, .2f, 640, 360);
                Assert.That(Vector3.Distance(renderPosition, position), Is.LessThan(.0001f), "Export uses the user's zoom and pan.");
                Assert.That(camera.transform.position, Is.EqualTo(position));
                Assert.That(camera.transform.rotation, Is.EqualTo(rotation));
                Assert.That(camera.aspect, Is.EqualTo(aspect));
                Assert.That(camera.fieldOfView, Is.EqualTo(fieldOfView));
                Assert.That((Vector3)type.GetField("_target", flags).GetValue(_preview), Is.EqualTo(editedTarget));
                Assert.That((float)type.GetField("_distance", flags).GetValue(_preview), Is.EqualTo(7.25f));
                Assert.That((float)type.GetField("_frameAspect", flags).GetValue(_preview), Is.EqualTo(1f));
                Assert.That((bool)type.GetField("_fitRequested", flags).GetValue(_preview), Is.False);
            }
            finally
            {
                UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= Observe;
                if (capture) Object.DestroyImmediate(capture);
            }
        }

        [Test]
        public void NullSelectionClearsPreviousEffectAndDisposalIsIdempotent()
        {
            SpellEffect previous = _preview.Sample(_preset, 0.1f);
            Assert.That(_preview.Sample(null, 0f), Is.Null);
            Assert.That(previous == null, Is.True);
            _preview.Dispose();
            Assert.DoesNotThrow(() => _preview.Dispose());
            Assert.Throws<ObjectDisposedException>(() => _preview.Sample(_preset, 0f));
        }

        static int CountObjects(string name) => Resources.FindObjectsOfTypeAll<GameObject>().Count(go => go.name == name);

        readonly struct Pose
        {
            public readonly Vector3 position, scale;
            public readonly Quaternion rotation;
            public readonly bool active;
            public Pose(Transform transform)
            {
                position = transform.position;
                rotation = transform.rotation;
                scale = transform.localScale;
                active = transform.gameObject.activeSelf;
            }
        }

        static Pose[] Snapshot(SpellEffect effect) => effect.parts.Select(part => new Pose(part)).ToArray();

        static void AssertPose(Pose[] actual, Pose[] expected)
        {
            Assert.That(actual.Length, Is.EqualTo(expected.Length));
            for (int i = 0; i < actual.Length; i++)
            {
                Assert.That(Vector3.Distance(actual[i].position, expected[i].position), Is.LessThan(0.0001f), "Position " + i);
                Assert.That(Quaternion.Angle(actual[i].rotation, expected[i].rotation), Is.LessThan(0.01f), "Rotation " + i);
                Assert.That(Vector3.Distance(actual[i].scale, expected[i].scale), Is.LessThan(0.0001f), "Scale " + i);
                Assert.That(actual[i].active, Is.EqualTo(expected[i].active), "Visibility " + i);
            }
        }

        static void AssertFinite(Vector3 vector)
        {
            Assert.That(float.IsFinite(vector.x) && float.IsFinite(vector.y) && float.IsFinite(vector.z), Is.True);
        }
    }
}
