using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Stage
{
    public static class FieldVariantCapture
    {
        [MenuItem("Tools/Render/Capture Field Variants")]
        public static void Capture() { SpellSourcePreparation.Enter(FieldVariantRun.Mode, 1800f); }
    }

    // One paused moment of the real Main battle with its Toolkit HUD, at 1080x1920 portrait, under each field
    // treatment of GrassBlade.mat: the moment with no effect, then Burst at its readable peak on a plant ally and on a
    // stone enemy. The battle frames are measured on the real board; the grass lab readability pass then runs under
    // each treatment for the per-element numbers. The material is restored after every treatment and never saved.
    public class FieldVariantRun : AStageRun
    {
        public const string Mode = "field-variants";
        const string BladeMaterialPath = "Assets/Render/Grass/Materials/GrassBlade.mat";
        const int Width = 1080;
        const int Height = 1920;
        const int RingPixels = 24;
        // Treatment 5's strength of treatments 1, 2 and 3 together; FIELD_COMBINATION_STRENGTH overrides it
        const float DefaultCombination = .8f;

        [Serializable]
        public class BattleTarget
        {
            public string target;
            public string entity;
            public string burstMaterial;
            public int creaturePixels;
            public double silhouetteSurvival;
            public double creatureLuma, creatureFieldLuma, creatureLumaDifference, creatureRgbDistance;
            public int burstPixels;
            public double burstLuma, burstFieldLuma, burstLumaDifference, burstRgbDistance;
        }

        [Serializable]
        public class Treatment
        {
            public int index;
            public string name;
            public string baseColor;
            public float valueScale, saturationScale, contrastScale, hue;
            public string[] frames;
            public List<BattleTarget> battle = new List<BattleTarget>();
            public List<SpellPolishImages.ReadabilityRow> readability = new List<SpellPolishImages.ReadabilityRow>();
            public List<SpellPolishImages.PairRow> pairs = new List<SpellPolishImages.PairRow>();
        }

        [Serializable]
        public class Sheet
        {
            public string revision = StagePlay.ReadRevision();
            public string unityVersion = Application.unityVersion;
            public string source = BladeMaterialPath + " (_BaseColor, _HLShadeTint, _HLShadeTurnTint, _HLHatchMultiplier)";
            public string condition = "Real Main battle and Toolkit HUD, paused; Burst composed and placed on the real "
                + "creature's anchors by the harness at 45% of its cycle, not a gameplay cast. Readability rows are the "
                + "isolated grass lab. The floor under the blades is StageGround.mat and is not treated.";
            public string qualityLevel;
            // QualitySettings.antiAliasing as the session reads it, and the MSAA of the pipeline asset the stage draws
            // with, which is the one URP honours
            public int antiAliasing;
            public string pipeline;
            public int authoredMsaa;
            public int capturedMsaa;
            public float bladeHeightScale;
            public float combinationStrength;
            public bool passed;
            public List<Treatment> treatments = new List<Treatment>();
        }

        readonly Sheet _sheet = new Sheet();
        StageInterfaceOutput _output;
        StageCaptureSession _session;

        public static string Folder
        {
            get
            {
                string folder = System.Environment.GetEnvironmentVariable("FIELD_VARIANTS_DIR");
                return string.IsNullOrEmpty(folder)
                    ? Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs", "FieldVariants")
                    : folder;
            }
        }

        public static float CombinationStrength
        {
            get
            {
                string value = System.Environment.GetEnvironmentVariable("FIELD_COMBINATION_STRENGTH");
                return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float strength)
                    ? Mathf.Clamp01(strength) : DefaultCombination;
            }
        }

        protected override void OnFailed(Exception error)
        {
            _output?.Fail(error.ToString());
            base.OnFailed(error);
        }

        protected override IEnumerator Run()
        {
            string folder = Folder;
            Directory.CreateDirectory(folder);
            _output = new StageInterfaceOutput(folder);
            _session = new StageCaptureSession(_manager, _output);
            Material blade = RenderAssets.Load<Material>(BladeMaterialPath);
            UniversalRenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            int msaa = pipeline ? pipeline.msaaSampleCount : 0;
            BattleFocus focus = null;
            bool focusEnabled = false;
            _sheet.combinationStrength = CombinationStrength;
            FieldTreatment[] treatments = FieldTreatment.Sheet(_sheet.combinationStrength);
            try
            {
                _sheet.qualityLevel = QualitySettings.names[QualitySettings.GetQualityLevel()];
                _sheet.antiAliasing = QualitySettings.antiAliasing;
                _sheet.pipeline = pipeline ? pipeline.name : "none";
                _sheet.authoredMsaa = msaa;
                // Every treatment at 2x MSAA, set on the pipeline in memory and restored, never saved
                if (pipeline) pipeline.msaaSampleCount = 2;
                _sheet.capturedMsaa = pipeline ? pipeline.msaaSampleCount : 0;
                _sheet.bladeHeightScale = _manager.grass.bladeHeightScale;
                _session.AttachInput();
                _manager.SetLandscape(false);
                yield return _session.Resize(Width, Height);
                _player.PlaceAllies(_manager, StagePlayer.LoadAllies());
                yield return Wait(0.5f);
                _hud.nextWaveButton.onClick.Invoke();
                yield return Wait(1f);
                focus = Object.FindAnyObjectByType<BattleFocus>();
                focusEnabled = focus && focus.enabled;
                if (focus) focus.enabled = false;
                Time.timeScale = 0f;
                yield return Wait(0.3f);
                CreatureBuilder plant = Target(Entity.EntityType.Player);
                CreatureBuilder stone = Target(Entity.EntityType.Computer);
                _output.Check(plant && stone, "The paused battle has a plant ally and a stone enemy on the board");
                for (int i = 0; i < treatments.Length; i++)
                {
                    FieldTreatment treatment = treatments[i];
                    FieldTreatment.Snapshot authored = treatment.Apply(blade);
                    Treatment record = new Treatment
                    {
                        index = i, name = treatment.name, valueScale = treatment.valueScale,
                        saturationScale = treatment.saturationScale, contrastScale = treatment.contrastScale,
                        hue = treatment.hue, baseColor = "#" + ColorUtility.ToHtmlStringRGB(blade.GetColor("_BaseColor"))
                    };
                    _sheet.treatments.Add(record);
                    try
                    {
                        string prefix = i + "-" + treatment.name;
                        record.frames = new[] { prefix + "-a-no-effect.png", prefix + "-b-burst-on-plant.png",
                            prefix + "-c-burst-on-stone.png" };
                        yield return Wait(0.2f);
                        yield return _session.Capture(prefix + "-a-no-effect", "Paused real battle, field treatment");
                        yield return Burst(plant, LookSide.Plant, prefix + "-b-burst-on-plant", record);
                        yield return Burst(stone, LookSide.Stone, prefix + "-c-burst-on-stone", record);
                    }
                    finally { authored.Restore(blade); }
                }
                // The lab pass takes the board's publications for itself, so it runs after every battle frame
                EffectVocabulary vocabulary = RenderAssets.Load<EffectVocabulary>(
                    "Assets/Render/Spells/Data/EffectVocabulary.asset");
                Material look = RenderAssets.Load<Material>("Assets/Render/Look/Look_Default.mat");
                var fixtures = new List<SpellReadabilityPass.Fixture>();
                foreach (LookSide target in new[] { LookSide.Plant, LookSide.Stone })
                    foreach (EffectKey element in SpellPolishRun.CoreElements)
                        fixtures.Add(new SpellReadabilityPass.Fixture(element, LookSide.Plant, target));
                for (int i = 0; i < treatments.Length; i++)
                {
                    FieldTreatment.Snapshot authored = treatments[i].Apply(blade);
                    try
                    {
                        string lab = Path.Combine(folder, i + "-" + treatments[i].name + "-lab");
                        Directory.CreateDirectory(lab);
                        using (var images = new SpellPolishImages(lab))
                        {
                            yield return SpellReadabilityPass.Run(_manager, images, vocabulary, look, fixtures);
                            images.WriteReadability();
                            _sheet.treatments[i].readability.AddRange(images.rows);
                            _sheet.treatments[i].pairs.AddRange(images.pairRows);
                        }
                    }
                    finally { authored.Restore(blade); }
                }
                _sheet.passed = _sheet.treatments.Count == treatments.Length
                    && _sheet.treatments.All(t => t.battle.Count == 2 && t.readability.Count == fixtures.Count
                        && t.pairs.Count == SpellReadabilityPass.Pairs.Length);
            }
            finally
            {
                if (pipeline)
                {
                    pipeline.msaaSampleCount = msaa;
                    EditorUtility.ClearDirty(pipeline);
                }
                EditorUtility.ClearDirty(blade);
                if (focus) focus.enabled = focusEnabled;
                _session.Dispose();
                _output.Write(_sheet.passed);
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "field-variants.json"), JsonUtility.ToJson(_sheet, true));
                Debug.Log("[FieldVariantRun] Wrote " + _sheet.treatments.Count + " treatments to " + folder);
                StagePlay.Finish(this, _sheet.passed);
            }
        }

        // The creature of that side nearest the board's centre, so its effect stays inside the frame
        CreatureBuilder Target(Entity.EntityType side)
        {
            Vector3 centre = _manager.gameCamera.ViewportToWorldPoint(new Vector3(.5f, .5f, 10f));
            return _manager.entityManager.GetEntities(side)
                .Select(g => g.GetComponentInChildren<CreatureBuilder>())
                .Where(host => host && host.rig != null)
                .OrderBy(host => Vector3.Distance(Vector3.ProjectOnPlane(host.transform.position - centre, Vector3.up),
                    Vector3.zero))
                .FirstOrDefault();
        }

        // Burst as the opposing side draws it on this target, at its readable peak, captured with the HUD and measured
        // on the game camera: the target's own pixels with and without the effect, and the field behind both
        IEnumerator Burst(CreatureBuilder host, LookSide target, string name, Treatment record)
        {
            LookSide caster = target == LookSide.Plant ? LookSide.Stone : LookSide.Plant;
            EffectVocabulary vocabulary = RenderAssets.Load<EffectVocabulary>(
                "Assets/Render/Spells/Data/EffectVocabulary.asset");
            EffectRecipe recipe = SpellReadabilityPass.Compose(vocabulary, EffectKey.Burst, caster);
            GameObject root = new GameObject("Field variant Burst");
            try
            {
                SpellEffect effect = root.AddComponent<SpellEffect>();
                effect.enabled = false;
                effect.Init(recipe, _manager.meshes, RenderAssets.Load<Material>("Assets/Render/Look/Look_Default.mat"),
                    target);
                if (!host.rig.TryGetAnchors(out EffectAnchors anchors))
                    throw new InvalidOperationException("Target creature has no effect anchors.");
                EffectPlacement.Place(effect, null, anchors);
                EffectPlacement.FaceCamera(effect, _manager.gameCamera);
                effect.SetSide(caster == LookSide.Plant ? Entity.EntityType.Player : Entity.EntityType.Computer);
                foreach (Transform part in root.GetComponentsInChildren<Transform>(true))
                    part.gameObject.layer = host.gameObject.layer;
                float peak = SpellReadabilityPass.PeakPhase * recipe.cycleSeconds;
                for (float age = SpellPolishRun.FrameStep; age <= peak; age += SpellPolishRun.FrameStep)
                    effect.Advance(SpellPolishRun.FrameStep);
                yield return Wait(0.2f);
                yield return _session.Capture(name, "Paused real battle, harness Burst at readable peak");
                record.battle.Add(Measure(host, root, target, caster, record.index == 0 ? name : null));
            }
            finally { Object.Destroy(root); }
        }

        // The control's game camera readbacks are kept beside the HUD frames, to check by eye what was measured
        BattleTarget Measure(CreatureBuilder host, GameObject effectRoot, LookSide target, LookSide caster, string keep)
        {
            Camera camera = _manager.gameCamera;
            // The rig is assembled under its own root, not under the builder
            Renderer[] effect = Visible(effectRoot), body = Visible(host.rig.root.gameObject);
            Texture2D full = StageReadback.Render(camera, Width, Height);
            Texture2D bare = null, field = null;
            try
            {
                Show(effect, false);
                bare = StageReadback.Render(camera, Width, Height);
                Show(body, false);
                field = StageReadback.Render(camera, Width, Height);
                Show(effect, true);
                Show(body, true);
                if (keep != null)
                {
                    File.WriteAllBytes(Path.Combine(Folder, keep + "-readback-full.png"), full.EncodeToPNG());
                    File.WriteAllBytes(Path.Combine(Folder, keep + "-readback-field.png"), field.EncodeToPNG());
                }
                Color32[] a = full.GetPixels32(), b = bare.GetPixels32(), c = field.GetPixels32();
                bool[] creature = ReadabilityPixels.Changed(b, c);
                ReadabilityPixels.Contrast shape = ReadabilityPixels.Measure(creature, b, c, Width, Height, RingPixels);
                ReadabilityPixels.Contrast burst = ReadabilityPixels.Measure(ReadabilityPixels.Changed(a, b), a, c,
                    Width, Height, RingPixels);
                return new BattleTarget
                {
                    target = target.ToString(), entity = host.GetComponentInParent<Entity>().name, burstMaterial = caster.ToString(),
                    creaturePixels = shape.pixels, silhouetteSurvival = ReadabilityPixels.Survival(creature, a, b),
                    creatureLuma = shape.shapeLuma, creatureFieldLuma = shape.fieldLuma,
                    creatureLumaDifference = shape.lumaDifference, creatureRgbDistance = shape.rgbDistance,
                    burstPixels = burst.pixels, burstLuma = burst.shapeLuma, burstFieldLuma = burst.fieldLuma,
                    burstLumaDifference = burst.lumaDifference, burstRgbDistance = burst.rgbDistance
                };
            }
            finally
            {
                Show(effect, true);
                Show(body, true);
                RenderObjects.Release(full);
                RenderObjects.Release(bare);
                RenderObjects.Release(field);
            }
        }

        static Renderer[] Visible(GameObject root)
        {
            return Array.FindAll(root.GetComponentsInChildren<Renderer>(false), renderer => renderer.enabled);
        }

        static void Show(Renderer[] renderers, bool isShown)
        {
            foreach (Renderer renderer in renderers) renderer.enabled = isShown;
        }
    }
}
