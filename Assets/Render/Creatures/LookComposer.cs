using UnityEngine;
using HealerLike.Render.Spells;

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
                Arms(recipe, neck);
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
                parts.Add("Body", Primitive.Sphere, bodyCentre, Vector3.one * (bodyRadius * 2f), PartVocabulary.PlantBody);
                if (channels.mass == MassBand.Heavy)
                {
                    parts.Add("BaseBulb", Primitive.Sphere, Vector3.up * 0.14f, new Vector3(1.3f, 0.45f, 1.3f) * mass,
                        PartVocabulary.PlantBody);
                }

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
                parts.Add("Body", Primitive.Boulder, bodyCentre, new Vector3(1f, 0.85f, 1f) * scale, PartVocabulary.StoneBody);
                for (int i = -1; i <= 1; i += 2)
                {
                    Vector3 foot = new Vector3(0.36f * i * scale, (limb + 0.2f) * 0.5f, 0.05f);
                    parts.Add("Limb", Primitive.Boulder, foot, new Vector3(0.42f, limb + 0.3f, 0.45f) * scale, PartVocabulary.StoneLimb,
                        new Vector3(0f, 25f * i, 0f));
                }

                if (channels.mass == MassBand.Heavy)
                {
                    parts.Add("Base", Primitive.Boulder, bodyCentre + Vector3.down * (bodyRadius * 0.55f),
                        new Vector3(1.35f, 0.5f, 1.2f) * scale, PartVocabulary.StoneLimb, new Vector3(0f, 40f, 0f));
                }

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
                    float angle = (i - (copies - 1) * 0.5f) * (copies == 3 ? 40f : 28f);
                    Vector3 direction = Quaternion.Euler(0f, 0f, -angle) * Vector3.up;
                    Vector3 end = top + direction * (isPlant ? 0.5f : 0.35f);
                    if (isPlant)
                    {
                        parts.Link("Branch", top, end, 0.12f, PartVocabulary.PlantStem);
                    }
                    else
                    {
                        end.y = top.y;
                    }

                    PartVocabulary.Head(parts, channels.head, channels.side, end, scale, 1, accent);
                }
            }

            PartVocabulary.Accessory(parts, channels.accessory, channels.accessoryHead, channels.side, hip, neck,
                bodyRadius + AccessoryReach * 0.5f, accent);
            return parts;
        }

        // Lianas from the neck: four coils of 48 half-cell links reach across the 16-cell board
        static void Arms(CreatureRecipe recipe, Vector3 neck)
        {
            Vector3 bodyPivot = recipe.parts[0].localPosition;
            recipe.sourceLocal = new Vector3[ArmCount];
            recipe.arms = new ArmDefinition[ArmCount];
            for (int j = 0; j < ArmCount; j++)
            {
                recipe.sourceLocal[j] = (neck + Vector3.right * (j % 2 == 0 ? -0.15f : 0.15f)) * BodyUnit;
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
                    colour = PartVocabulary.PlantStem
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
