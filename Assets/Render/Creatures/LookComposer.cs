using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{
    // Builds the recipe of a unit from its channels at spawn, in memory and never saved
    public static class LookComposer
    {
        // Cells per body unit: the reference study reads 1.8 body units to a cell
        public static readonly float BodyUnit = 0.55f;
        public static readonly int MaxParts = 40;
        // Root reach in body units per band, the study's measured spread
        public static readonly float ShortReach = 1.1f;
        public static readonly float MidReach = 1.5f;
        public static readonly float LongReach = 2.1f;
        // Every live unit has a board-wide range, so reach stays at one value until the data has bands
        public static readonly bool IsReachPinned = true;
        public static readonly float PinnedReach = 1.3f;
        public static readonly int RootCount = 10;
        // Root radius in cells, a 0.15 body unit thick cylinder
        public static readonly float RootThickness = 0.042f;
        public static readonly float RootHip = 0.08f;
        public static readonly float RootKnee = 0.06f;
        public static readonly float AccessoryReach = 0.45f;
        public static readonly int ArmCount = 2;
        public static readonly Color StoneWilt = new Color(0.22f, 0.25f, 0.33f);
        // A sturdy stone stands about 2.2 body units tall, the study's central enemy stone
        public static readonly float StoneScale = 1.6f;

        public static CreatureRecipe Compose(UnitChannels channels)
        {
            CreatureRecipe recipe = ScriptableObject.CreateInstance<CreatureRecipe>();
            recipe.name = "Derived" + channels.side + channels.head;
            recipe.hideFlags = HideFlags.DontSave;

            // A busy head drawn many times can pass the part cap, then it is drawn fewer times
            int copies = Copies(channels.count);
            PartList parts = Parts(channels, copies, out Vector3 neck, out Vector3 bodyCentre);
            while (parts.count > MaxParts && copies > 1)
            {
                copies = copies > 3 ? 3 : 1;
                parts = Parts(channels, copies, out neck, out bodyCentre);
            }

            recipe.parts = parts.ToArray();
            recipe.targetLocal = bodyCentre * BodyUnit;
            recipe.idle.seed = Seed(channels);
            if (channels.side == LookSide.Plant)
            {
                recipe.roots = Roots(channels.reach);
                Arms(recipe, neck, ArmCount, BodyUnit, PartVocabulary.PlantStem);
            }
            else
            {
                recipe.roots.count = 0;
                recipe.idle.swayDegrees = 0.6f;
                recipe.idle.breathAmount = 0.01f;
                recipe.wiltColour = StoneWilt;
                recipe.sourceLocal = new Vector3[] { neck * BodyUnit };
            }

            if (!CreatureValidator.TryValidate(recipe, out string error))
            {
                Debug.LogError($"[LookComposer] {recipe.name}: {error}");
                Object.DestroyImmediate(recipe);
                return null;
            }
            return recipe;
        }

        // The same unit composed from the vocabulary asset instead of the code
        public static CreatureRecipe Compose(UnitChannels channels, LookVocabulary vocabulary)
        {
            if (!HasEntries(channels, vocabulary))
            {
                return null;
            }

            CreatureRecipe recipe = ScriptableObject.CreateInstance<CreatureRecipe>();
            recipe.name = "Derived" + channels.side + channels.head;
            recipe.hideFlags = HideFlags.DontSave;

            int copies = Copies(channels.count);
            PartList parts = Parts(channels, vocabulary, copies, out Vector3 neck, out Vector3 bodyCentre);
            while (parts.count > vocabulary.maxParts && copies > 1)
            {
                copies = copies > 3 ? 3 : 1;
                parts = Parts(channels, vocabulary, copies, out neck, out bodyCentre);
            }

            recipe.parts = parts.ToArray();
            recipe.targetLocal = bodyCentre * vocabulary.bodyUnit;
            recipe.idle.seed = Seed(channels);
            if (channels.side == LookSide.Plant)
            {
                recipe.roots = Roots(channels.reach, vocabulary);
                Arms(recipe, neck, vocabulary.armCount, vocabulary.bodyUnit, vocabulary.palette.plantStem);
            }
            else
            {
                recipe.roots.count = 0;
                recipe.idle.swayDegrees = 0.6f;
                recipe.idle.breathAmount = 0.01f;
                recipe.wiltColour = vocabulary.stoneWilt;
                recipe.sourceLocal = new Vector3[] { neck * vocabulary.bodyUnit };
            }

            if (!CreatureValidator.TryValidate(recipe, out string error))
            {
                Debug.LogError($"[LookComposer] {recipe.name}: {error}");
                Object.DestroyImmediate(recipe);
                return null;
            }
            return recipe;
        }

        static bool HasEntries(UnitChannels channels, LookVocabulary vocabulary)
        {
            if (vocabulary == null || vocabulary.palette == null)
            {
                Debug.LogError("[LookComposer] Needs a vocabulary with a palette.");
                return false;
            }

            bool hasAccessory = channels.accessory == AccessoryKind.None || vocabulary.accessories.ContainsKey(channels.accessory);
            bool hasMiniHead = channels.accessory != AccessoryKind.MiniHead || vocabulary.heads.ContainsKey(channels.accessoryHead);
            if (!vocabulary.heads.ContainsKey(channels.head) || !vocabulary.bodies.ContainsKey(channels.mass)
                || !vocabulary.stems.ContainsKey(channels.stem) || !hasAccessory || !hasMiniHead)
            {
                Debug.LogError($"[LookComposer] The vocabulary has no entry for {channels.head}, {channels.mass}, {channels.stem} or {channels.accessory}.");
                return false;
            }
            return true;
        }

        public static RootDefinition Roots(ReachBand band, LookVocabulary vocabulary)
        {
            float reach = vocabulary.Reach(band);
            float mid = vocabulary.roots.ContainsKey(ReachBand.Mid) ? vocabulary.roots[ReachBand.Mid].reach : reach;
            return new RootDefinition
            {
                count = vocabulary.rootCount,
                segments = reach < mid ? 2 : 3,
                footRadius = reach * vocabulary.bodyUnit,
                hipHeight = vocabulary.rootHip,
                kneeHeight = vocabulary.rootKnee,
                thickness = vocabulary.rootThickness,
                colour = vocabulary.palette.plantStem
            };
        }

        static PartList Parts(UnitChannels channels, LookVocabulary vocabulary, int copies, out Vector3 neck, out Vector3 bodyCentre)
        {
            PartList parts = new PartList(vocabulary.bodyUnit);
            LookVocabulary.BodyEntry body = vocabulary.bodies[channels.mass];
            LookVocabulary.StemEntry stem = vocabulary.stems[channels.stem];
            bool isPlant = channels.side == LookSide.Plant;
            LookPart[] bodyParts = isPlant ? body.plant : body.stone;
            float scale = isPlant ? 1f : vocabulary.stoneScale;
            float bodyRadius = bodyParts[0].size.x * 0.5f * scale;
            Vector3 top;
            if (isPlant)
            {
                bodyCentre = Vector3.up * (0.42f * body.scale);
                Fragment(parts, vocabulary, channels, bodyParts, bodyCentre, 1f, CountBand.One);
                Vector3 stemFoot = bodyCentre + Vector3.up * (bodyRadius * 0.8f);
                top = stemFoot + Vector3.up * stem.length;
                parts.Link("Stem", stemFoot, top, stem.thickness, vocabulary.Colour(ColourRole.Stem, channels.accent, channels.side));
            }
            else
            {
                bodyCentre = Vector3.up * (stem.limbLength + bodyRadius * 0.8f);
                Fragment(parts, vocabulary, channels, bodyParts, bodyCentre, scale, CountBand.One);
                Limbs(parts, stem.limbLength, body.scale * scale, vocabulary.Colour(ColourRole.Limb, channels.accent, channels.side));
                top = bodyCentre + Vector3.up * (bodyRadius * 0.75f);
            }

            neck = top;
            Vector3 hip = bodyCentre;
            LookVocabulary.HeadEntry head = vocabulary.heads[channels.head];
            LookPart[] headParts = isPlant ? head.plant : head.stone;
            if (head.carriesCount)
            {
                Fragment(parts, vocabulary, channels, headParts, top, 1f, channels.count);
            }
            else if (copies == 1)
            {
                Fragment(parts, vocabulary, channels, headParts, top, 1f, CountBand.One);
            }
            else
            {
                float copyScale = copies == 3 ? 0.72f : 0.55f;
                Color branch = vocabulary.Colour(ColourRole.Stem, channels.accent, channels.side);
                for (int i = 0; i < copies; i++)
                {
                    Vector3 end = Branch(parts, top, i, copies, isPlant, branch);
                    Fragment(parts, vocabulary, channels, headParts, end, copyScale, CountBand.One);
                }
            }

            if (channels.accessory != AccessoryKind.None)
            {
                LookVocabulary.AccessoryEntry accessory = vocabulary.accessories[channels.accessory];
                Vector3 socket = Socket(accessory.socket, hip, neck, bodyRadius + vocabulary.accessoryReach * 0.5f);
                Fragment(parts, vocabulary, channels, isPlant ? accessory.plant : accessory.stone, socket, 1f, CountBand.One);
                if (channels.accessory == AccessoryKind.MiniHead)
                {
                    LookVocabulary.HeadEntry mini = vocabulary.heads[channels.accessoryHead];
                    Fragment(parts, vocabulary, channels, isPlant ? mini.plant : mini.stone, socket + accessory.miniHeadAt,
                        accessory.miniHeadScale, CountBand.One);
                }
            }
            return parts;
        }

        // A fragment's parts around a socket, those its count band allows
        static void Fragment(PartList parts, LookVocabulary vocabulary, UnitChannels channels, LookPart[] fragment, Vector3 at,
            float scale, CountBand band)
        {
            foreach (LookPart part in fragment)
            {
                if (part.minCount > band)
                {
                    continue;
                }

                Color colour = vocabulary.Colour(part.colour, channels.accent, channels.side);
                parts.Add(part.id, part.primitive, at + part.position * scale, part.size * scale, colour, part.euler, part.glow,
                    part.role);
            }
        }

        // Reach in body units, pinned while every unit reaches the whole board
        public static float Reach(ReachBand band)
        {
            if (IsReachPinned)
            {
                return PinnedReach;
            }

            switch (band)
            {
                case ReachBand.Short:
                    return ShortReach;
                case ReachBand.Mid:
                    return MidReach;
                default:
                    return LongReach;
            }
        }

        public static float MassScale(MassBand mass)
        {
            switch (mass)
            {
                case MassBand.Light:
                    return 0.85f;
                case MassBand.Heavy:
                    return 1.3f;
                default:
                    return 1f;
            }
        }

        // Stem length in body units, 2.4 : 1.6 : 1 from quick to slow
        public static float StemLength(StemBand stem)
        {
            switch (stem)
            {
                case StemBand.Quick:
                    return 1.2f;
                case StemBand.Slow:
                    return 0.5f;
                default:
                    return 0.8f;
            }
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

        public static RootDefinition Roots(ReachBand band)
        {
            float reach = Reach(band);
            return new RootDefinition
            {
                count = RootCount,
                segments = reach < MidReach ? 2 : 3,
                footRadius = reach * BodyUnit,
                hipHeight = RootHip,
                kneeHeight = RootKnee,
                thickness = RootThickness,
                colour = PartVocabulary.PlantStem
            };
        }

        static PartList Parts(UnitChannels channels, int copies, out Vector3 neck, out Vector3 bodyCentre)
        {
            PartList parts = new PartList(BodyUnit);
            float mass = MassScale(channels.mass);
            Color accent = PartVocabulary.Accent(channels.accent);
            bool isPlant = channels.side == LookSide.Plant;
            float bodyRadius;
            Vector3 top;
            if (isPlant)
            {
                bodyRadius = 0.5f * mass;
                bodyCentre = Vector3.up * (0.42f * mass);
                Body(parts, channels.side, channels.mass, bodyCentre, StoneScale);
                float length = StemLength(channels.stem);
                float thickness = channels.stem == StemBand.Slow ? 0.28f : 0.2f;
                Vector3 stemFoot = bodyCentre + Vector3.up * (bodyRadius * 0.8f);
                top = stemFoot + Vector3.up * length;
                parts.Link("Stem", stemFoot, top, thickness, PartVocabulary.PlantStem);
            }
            else
            {
                // Stones stand on boulder limbs, the stem band is the limb length, and start larger than plants
                float scale = mass * StoneScale;
                float limb = StemLength(channels.stem) * 0.6f;
                bodyRadius = 0.5f * scale;
                bodyCentre = Vector3.up * (limb + bodyRadius * 0.8f);
                Body(parts, channels.side, channels.mass, bodyCentre, StoneScale);
                Limbs(parts, limb, scale, PartVocabulary.StoneLimb);
                top = bodyCentre + Vector3.up * (bodyRadius * 0.75f);
            }

            neck = top;
            Vector3 hip = bodyCentre;
            if (channels.head == HeadKind.Arch)
            {
                PartVocabulary.Head(parts, channels.head, channels.side, top, 1f, Copies(channels.count), accent);
            }
            else if (copies == 1)
            {
                PartVocabulary.Head(parts, channels.head, channels.side, top, 1f, 1, accent);
            }
            else
            {
                // Three or five smaller heads on a branching neck, a stone carries them side by side
                float scale = copies == 3 ? 0.72f : 0.55f;
                for (int i = 0; i < copies; i++)
                {
                    Vector3 end = Branch(parts, top, i, copies, isPlant, PartVocabulary.PlantStem);
                    PartVocabulary.Head(parts, channels.head, channels.side, end, scale, 1, accent);
                }
            }

            if (channels.accessory != AccessoryKind.None)
            {
                Vector3 socket = Socket(PartVocabulary.Socket(channels.accessory), hip, neck, bodyRadius + AccessoryReach * 0.5f);
                PartVocabulary.Accessory(parts, channels.accessory, channels.side, socket, accent);
                if (channels.accessory == AccessoryKind.MiniHead)
                {
                    PartVocabulary.Head(parts, channels.accessoryHead, channels.side, socket + PartVocabulary.MiniHeadAt,
                        PartVocabulary.MiniHeadScale, 1, accent);
                }
            }
            return parts;
        }

        // The body parts around the body centre, a stone's grown by the stone scale
        public static void Body(PartList parts, LookSide side, MassBand band, Vector3 centre, float stoneScale)
        {
            float mass = MassScale(band);
            if (side == LookSide.Plant)
            {
                parts.Add("Body", Primitive.Sphere, centre, Vector3.one * mass, PartVocabulary.PlantBody);
                if (band == MassBand.Heavy)
                {
                    parts.Add("BaseBulb", Primitive.Sphere, Vector3.up * 0.14f + (centre - Vector3.up * (0.42f * mass)),
                        new Vector3(1.3f, 0.45f, 1.3f) * mass, PartVocabulary.PlantBody);
                }
                return;
            }

            float scale = mass * stoneScale;
            parts.Add("Body", Primitive.Boulder, centre, new Vector3(1f, 0.85f, 1f) * scale, PartVocabulary.StoneBody);
            if (band == MassBand.Heavy)
            {
                parts.Add("Base", Primitive.Boulder, centre + Vector3.down * (0.5f * scale * 0.55f),
                    new Vector3(1.35f, 0.5f, 1.2f) * scale, PartVocabulary.StoneLimb, new Vector3(0f, 40f, 0f));
            }
        }

        // Two boulder legs under a stone body
        static void Limbs(PartList parts, float limb, float scale, Color colour)
        {
            for (int i = -1; i <= 1; i += 2)
            {
                Vector3 foot = new Vector3(0.36f * i * scale, (limb + 0.2f) * 0.5f, 0.05f);
                parts.Add("Limb", Primitive.Boulder, foot, new Vector3(0.42f, limb + 0.3f, 0.45f) * scale, colour,
                    new Vector3(0f, 25f * i, 0f), 0f, PartRole.Limb);
            }
        }

        // One branch of a fanned neck, returns where its head sits
        static Vector3 Branch(PartList parts, Vector3 top, int index, int copies, bool isPlant, Color colour)
        {
            float angle = (index - (copies - 1) * 0.5f) * (copies == 3 ? 40f : 28f);
            Vector3 direction = Quaternion.Euler(0f, 0f, -angle) * Vector3.up;
            Vector3 end = top + direction * (isPlant ? 0.5f : 0.35f);
            if (isPlant)
            {
                parts.Link("Branch", top, end, 0.12f, colour);
            }
            else
            {
                end.y = top.y;
            }
            return end;
        }

        // Where an accessory socket sits on the body
        static Vector3 Socket(AccessorySocket socket, Vector3 hip, Vector3 neck, float radius)
        {
            switch (socket)
            {
                case AccessorySocket.NeckOrbit:
                case AccessorySocket.Crook:
                    return neck;
                case AccessorySocket.Shoulder:
                    return hip + Vector3.left * radius;
                default:
                    return hip;
            }
        }

        // Lianas from the neck: four coils of 48 half-cell links reach across the 16-cell board
        static void Arms(CreatureRecipe recipe, Vector3 neck, int armCount, float bodyUnit, Color colour)
        {
            Vector3 bodyPivot = recipe.parts[0].localPosition;
            recipe.sourceLocal = new Vector3[armCount];
            recipe.arms = new ArmDefinition[armCount];
            for (int j = 0; j < armCount; j++)
            {
                recipe.sourceLocal[j] = (neck + Vector3.right * (j % 2 == 0 ? -0.15f : 0.15f)) * bodyUnit;
                Vector3[] rest = new Vector3[49];
                for (int i = 0; i < 48; i++)
                {
                    float angle = i * Mathf.PI * 2f / 12f;
                    rest[i + 1] = rest[i] + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0.015f).normalized * 0.5f;
                }

                recipe.arms[j] = new ArmDefinition
                {
                    bodyPart = 0,
                    rootLocal = recipe.sourceLocal[j] - bodyPivot,
                    sourceSocketIndex = j,
                    segmentCount = 48,
                    segmentLength = 0.5f,
                    radius = 0.045f,
                    restJoints = rest,
                    bendPole = Vector3.up,
                    colour = colour
                };
            }
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
