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
            PartList parts = Parts(channels, vocabulary, copies, seed, out UnitSockets sockets);
            if (parts == null)
            {
                RenderObjects.Release(recipe);
                return null;
            }
            int budget = Mathf.Min(vocabulary.maxParts, CreatureValidator.MaxParts);
            if (parts.count > budget)
            {
                Debug.LogError($"[LookComposer] {recipe.name}: {channels.count} requires {parts.count} parts,"
                    + $" exceeding the budget of {budget}; count is preserved.");
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

        public static float AccessoryClearance(LookSide side, LookVocabulary vocabulary)
        {
            return side == LookSide.Plant ? vocabulary.Layout.plantAccessoryClearance
                : vocabulary.Layout.stoneAccessoryClearance;
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

        static RootDefinition Roots(float reach, float unit, LookVocabulary vocabulary,
            LookVocabulary.RootEntry entry)
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
                thickness = vocabulary.rootThickness * 0.5f * unit * (entry == null ? 1f : entry.ThicknessScale),
                segmentShape = entry == null ? default : entry.segmentShape,
                jointShape = entry == null ? default : entry.jointShape,
                taper = entry == null ? 0.65f : entry.taper,
                jointScale = entry == null ? 2.8f : entry.jointScale,
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
            LookVocabulary.BodyEntry body = vocabulary.bodies[channels.mass];
            LookVocabulary.StemEntry stem = vocabulary.stems[channels.stem];
            bool plant = channels.side == LookSide.Plant;
            bool sizesValid = Positive(vocabulary.bodyUnit) && Positive(vocabulary.Unit(channels.side))
                && Positive(vocabulary.stoneScale) && Positive(body.scale) && float.IsFinite(body.bodyLift)
                && float.IsFinite(body.headScale) && body.headScale >= 0f
                && float.IsFinite(body.stemScale) && body.stemScale >= 0f
                && (plant ? Positive(stem.length) && Positive(stem.thickness) : Positive(stem.limbLength));
            if (!sizesValid || !vocabulary.Layout.IsValid() || vocabulary.maxParts < 1 || vocabulary.maxParts > CreatureValidator.MaxParts
                || !stem.plantShape.IsValid() || !stem.stoneLimbShape.IsValid())
            {
                Debug.LogError("[LookComposer] Invalid layout, shape profile or selected band dimensions.");
                return false;
            }
            LookVocabulary.HeadEntry head = vocabulary.heads[channels.head];
            if (head.plantStem != null && !head.plantStem.IsValid())
            {
                Debug.LogError("[LookComposer] Invalid articulated plant stem profile.");
                return false;
            }
            if (!float.IsFinite(head.plantStemScale) || head.plantStemScale < 0f)
            {
                Debug.LogError("[LookComposer] Plant family stem scale must be finite and nonnegative; zero keeps legacy length.");
                return false;
            }
            if (!FragmentPlacement.TryValidate(plant ? body.plant : body.stone, CountBand.One, out string error)
                || !FragmentPlacement.TryValidate(plant ? head.plant : head.stone,
                    head.carriesCount ? channels.count : CountBand.One, out error))
            {
                Debug.LogError("[LookComposer] " + error);
                return false;
            }
            if (channels.accessory != AccessoryKind.None)
            {
                LookVocabulary.AccessoryEntry accessory = vocabulary.accessories[channels.accessory];
                if (!FragmentPlacement.TryValidate(plant ? accessory.plant : accessory.stone, CountBand.One, out error))
                {
                    Debug.LogError("[LookComposer] " + error);
                    return false;
                }
                if (channels.accessory == AccessoryKind.MiniHead)
                {
                    LookVocabulary.HeadEntry mini = vocabulary.heads[channels.accessoryHead];
                    if (!FragmentPlacement.TryValidate(plant ? mini.plant : mini.stone, CountBand.One, out error))
                    {
                        Debug.LogError("[LookComposer] " + error);
                        return false;
                    }
                }
            }
            return true;
        }

        static bool Positive(float value) { return float.IsFinite(value) && value > 0f; }

        static PartList Parts(UnitChannels channels, LookVocabulary vocabulary, int copies, int seed,
            out UnitSockets sockets)
        {
            PartList parts = new PartList(vocabulary.Unit(channels.side));
            sockets = UnitSockets.Place(channels, vocabulary);
            LookVocabulary.BodyEntry body = vocabulary.bodies[channels.mass];
            LookVocabulary.StemEntry stem = vocabulary.stems[channels.stem];
            LookVocabulary.HeadEntry head = vocabulary.heads[channels.head];
            bool isPlant = channels.side == LookSide.Plant;
            float scale = sockets.scale;
            float headScale = sockets.headScale;
            Color stemColour = vocabulary.Colour(ColourRole.Stem, channels.accent, channels.side);
            if (isPlant)
            {
                if (!Fragment(parts, vocabulary, channels, body.plant, sockets.body, 1f, CountBand.One, seed))
                    return null;
                if (head.plantStem == null)
                {
                    parts.Link("Stem", sockets.stemFoot, sockets.neck, stem.thickness, stemColour, PartRole.Stem,
                        stem.plantShape);
                }
                else
                {
                    PlantStem(parts, sockets, stem, head.plantStem,
                        vocabulary.Colour(ColourRole.Body, channels.accent, channels.side));
                }
            }
            else
            {
                if (!Fragment(parts, vocabulary, channels, body.stone, sockets.body, vocabulary.stoneScale,
                    CountBand.One, seed)) return null;
                Limbs(parts, stem.limbLength * sockets.stemScale, sockets.bodyRadius, scale, stemColour, seed,
                    vocabulary.Layout, stem.stoneLimbShape);
            }

            LookPart[] headParts = isPlant ? head.plant : head.stone;
            if (head.carriesCount)
            {
                parts.headStarts.Add(parts.count);
                if (!Fragment(parts, vocabulary, channels, headParts, sockets.neck, headScale, channels.count, seed))
                    return null;
            }
            else if (copies == 1)
            {
                parts.headStarts.Add(parts.count);
                if (!Fragment(parts, vocabulary, channels, headParts, sockets.neck, headScale, CountBand.One, seed))
                    return null;
            }
            else
            {
                // Three or five smaller heads on a branching neck, spread so two neighbours never touch on screen;
                // a stone carries them side by side
                if (!FragmentPlacement.TryResolve(headParts, CountBand.One, seed, parts.count + 1,
                    out LookPart[] measuredHead, out string attachmentError))
                {
                    Debug.LogError("[LookComposer] " + attachmentError);
                    return null;
                }
                HeadFan fan = HeadFan.Shape(measuredHead, copies, isPlant, vocabulary.Layout,
                    isPlant ? stem.plantShape : stem.stoneLimbShape);
                for (int i = 0; i < copies; i++)
                {
                    parts.headStarts.Add(parts.count);
                    Vector3 end = fan.Branch(parts, sockets.neck, i, headScale, stemColour);
                    if (!Fragment(parts, vocabulary, channels, headParts, end, fan.copyScale * headScale,
                        CountBand.One, seed)) return null;
                }
            }

            if (channels.accessory != AccessoryKind.None)
            {
                parts.accessoryStart = parts.count;
                LookVocabulary.AccessoryEntry accessory = vocabulary.accessories[channels.accessory];
                Vector3 socket = sockets.At(accessory.socket);
                LookPart[] accessoryParts = isPlant ? accessory.plant : accessory.stone;
                if (!Fragment(parts, vocabulary, channels, accessoryParts, socket, scale, CountBand.One, seed))
                    return null;
                if (channels.accessory == AccessoryKind.MiniHead)
                {
                    LookVocabulary.HeadEntry mini = vocabulary.heads[channels.accessoryHead];
                    LookPart[] miniParts = isPlant ? mini.plant : mini.stone;
                    Vector3 miniAt = socket + accessory.miniHeadAt * scale;
                    float miniScale = accessory.miniHeadScale * headScale;
                    if (!Fragment(parts, vocabulary, channels, miniParts, miniAt, miniScale, CountBand.One, seed))
                        return null;
                }
                if (vocabulary.Layout.extendAccessorySupports && !accessory.isCentered)
                {
                    ExtendAccessory(parts, channels, vocabulary, socket, scale, stem, stemColour, seed);
                }
            }
            return parts;
        }

        static void PlantStem(PartList parts, UnitSockets sockets, LookVocabulary.StemEntry cadence,
            LookVocabulary.PlantStemEntry growth, Color colour)
        {
            float width = cadence.thickness * growth.thicknessScale;
            Vector3 start = sockets.stemFoot;
            for (int i = 0; i < growth.segments; i++)
            {
                float t = (i + 1f) / growth.segments;
                Vector3 end = Vector3.Lerp(sockets.stemFoot, sockets.neck, t)
                    + Vector3.right * (growth.bow * Mathf.Sin(Mathf.PI * t));
                parts.Link("StemGrowth", start, end, width, colour, PartRole.Stem, growth.segmentShape);
                if (i + 1 < growth.segments)
                {
                    parts.Add("StemJoint", Primitive.Sphere, end, Vector3.one * (width * growth.jointScale),
                        colour, Vector3.zero, 0f, PartRole.Stem, shape: growth.jointShape);
                }
                start = end;
            }
        }

        // Keep the whole accessory readable beyond a broad crown, with an attached support back to its socket.
        static void ExtendAccessory(PartList parts, UnitChannels channels, LookVocabulary vocabulary,
            Vector3 socket, float scale, LookVocabulary.StemEntry stem, Color colour, int seed)
        {
            float unit = vocabulary.Unit(channels.side);
            float needed = AccessoryClearance(channels.side, vocabulary);
            if (LookMeasure.OutlineReach(parts, unit) >= needed)
            {
                return;
            }

            float bodyRight = float.MinValue;
            float accessoryRight = float.MinValue;
            for (int i = 0; i < parts.count; i++)
            {
                LookPart part = parts.Source(i);
                Quaternion rotation = Quaternion.Euler(part.euler);
                Vector3 half = part.size * 0.5f;
                Vector3 direction = Quaternion.Inverse(rotation) * Vector3.right;
                if (i < parts.accessoryStart)
                {
                    bodyRight = Mathf.Max(bodyRight, part.position.x + LookMeasure.Extent(half, direction, part.shape));
                }
                else
                {
                    // OutlineReach samples the six face centres, so use the rightmost of those same samples.
                    float face = Mathf.Max(Mathf.Abs(half.x * direction.x), Mathf.Abs(half.y * direction.y),
                        Mathf.Abs(half.z * direction.z));
                    accessoryRight = Mathf.Max(accessoryRight, part.position.x + face);
                }
            }
            if (!float.IsFinite(bodyRight) || !float.IsFinite(accessoryRight))
            {
                return;
            }

            float shift = Mathf.Max(0f, bodyRight + needed / unit - accessoryRight + 0.001f);
            Vector3 offset = Vector3.right * shift;
            parts.Translate(parts.accessoryStart, offset);
            bool plant = channels.side == LookSide.Plant;
            ShapeProfile profile = plant ? stem.plantShape : stem.stoneLimbShape;
            Vector3 size = new Vector3(vocabulary.Layout.branchThickness * scale,
                shift + vocabulary.Layout.branchThickness * scale, vocabulary.Layout.branchThickness * scale);
            parts.Add("AccessorySupport", plant ? Primitive.Capsule : Primitive.Stone,
                socket + offset * 0.5f, size, colour, new Vector3(0f, 0f, -90f), 0f, PartRole.Accessory,
                Variant(seed, parts.count), profile);
        }

        // A fragment's parts around a socket, those its count band allows; a stone part takes a variant from the seed
        static bool Fragment(PartList parts, LookVocabulary vocabulary, UnitChannels channels, LookPart[] fragment,
            Vector3 at, float scale, CountBand band, int seed)
        {
            if (!FragmentPlacement.TryResolve(fragment, band, seed, parts.count, out LookPart[] resolved,
                out string error))
            {
                Debug.LogError("[LookComposer] " + error);
                return false;
            }
            foreach (LookPart part in resolved)
            {
                Color colour = vocabulary.Colour(part.colour, channels.accent, channels.side);
                parts.Add(part.id, part.primitive, at + part.position * scale, part.size * scale, colour, part.euler,
                    part.glow, part.role, Variant(seed, parts.count), part.shape);
            }
            return true;
        }

        // Exactly two mineral legs; the cadence band supplies their profile and length.
        static void Limbs(PartList parts, float limb, float bodyRadius, float scale, Color colour, int seed,
            LookVocabulary.LayoutEntry layout, ShapeProfile shape)
        {
            float height = limb + bodyRadius * layout.limbBodyOverlap;
            for (int i = -1; i <= 1; i += 2)
            {
                float proportion = 1f + i * layout.limbAsymmetry;
                Vector3 foot = new Vector3(layout.limbSpread * i * scale, height * proportion * 0.5f, layout.limbDepth);
                Vector3 size = new Vector3(layout.limbWidth * scale, height, layout.limbThickness * scale) * proportion;
                parts.Add("Limb", Primitive.Stone, foot, size, colour, new Vector3(0f, layout.limbSplay * i, 0f), 0f,
                    PartRole.Limb, Variant(seed, parts.count), shape);
            }
        }

        internal static int Variant(int seed, int index)
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
