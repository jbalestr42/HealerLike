using System;
using System.Collections;
using System.IO;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    public static class SpellPolishCapture
    {
        [MenuItem("Tools/Render/Capture Polished Spells on Grass")]
        public static void All() { StagePlay.Enter("spell-polish", 600f); }
    }

    // Presentation fixtures, not gameplay casts: all authored entries run through the real parts and grass paths.
    public class SpellPolishRun : AStageRun
    {
        const float Step = 1f / 60f;
        static readonly float[] Phases = { .15f, .45f, .8f };
        protected override bool shouldStartGame => false;

        protected override IEnumerator Run()
        {
            string folder = Path.Combine(StagePlay.CaptureFolder, "spell-polish");
            Directory.CreateDirectory(folder);
            EffectVocabulary vocabulary = RenderAssets.Load<EffectVocabulary>(
                "Assets/Render/Spells/Data/EffectVocabulary.asset");
            Material material = RenderAssets.Load<Material>("Assets/Render/Look/Look_Default.mat");
            using (var images = new SpellPolishImages(folder))
            {
                foreach (EffectKey element in Enum.GetValues(typeof(EffectKey)))
                {
                    using (var scene = new GrassLabScene(_manager))
                    {
                        if (!scene.Init()) throw new InvalidOperationException("Could not initialize grass spell fixture.");
                        Prepare(scene);
                        for (int frame = 0; frame < 30; frame++) Tick(scene, Step, frame * Step);
                        EffectRecipe recipe = EffectComposer.Compose(vocabulary, element, Family(element),
                            EffectTempo.Once, 0f, 3, 3, .5f);
                        SpellEffect effect = Build(scene, recipe, material);
                        ShowGround(scene, recipe);
                        int sample = 0;
                        int total = Mathf.CeilToInt(recipe.cycleSeconds / Step);
                        for (int frame = 0; frame <= total && sample < Phases.Length; frame++)
                        {
                            float age = frame * Step;
                            if (frame > 0) effect.Advance(Step);
                            Tick(scene, Step, .5f + age);
                            if (age >= Phases[sample] * recipe.cycleSeconds)
                            {
                                images.Capture(scene.camera, element, sample, age);
                                images.Measure(scene.field.simulation, element, sample, age);
                                sample++;
                            }
                            yield return null;
                        }
                    }
                    yield return null;
                }
                images.Write();
            }
            File.WriteAllText(Path.Combine(folder, "README.txt"),
                "Native Unity presentation fixtures, not gameplay casts.\n" +
                "All 14 vocabulary entries at 15%, 45%, 80% of their authored cycle.\n" +
                "Fixed 60 Hz motion and GPU grass simulation, identical camera and authored board lighting/grass height.\n" +
                "ground-metrics.csv records raw GPU motion and colour-state extrema for every frame.\n" +
                "Every fixture uses the saved composition, real creature rig, and its authored ground reaction.\n");
            Debug.Log("[SpellPolishRun] Wrote 42 native spell frames and contact sheet to " + folder);
            StagePlay.Finish(this, true);
        }

        void Prepare(GrassLabScene scene)
        {
            for (int i = 1; i < scene.creatures.Count; i++) scene.creatures[i].anchor.gameObject.SetActive(false);
            GrassLabScene.CreatureBody creature = scene.creatures[0];
            creature.anchor.position = Vector3.zero;
            creature.preview.Tick(0, 0, new FootFrame(Vector3.zero, Vector3.up, 1), Vector3.back);
            creature.Refresh();
            scene.field.bladeHeightScale = _manager.grass.bladeHeightScale;
            scene.field.windStrength = _manager.grass.windStrength;
            scene.look.settings = _manager.look.settings;
            scene.camera.orthographic = true;
            scene.camera.orthographicSize = 2.8f;
            scene.camera.transform.rotation = Quaternion.Euler(50, 0, 0);
            scene.camera.transform.position = Vector3.up * .45f - scene.camera.transform.forward * 9f;
        }

        SpellEffect Build(GrassLabScene scene, EffectRecipe recipe, Material material)
        {
            GameObject host = scene.Fixture("Spell polish " + recipe.element);
            SpellEffect effect = host.AddComponent<SpellEffect>();
            effect.enabled = false;
            effect.Init(recipe, _manager.meshes, material, LookSide.Plant);
            if (recipe.socket == EffectSocket.Link)
                effect.SetEndpoints(new Vector3(-1.45f, .65f, -.15f), new Vector3(1.45f, .65f, -.15f), false);
            else if (recipe.socket == EffectSocket.Ground)
                effect.transform.localScale = Vector3.one * 1.65f;
            else
            {
                if (!scene.creatures[0].preview.rig.TryGetAnchors(out EffectAnchors anchors))
                    throw new InvalidOperationException("Creature has no effect anchors.");
                EffectPlacement.Place(effect, null, anchors);
            }
            EffectPlacement.FaceCamera(effect, scene.camera);
            effect.SetSide(Entity.EntityType.Player);
            foreach (Transform part in host.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = 30;
            return effect;
        }

        static void ShowGround(GrassLabScene scene, EffectRecipe recipe)
        {
            ElementEntry entry = recipe.entry;
            if (recipe.socket == EffectSocket.Link)
                scene.ground.Play(entry.ground, Vector3.left * 1.45f, Vector3.right * 1.45f, entry.groundStrength);
            else
                scene.ground.Play(entry.ground, Vector3.zero, entry.groundRadius *
                    (recipe.socket == EffectSocket.Ground ? 1.65f : 1f), entry.groundStrength);
        }

        static void Tick(GrassLabScene scene, float step, float time)
        {
            scene.registry.PublishFrame(step);
            scene.ground.Advance(step);
            scene.field.UpdateField(scene.registry, scene.ground, step, time);
            scene.look.ApplyGlobals();
        }

        static EffectFamily Family(EffectKey element)
        {
            switch (element)
            {
                case EffectKey.Rise:
                case EffectKey.Ring:
                case EffectKey.Beam: return EffectFamily.Heal;
                case EffectKey.Stalks: return EffectFamily.Renew;
                case EffectKey.Drips: return EffectFamily.Rot;
                case EffectKey.Orbit:
                case EffectKey.Plates:
                case EffectKey.Bud: return EffectFamily.Boon;
                case EffectKey.Press:
                case EffectKey.Crack:
                case EffectKey.Litter: return EffectFamily.Bane;
                default: return EffectFamily.Damage;
            }
        }
    }
}
