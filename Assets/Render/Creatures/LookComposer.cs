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
        // A stone's two limbs: their spread and depth in body units, their width and depth, their splay in degrees
        static readonly float limbSpread = 0.36f;
        static readonly float limbDepth = 0.05f;
        static readonly float limbWidth = 0.42f;
        static readonly float limbThickness = 0.45f;
        static readonly float limbSplay = 25f;
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

            // A busy head drawn many times can pass the part cap, then it is drawn fewer times
            int seed = Seed(channels);
            int copies = Copies(channels.count);
            PartList parts = Parts(channels, vocabulary, copies, seed, out UnitSockets sockets);
            while (parts.count > vocabulary.maxParts && copies > 1)
            {
                copies = copies > 3 ? 3 : 1;
                parts = Parts(channels, vocabulary, copies, seed, out sockets);
            }

            float unit = vocabulary.Unit(channels.side);
            if (channels.accessory != AccessoryKind.None)
            {
                float reach = LookMeasure.OutlineReach(parts, unit);
                float needed = channels.side == LookSide.Plant ? PlantAccessoryReach : StoneAccessoryReach;
                if (reach < needed)
                {
                    Debug.LogError($"[LookComposer] {recipe.name}: the {channels.accessory} reaches {reach:0.00} cell"
                        + $" past the outline, under {needed}.");
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
                LianaShape.Arms(recipe, sockets.neck, vocabulary.armCount, unit,
                    vocabulary.Colour(ColourRole.Stem, channels.accent, LookSide.Plant),
                    vocabulary.Colour(ColourRole.Accent, channels.accent, LookSide.Plant));
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
            return Parts(channels, vocabulary, Copies(channels.count), Seed(channels), out _);
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
            return Roots(vocabulary.Reach(band), vocabulary.Unit(LookSide.Plant), vocabulary);
        }

        // Roots reaching this many body units from a body of this many cells
        public static RootDefinition Roots(float reach, float unit, LookVocabulary vocabulary)
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
                thickness = vocabulary.rootThickness * 0.5f * unit,
                // The stem role takes no accent, any family reads the same colour
                colour = vocabulary.Colour(ColourRole.Stem, EffectFamily.Damage, LookSide.Plant)
            };
        }

        static bool HasEntries(UnitChannels channels, LookVocabulary vocabulary)
        {
            if (vocabulary == null || vocabulary.palette == null)
            {
                Debug.LogError("[LookComposer] Needs a vocabulary with a palette.");
                return false;
            }

            bool hasAccessory = channels.accessory == AccessoryKind.None
                || vocabulary.accessories.ContainsKey(channels.accessory);
            bool hasMiniHead = channels.accessory != AccessoryKind.MiniHead
                || vocabulary.heads.ContainsKey(channels.accessoryHead);
            if (!vocabulary.heads.ContainsKey(channels.head) || !vocabulary.bodies.ContainsKey(channels.mass)
                || !vocabulary.stems.ContainsKey(channels.stem) || !hasAccessory || !hasMiniHead)
            {
                Debug.LogError($"[LookComposer] The vocabulary has no entry for {channels.head}, {channels.mass},"
                    + $" {channels.stem} or {channels.accessory}.");
                return false;
            }
            return true;
        }

        static PartList Parts(UnitChannels channels, LookVocabulary vocabulary, int copies, int seed,
            out UnitSockets sockets)
        {
            PartList parts = new PartList(vocabulary.Unit(channels.side));
            sockets = UnitSockets.Place(channels, vocabulary);
            LookVocabulary.BodyEntry body = vocabulary.bodies[channels.mass];
            LookVocabulary.StemEntry stem = vocabulary.stems[channels.stem];
            bool isPlant = channels.side == LookSide.Plant;
            float scale = sockets.scale;
            Color stemColour = vocabulary.Colour(ColourRole.Stem, channels.accent, channels.side);
            if (isPlant)
            {
                Fragment(parts, vocabulary, channels, body.plant, sockets.body, 1f, CountBand.One, seed);
                parts.Link("Stem", sockets.stemFoot, sockets.neck, stem.thickness, stemColour, PartRole.Stem);
            }
            else
            {
                Fragment(parts, vocabulary, channels, body.stone, sockets.body, vocabulary.stoneScale, CountBand.One,
                    seed);
                Limbs(parts, stem.limbLength * scale, sockets.bodyRadius, scale, stemColour, seed);
            }

            LookVocabulary.HeadEntry head = vocabulary.heads[channels.head];
            LookPart[] headParts = isPlant ? head.plant : head.stone;
            if (head.carriesCount)
            {
                parts.headStarts.Add(parts.count);
                Fragment(parts, vocabulary, channels, headParts, sockets.neck, scale, channels.count, seed);
            }
            else if (copies == 1)
            {
                parts.headStarts.Add(parts.count);
                Fragment(parts, vocabulary, channels, headParts, sockets.neck, scale, CountBand.One, seed);
            }
            else
            {
                // Three or five smaller heads on a branching neck, spread so two neighbours never touch on screen;
                // a stone carries them side by side
                HeadFan fan = HeadFan.Shape(headParts, copies, isPlant);
                for (int i = 0; i < copies; i++)
                {
                    parts.headStarts.Add(parts.count);
                    Vector3 end = fan.Branch(parts, sockets.neck, i, scale, stemColour);
                    Fragment(parts, vocabulary, channels, headParts, end, fan.copyScale * scale, CountBand.One, seed);
                }
            }

            if (channels.accessory != AccessoryKind.None)
            {
                parts.accessoryStart = parts.count;
                LookVocabulary.AccessoryEntry accessory = vocabulary.accessories[channels.accessory];
                Vector3 socket = sockets.At(accessory.socket);
                LookPart[] accessoryParts = isPlant ? accessory.plant : accessory.stone;
                Fragment(parts, vocabulary, channels, accessoryParts, socket, scale, CountBand.One, seed);
                if (channels.accessory == AccessoryKind.MiniHead)
                {
                    LookVocabulary.HeadEntry mini = vocabulary.heads[channels.accessoryHead];
                    LookPart[] miniParts = isPlant ? mini.plant : mini.stone;
                    Vector3 miniAt = socket + accessory.miniHeadAt * scale;
                    float miniScale = accessory.miniHeadScale * scale;
                    Fragment(parts, vocabulary, channels, miniParts, miniAt, miniScale, CountBand.One, seed);
                }
            }
            return parts;
        }

        // A fragment's parts around a socket, those its count band allows; a stone part takes a variant from the seed
        static void Fragment(PartList parts, LookVocabulary vocabulary, UnitChannels channels, LookPart[] fragment,
            Vector3 at, float scale, CountBand band, int seed)
        {
            foreach (LookPart part in fragment)
            {
                if (part.minCount > band)
                {
                    continue;
                }

                Color colour = vocabulary.Colour(part.colour, channels.accent, channels.side);
                parts.Add(part.id, part.primitive, at + part.position * scale, part.size * scale, colour, part.euler,
                    part.glow, part.role, Variant(seed, parts.count));
            }
        }

        // Two boulder legs under a stone body, from the ground up into it
        static void Limbs(PartList parts, float limb, float bodyRadius, float scale, Color colour, int seed)
        {
            float height = limb + bodyRadius * 0.5f;
            for (int i = -1; i <= 1; i += 2)
            {
                Vector3 foot = new Vector3(limbSpread * i * scale, height * 0.5f, limbDepth);
                Vector3 size = new Vector3(limbWidth * scale, height, limbThickness * scale);
                parts.Add("Limb", Primitive.Stone, foot, size, colour, new Vector3(0f, limbSplay * i, 0f), 0f,
                    PartRole.Limb, Variant(seed, parts.count));
            }
        }

        static int Variant(int seed, int index)
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
