using System;
using System.Collections;
using System.Collections.Generic;
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

        [MenuItem("Tools/Render/Measure Spell Readability on Grass")]
        public static void Readability() { StagePlay.Enter("spell-readability", 600f); }
    }

    // Presentation fixtures, not gameplay casts: all authored entries run through the real parts and grass paths.
    public class SpellPolishRun : AStageRun
    {
        const float Step = 1f / 60f;
        public const float FrameStep = Step;
        static readonly float[] Phases = { 0.15f, 0.45f, 0.8f };
        public static readonly EffectKey[] CoreElements = { EffectKey.Burst, EffectKey.Rise, EffectKey.Stalks,
            EffectKey.Drips, EffectKey.Orbit, EffectKey.Plates, EffectKey.Bud, EffectKey.Press, EffectKey.Crack,
            EffectKey.ManaUp, EffectKey.ManaDown };
        // The elements that have a Stone entry, measured again as a stone caster draws them
        static readonly EffectKey[] StoneElements = { EffectKey.Burst, EffectKey.Rise, EffectKey.Press, EffectKey.Spark,
            EffectKey.Echo, EffectKey.Tether, EffectKey.Sprout, EffectKey.Stem };
        // The Boon offence kinds, measured in Plant: Stone draws the first six through the Plant fallback
        static readonly EffectKey[] KindElements = { EffectKey.Dart, EffectKey.Seeds, EffectKey.Cadence,
            EffectKey.Brackets, EffectKey.Footring, EffectKey.Canopy, EffectKey.Spark, EffectKey.Echo,
            EffectKey.Tether, EffectKey.Sprout, EffectKey.Stem };
        const string GainHandler = "Assets/Data/PlayerItems/ManaOnRoundEndItem/ManaOnRoundEndItem_BuffHandlerFactory.asset";
        const string DrainHandler = "Assets/Data/EntityItems/SiphonItem/BuffHandlerFactory.asset";
        readonly bool _isReadability;
        protected override bool shouldStartGame => false;

        public SpellPolishRun(bool isReadability = false) { _isReadability = isReadability; }

        protected override IEnumerator Run()
        {
            string folder = Path.Combine(StagePlay.CaptureFolder, _isReadability ? "spell-readability" : "spell-polish");
            Directory.CreateDirectory(folder);
            EffectVocabulary vocabulary = RenderAssets.Load<EffectVocabulary>(
                "Assets/Render/Spells/Data/EffectVocabulary.asset");
            Material material = RenderAssets.Load<Material>("Assets/Render/Look/Look_Default.mat");
            float[] phases = Phases;
            EffectKey[] elements = (EffectKey[])Enum.GetValues(typeof(EffectKey));
            using (var images = new SpellPolishImages(folder))
            {
                if (_isReadability)
                {
                    List<CharacterData> characters = AtlasAssetCatalog.Characters();
                    images.Resolved(Resolve(vocabulary, GainHandler, characters),
                        Resolve(vocabulary, DrainHandler, characters));
                    var readability = new List<SpellReadabilityPass.Fixture>();
                    // Each core element in its plant drawing on both target bodies, the plant ally and the stone enemy
                    foreach (EffectKey element in CoreElements)
                    {
                        readability.Add(new SpellReadabilityPass.Fixture(element, LookSide.Plant, LookSide.Plant));
                    }

                    foreach (EffectKey element in CoreElements)
                    {
                        readability.Add(new SpellReadabilityPass.Fixture(element, LookSide.Plant, LookSide.Stone));
                    }

                    foreach (EffectKey element in StoneElements)
                    {
                        readability.Add(new SpellReadabilityPass.Fixture(element, LookSide.Stone, LookSide.Plant));
                    }

                    foreach (EffectKey element in KindElements)
                    {
                        readability.Add(new SpellReadabilityPass.Fixture(element, LookSide.Plant, LookSide.Plant));
                    }
                    // The Stem shows one more segment per stack: one stack is its smallest drawing
                    readability.Add(new SpellReadabilityPass.Fixture(EffectKey.Stem, LookSide.Plant, LookSide.Plant, 1));
                    readability.Add(new SpellReadabilityPass.Fixture(EffectKey.Stem, LookSide.Stone, LookSide.Plant, 1));
                    yield return SpellReadabilityPass.Run(_manager, images, vocabulary, material, readability);
                }
                var fixtures = new List<(EffectKey element, LookSide side)>();
                if (!_isReadability)
                {
                    foreach (EffectKey element in elements)
                    {
                        fixtures.Add((element, LookSide.Plant));
                    }
                }

                foreach (var (element, side) in fixtures)
                {
                    using (var scene = new GrassLabScene(_manager))
                    {
                        if (!scene.Init())
                        {
                            throw new InvalidOperationException("Could not initialize grass spell fixture.");
                        }

                        Prepare(_manager, scene, 0);
                        for (int frame = 0; frame < 30; frame++)
                        {
                            Tick(scene, Step, frame * Step);
                        }

                        EffectRecipe recipe = EffectComposer.Compose(vocabulary, element, Family(element),
                            EffectTempo.Once, 0f, 3, 3, 0.5f, material: side);
                        SpellEffect effect = Build(_manager, scene, recipe, material, 0);
                        ShowGround(scene, recipe, Vector3.zero);
                        int sample = 0;
                        int total = Mathf.CeilToInt(recipe.cycleSeconds / Step);
                        for (int frame = 0; frame <= total && sample < phases.Length; frame++)
                        {
                            float age = frame * Step;
                            if (frame > 0)
                            {
                                effect.Advance(Step);
                            }

                            Tick(scene, Step, 0.5f + age);
                            if (age >= phases[sample] * recipe.cycleSeconds)
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
                if (_isReadability)
                {
                    images.WriteReadability();
                }
                else
                {
                    images.Write();
                }
            }
            if (_isReadability)
            {
                Debug.Log("[SpellPolishRun] Wrote spell readability frames and readability.csv to " + folder);
                StagePlay.Finish(this, true);
                yield break;
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

        // The element a handler resolves to as its own class casts it; a creature's or unowned handler reads plain
        public static EffectKey Resolve(EffectVocabulary vocabulary, string path, IEnumerable<CharacterData> characters)
        {
            ABuffHandlerFactory handler = RenderAssets.Load<ABuffHandlerFactory>(path);
            return EffectComposer.Element(vocabulary, PlayerClassContext.Channels(handler, true, characters));
        }

        // The lab's plant ally is creature 0, its stone enemy creature 1
        public static int CreatureIndex(LookSide side) { return side == LookSide.Plant ? 0 : 1; }

        public static void Prepare(RenderManager manager, GrassLabScene scene, params int[] shown)
        {
            Prepare(manager, scene, shown, new[] { Vector3.zero });
        }

        // Shows the listed creatures, the first at the first position and so on, and hides the rest
        public static void Prepare(RenderManager manager, GrassLabScene scene, int[] shown, Vector3[] positions)
        {
            for (int i = 0; i < scene.creatures.Count; i++)
            {
                scene.creatures[i].anchor.gameObject.SetActive(Array.IndexOf(shown, i) >= 0);
            }

            for (int k = 0; k < shown.Length; k++)
            {
                GrassLabScene.CreatureBody creature = scene.creatures[shown[k]];
                creature.anchor.position = positions[k];
                creature.preview.Tick(0, 0, new FootFrame(positions[k], Vector3.up, 1), Vector3.back);
                creature.Refresh();
            }
            scene.field.bladeHeightScale = manager.grass.bladeHeightScale;
            scene.field.windStrength = manager.grass.windStrength;
            scene.look.settings = manager.look.settings;
            scene.camera.orthographic = true;
            scene.camera.orthographicSize = 2.8f;
            scene.camera.transform.rotation = Quaternion.Euler(50, 0, 0);
            scene.camera.transform.position = Vector3.up * 0.45f - scene.camera.transform.forward * 9f;
        }

        public static SpellEffect Build(RenderManager manager, GrassLabScene scene, EffectRecipe recipe, Material material,
                                        int creature, LookSide target = LookSide.Plant)
        {
            GameObject host = scene.Fixture("Spell polish " + recipe.element);
            SpellEffect effect = host.AddComponent<SpellEffect>();
            effect.enabled = false;
            effect.Init(recipe, manager.meshes, material, target);
            if (recipe.socket == EffectSocket.Link)
            {
                effect.SetEndpoints(new Vector3(-1.45f, 0.65f, -0.15f), new Vector3(1.45f, 0.65f, -0.15f), false);
            }
            else if (recipe.socket == EffectSocket.Ground)
            {
                effect.transform.localScale = Vector3.one * 1.65f;
            }
            else
            {
                if (!scene.creatures[creature].preview.rig.TryGetAnchors(out EffectAnchors anchors))
                {
                    throw new InvalidOperationException("Creature has no effect anchors.");
                }

                EffectPlacement.Place(effect, null, anchors);
            }
            EffectPlacement.FaceCamera(effect, scene.camera);
            effect.SetSide(Entity.EntityType.Player);
            foreach (Transform part in host.GetComponentsInChildren<Transform>(true))
            {
                part.gameObject.layer = 30;
            }

            return effect;
        }

        public static void ShowGround(GrassLabScene scene, EffectRecipe recipe, Vector3 at)
        {
            ElementEntry entry = recipe.entry;
            if (recipe.socket == EffectSocket.Link)
            {
                scene.ground.Play(entry.ground, Vector3.left * 1.45f, Vector3.right * 1.45f, entry.groundStrength);
            }
            else
            {
                scene.ground.Play(entry.ground, at, entry.groundRadius *
                    (recipe.socket == EffectSocket.Ground ? 1.65f : 1f), entry.groundStrength);
            }
        }

        public static void Tick(GrassLabScene scene, float step, float time)
        {
            scene.registry.PublishFrame(step);
            scene.ground.Advance(step);
            scene.field.UpdateField(scene.registry, scene.ground, step, time);
            scene.look.ApplyGlobals();
        }

        public static EffectFamily Family(EffectKey element)
        {
            switch (element)
            {
                case EffectKey.Rise:
                case EffectKey.Ring:
                case EffectKey.Beam: return EffectFamily.Heal;
                case EffectKey.Stalks: return EffectFamily.Renew;
                case EffectKey.Drips: return EffectFamily.Rot;
                case EffectKey.Orbit:
                case EffectKey.Dart:
                case EffectKey.Seeds:
                case EffectKey.Cadence:
                case EffectKey.Brackets:
                case EffectKey.Footring:
                case EffectKey.Canopy:
                case EffectKey.Spark:
                case EffectKey.Echo:
                case EffectKey.Tether:
                case EffectKey.Sprout:
                case EffectKey.Stem:
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
