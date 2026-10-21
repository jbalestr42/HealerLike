using System;
using System.Collections;
using System.Collections.Generic;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // The readability measurement on the isolated grass lab, shared by the readability menu and the field variant
    // sheet: every fixture at its readable peak on its target body, then the element pairs a player must tell apart.
    public static class SpellReadabilityPass
    {
        // The middle moment is the harness's readable peak; the readability pass samples only that one
        public const float PeakPhase = 0.45f;
        // Every fixture's stacks unless it asks for others
        public const int DefaultStacks = 3;
        // Damage against heal, heal against boon, boon against bane, each family drawn by its first core element
        public static readonly (EffectKey first, EffectKey second)[] Pairs =
            { (EffectKey.Burst, EffectKey.Rise), (EffectKey.Rise, EffectKey.Orbit), (EffectKey.Orbit, EffectKey.Press) };
        // Two plant allies side by side, one grid cell apart; creature 2 is the lab's second NormalEntity
        static readonly int[] pairCreatures = { 0, 2 };
        static readonly Vector3[] pairPositions = { new Vector3(-0.5f, 0f, 0f), new Vector3(0.5f, 0f, 0f) };

        public readonly struct Fixture
        {
            public readonly EffectKey element;
            // The caster's drawing of the element
            public readonly LookSide material;
            // The body the element lands on
            public readonly LookSide target;
            // The stacks the element is composed at; a stacking element shows one more shape per stack
            public readonly int stacks;

            public Fixture(EffectKey element, LookSide material, LookSide target, int stacks = DefaultStacks)
            {
                this.element = element;
                this.material = material;
                this.target = target;
                this.stacks = stacks;
            }
        }

        public static IEnumerator Run(RenderManager manager, SpellPolishImages images, EffectVocabulary vocabulary,
                                      Material material, IEnumerable<Fixture> fixtures)
        {
            const float step = SpellPolishRun.FrameStep;
            foreach (Fixture fixture in fixtures)
            {
                using (GrassLabScene scene = new GrassLabScene(manager))
                {
                    if (!scene.Init())
                    {
                        throw new InvalidOperationException("Could not initialize grass spell fixture.");
                    }

                    int creature = SpellPolishRun.CreatureIndex(fixture.target);
                    SpellPolishRun.Prepare(manager, scene, creature);
                    for (int frame = 0; frame < 30; frame++)
                    {
                        SpellPolishRun.Tick(scene, step, frame * step);
                    }

                    EffectRecipe recipe = Compose(vocabulary, fixture.element, fixture.material, fixture.stacks);
                    SpellEffect effect = SpellPolishRun.Build(manager, scene, recipe, material, creature, fixture.target);
                    SpellPolishRun.ShowGround(scene, recipe, Vector3.zero);
                    int total = Mathf.CeilToInt(recipe.cycleSeconds / step);
                    for (int frame = 0; frame <= total; frame++)
                    {
                        float age = frame * step;
                        if (frame > 0)
                        {
                            effect.Advance(step);
                        }

                        SpellPolishRun.Tick(scene, step, 0.5f + age);
                        if (age >= PeakPhase * recipe.cycleSeconds)
                        {
                            images.Readability(scene.camera, scene.creatures[creature].anchor.gameObject, effect.gameObject,
                                fixture.element, age, recipe, effect.lifetime, fixture.target, fixture.stacks);
                            break;
                        }
                        yield return null;
                    }
                }
                yield return null;
            }
            foreach (var (first, second) in Pairs)
            {
                using (GrassLabScene scene = new GrassLabScene(manager))
                {
                    if (!scene.Init())
                    {
                        throw new InvalidOperationException("Could not initialize grass pair fixture.");
                    }

                    SpellPolishRun.Prepare(manager, scene, pairCreatures, pairPositions);
                    for (int frame = 0; frame < 30; frame++)
                    {
                        SpellPolishRun.Tick(scene, step, frame * step);
                    }

                    EffectRecipe firstRecipe = Compose(vocabulary, first, LookSide.Plant);
                    EffectRecipe secondRecipe = Compose(vocabulary, second, LookSide.Plant);
                    SpellEffect firstEffect = SpellPolishRun.Build(manager, scene, firstRecipe, material, pairCreatures[0]);
                    SpellEffect secondEffect = SpellPolishRun.Build(manager, scene, secondRecipe, material, pairCreatures[1]);
                    SpellPolishRun.ShowGround(scene, firstRecipe, pairPositions[0]);
                    SpellPolishRun.ShowGround(scene, secondRecipe, pairPositions[1]);
                    // Both at their own readable peak in the same frame: the later peak sets the frame count
                    float firstPeak = PeakPhase * firstRecipe.cycleSeconds, secondPeak = PeakPhase * secondRecipe.cycleSeconds;
                    float end = Mathf.Max(firstPeak, secondPeak);
                    for (float age = 0f; age < end; age += step)
                    {
                        if (age >= end - firstPeak)
                        {
                            firstEffect.Advance(step);
                        }

                        if (age >= end - secondPeak)
                        {
                            secondEffect.Advance(step);
                        }

                        SpellPolishRun.Tick(scene, step, 0.5f + age);
                        yield return null;
                    }
                    images.Pair(scene.camera, firstEffect.gameObject, secondEffect.gameObject, first, second);
                }
                yield return null;
            }
        }

        public static EffectRecipe Compose(EffectVocabulary vocabulary, EffectKey element, LookSide material,
                                           int stacks = DefaultStacks)
        {
            return EffectComposer.Compose(vocabulary, element, SpellPolishRun.Family(element), EffectTempo.Once, 0f, stacks, 3,
                0.5f, material: material);
        }
    }
}
