using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{
    // Builds the recipe of a unit from its channels and the vocabulary asset at spawn, in memory and never saved
    public static class LookComposer
    {
        // How far past the body and head an accessory must reach on screen, in cells; stones need more to tell a
        // group apart
        public static readonly float PlantAccessoryReach = 0.25f;
        public static readonly float StoneAccessoryReach = 0.3f;

        // A stone barely moves at idle
        static readonly float stoneSwayDegrees = 0.6f;
        static readonly float stoneBreath = 0.01f;

        public static CreatureRecipe Compose(UnitChannels channels, LookVocabulary vocabulary)
        {
            if (!HasEntries(channels, vocabulary))
            {
                return null;
            }

            CreatureRecipe recipe = ScriptableObject.CreateInstance<CreatureRecipe>();
            recipe.name = "Derived" + channels.side + channels.head;
            recipe.hideFlags = HideFlags.DontSave;
            // Count carries gameplay meaning; an oversized recipe is rejected, never thinned silently.
            int seed = Seed(channels);
            int copies = Copies(channels.count);
            PartList parts = CreatureLayout.Build(channels, vocabulary, copies, seed, out UnitSockets sockets);
            if (parts == null)
            {
                RenderObjects.Release(recipe);
                return null;
            }

            int budget = Mathf.Min(vocabulary.maxParts, CreatureValidator.MaxParts);
            if (parts.count > budget)
            {
                Debug.LogError(
                    $"[LookComposer] {recipe.name}: {channels.count} requires {parts.count} parts,"
                        + $" exceeding the budget of {budget}; count is preserved."
                );
                RenderObjects.Release(recipe);
                return null;
            }

            float unit = vocabulary.Unit(channels.side);
            if (channels.accessory != AccessoryKind.None && !vocabulary.accessories[channels.accessory].isCentered)
            {
                float reach = LookMeasure.OutlineReach(parts, unit);
                float needed = AccessoryClearance(channels.side, vocabulary);
                if (reach < needed)
                {
                    Debug.LogError(
                        $"[LookComposer] {recipe.name}: the {channels.accessory} reaches {reach:0.00} cell"
                            + $" past the outline, under {needed}."
                    );
                }
            }

            recipe.parts = parts.ToArray();
            recipe.idle.seed = seed;
            recipe.stoneOchre = vocabulary.Colour(ColourRole.Ochre, channels.accent, channels.side);
            recipe.wiltColour = vocabulary.Colour(ColourRole.Wilt, channels.accent, channels.side);
            recipe.neckLocal = sockets.neck * unit;
            if (channels.side == LookSide.Plant)
            {
                recipe.roots = Roots(channels.reach, vocabulary);
                LianaShape.Arms(
                    recipe,
                    sockets.neck,
                    vocabulary.armCount,
                    unit,
                    vocabulary.Colour(ColourRole.Stem, channels.accent, LookSide.Plant),
                    vocabulary.Colour(ColourRole.Accent, channels.accent, LookSide.Plant)
                );
            }
            else
            {
                recipe.roots.count = 0;
                recipe.idle.swayDegrees = stoneSwayDegrees;
                recipe.idle.breathAmount = stoneBreath;
                recipe.sourceLocal = new Vector3[] { sockets.neck * unit };
            }

            if (!CreatureValidator.TryValidate(recipe, out string error))
            {
                Debug.LogError($"[LookComposer] {recipe.name}: {error}");
                RenderObjects.Release(recipe);
                return null;
            }

            return recipe;
        }

        // The parts of a unit at its own count, before the part cap; null when the vocabulary lacks an entry
        public static PartList Layout(UnitChannels channels, LookVocabulary vocabulary)
        {
            if (!HasEntries(channels, vocabulary))
            {
                return null;
            }

            return CreatureLayout.Build(channels, vocabulary, Copies(channels.count), Seed(channels), out _);
        }

        public static float AccessoryClearance(LookSide side, LookVocabulary vocabulary)
        {
            return side == LookSide.Plant
                ? vocabulary.layoutSettings.plantAccessoryClearance
                : vocabulary.layoutSettings.stoneAccessoryClearance;
        }

        public static int Copies(CountBand count)
        {
            switch (count)
            {
                case CountBand.Few:
                    return 3;
                case CountBand.Many:
                    return 5;
                default:
                    return 1;
            }
        }

        public static RootDefinition Roots(ReachBand band, LookVocabulary vocabulary)
        {
            vocabulary.roots.TryGetValue(band, out LookVocabulary.RootEntry entry);
            return Roots(vocabulary.Reach(band), vocabulary.Unit(LookSide.Plant), vocabulary, entry);
        }

        // Roots reaching this many body units from a body of this many cells
        public static RootDefinition Roots(float reach, float unit, LookVocabulary vocabulary)
        {
            return Roots(reach, unit, vocabulary, null);
        }

        static RootDefinition Roots(float reach, float unit, LookVocabulary vocabulary, LookVocabulary.RootEntry entry)
        {
            float mid = vocabulary.roots.ContainsKey(ReachBand.Mid) ? vocabulary.roots[ReachBand.Mid].reach : reach;
            // Roots shorter than the mid band bend once, the others twice
            int segments = 3;
            if (reach < mid)
            {
                segments = 2;
            }

            return new RootDefinition
            {
                count = vocabulary.rootCount,
                segments = segments,
                footRadius = reach * unit,
                hipHeight = vocabulary.rootHip * unit,
                kneeHeight = vocabulary.rootKnee * unit,
                thickness =
                    vocabulary.rootThickness * 0.5f * unit * (entry == null ? 1f : entry.effectiveThicknessScale),
                segmentShape = entry == null ? default : entry.segmentShape,
                jointShape = entry == null ? default : entry.jointShape,
                taper = entry == null ? 0.65f : entry.taper,
                jointScale = entry == null ? 2.8f : entry.jointScale,
                // The stem role takes no accent, any family reads the same colour
                colour = vocabulary.Colour(ColourRole.Stem, EffectFamily.Damage, LookSide.Plant),
            };
        }

        static bool HasEntries(UnitChannels channels, LookVocabulary vocabulary)
        {
            if (vocabulary == null || vocabulary.palette == null)
            {
                Debug.LogError("[LookComposer] Needs a vocabulary with a palette.");
                return false;
            }

            if (!CreatureCompositionValidator.TryValidate(channels, vocabulary, out string error))
            {
                Debug.LogError("[LookComposer] " + error);
                return false;
            }

            return true;
        }

        public static int Variant(int seed, int index)
        {
            return (seed * 31 + index * 7919) & 0x7fffffff;
        }

        static int Seed(UnitChannels channels)
        {
            int seed = 17;
            seed = seed * 31 + (int)channels.side;
            seed = seed * 31 + (int)channels.head;
            seed = seed * 31 + (int)channels.count;
            seed = seed * 31 + (int)channels.stem;
            seed = seed * 31 + (int)channels.mass;
            seed = seed * 31 + (int)channels.accessory;
            return seed;
        }
    }
}
