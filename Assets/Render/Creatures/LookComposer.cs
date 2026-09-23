using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Grammar;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Creatures
{
    // Builds the recipe of a unit from its channels and the vocabulary asset at spawn, in memory and never saved
    public static class LookComposer
    {
        // How far past the body and head an accessory must reach on screen, in cells; stones need more to tell a group apart
        public static readonly float PlantAccessoryReach = 0.25f;
        public static readonly float StoneAccessoryReach = 0.3f;
        // The board camera's pitch, an accessory is measured on its screen plane
        static readonly Quaternion boardCamera = Quaternion.Euler(StageCalibration.PortraitPitch, 0f, 0f);

        // Parts laid out in body units around the creature's foot, turned into recipe parts in cells.
        // Every part hangs from the first one, which never rotates, so a part's local position is its offset from it.
        public class PartList
        {
            readonly List<CreaturePart> _parts = new List<CreaturePart>();
            readonly List<Vector3> _positions = new List<Vector3>();
            readonly List<LookPart> _sources = new List<LookPart>();
            readonly HashSet<string> _ids = new HashSet<string>();
            float _unit;

            public int count { get { return _parts.Count; } }

            // The first accessory part, every part before it is body and head
            public int accessoryStart { get; set; } = -1;

            public PartList(float unit)
            {
                _unit = unit;
            }

            // Size is the part's bounding box in body units, whatever the mesh's own pivot
            public int Add(string id, Primitive primitive, Vector3 centre, Vector3 size, Color colour, Vector3 euler, float glow,
                PartRole role, int variant = 0)
            {
                _sources.Add(new LookPart
                {
                    id = id,
                    primitive = primitive,
                    role = role,
                    position = centre,
                    euler = euler,
                    size = size,
                    glow = glow
                });

                Vector3 dimensions = size;
                Vector3 pivot = centre;
                Quaternion rotation = Quaternion.Euler(euler);
                if (primitive == Primitive.Boulder || primitive == Primitive.Stone)
                {
                    // The baked boulder spans two units across and 1.7 from -0.7 to 1, a stone variant falls back to it
                    dimensions = new Vector3(size.x * 0.5f, size.y / 1.7f, size.z * 0.5f);
                    pivot = centre - rotation * (Vector3.up * (0.15f * dimensions.y));
                }
                else if (primitive == Primitive.Pyramid)
                {
                    // The baked pyramid stands on its base, apex one unit up
                    pivot = centre - rotation * (Vector3.up * (size.y * 0.5f));
                }

                string uniqueId = _ids.Contains(id) ? id + _parts.Count : id;
                _ids.Add(uniqueId);
                Vector3 origin = _positions.Count > 0 ? _positions[0] : Vector3.zero;
                _parts.Add(new CreaturePart
                {
                    id = uniqueId,
                    parent = _parts.Count == 0 ? -1 : 0,
                    primitive = primitive,
                    localPosition = (_parts.Count == 0 ? pivot : pivot - origin) * _unit,
                    localEuler = euler,
                    dimensions = dimensions * _unit,
                    colour = colour,
                    torusTubeRatio = 0.2f,
                    glow = glow,
                    role = role,
                    variant = variant
                });
                _positions.Add(pivot);
                return _parts.Count - 1;
            }

            // A capsule from one point to another
            public int Link(string id, Vector3 from, Vector3 to, float thickness, Color colour, PartRole role)
            {
                Vector3 delta = to - from;
                Vector3 euler = Quaternion.FromToRotation(Vector3.up, delta).eulerAngles;
                return Add(id, Primitive.Capsule, (from + to) * 0.5f, new Vector3(thickness, delta.magnitude + thickness, thickness),
                    colour, euler, 0f, role);
            }

            // The part as it was added, centre and bounding box in body units before any pivot or mesh correction
            public LookPart Source(int index)
            {
                return _sources[index];
            }

            public CreaturePart[] ToArray()
            {
                return _parts.ToArray();
            }
        }

        // Where the composer put the body, in its side's body units from the foot
        public struct Sockets
        {
            public Vector3 foot;
            public Vector3 body;
            public float bodyRadius;
            public Vector3 hip;
            public Vector3 neck;
            // Mass scale, times the stone scale on stones
            public float scale;
        }

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
            PartList parts = Parts(channels, vocabulary, copies, seed, out Sockets sockets);
            while (parts.count > vocabulary.maxParts && copies > 1)
            {
                copies = copies > 3 ? 3 : 1;
                parts = Parts(channels, vocabulary, copies, seed, out sockets);
            }

            float unit = vocabulary.Unit(channels.side);
            if (channels.accessory != AccessoryKind.None)
            {
                float reach = OutlineReach(parts, unit);
                float needed = channels.side == LookSide.Plant ? PlantAccessoryReach : StoneAccessoryReach;
                if (reach < needed)
                {
                    Debug.LogError($"[LookComposer] {recipe.name}: the {channels.accessory} reaches {reach:0.00} cell past the outline, under {needed}.");
                }
            }

            recipe.parts = parts.ToArray();
            recipe.targetLocal = sockets.body * unit;
            recipe.idle.seed = seed;
            recipe.stoneOchre = vocabulary.palette.stoneOchre;
            recipe.neckLocal = sockets.neck * unit;
            if (channels.side == LookSide.Plant)
            {
                recipe.roots = Roots(channels.reach, vocabulary);
                Arms(recipe, sockets.neck, vocabulary.armCount, unit, vocabulary.palette.plantStem,
                    vocabulary.palette.Accent(channels.accent));
            }
            else
            {
                recipe.roots.count = 0;
                recipe.idle.swayDegrees = 0.6f;
                recipe.idle.breathAmount = 0.01f;
                recipe.wiltColour = vocabulary.stoneWilt;
                recipe.sourceLocal = new Vector3[] { sockets.neck * unit };
            }

            if (!CreatureValidator.TryValidate(recipe, out string error))
            {
                Debug.LogError($"[LookComposer] {recipe.name}: {error}");
                Object.DestroyImmediate(recipe);
                return null;
            }
            return recipe;
        }

        // How far the accessory reaches past the body and head on the board camera's screen plane, in cells
        public static float AccessoryReach(UnitChannels channels, LookVocabulary vocabulary)
        {
            if (channels.accessory == AccessoryKind.None || !HasEntries(channels, vocabulary))
            {
                return 0f;
            }

            PartList parts = Parts(channels, vocabulary, Copies(channels.count), Seed(channels), out _);
            return OutlineReach(parts, vocabulary.Unit(channels.side));
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

        // The sockets of a unit's body in body units, where its head, accessory and effects attach
        public static Sockets Place(UnitChannels channels, LookVocabulary vocabulary)
        {
            LookVocabulary.BodyEntry body = vocabulary.bodies[channels.mass];
            LookVocabulary.StemEntry stem = vocabulary.stems[channels.stem];
            bool isPlant = channels.side == LookSide.Plant;
            LookPart[] bodyParts = isPlant ? body.plant : body.stone;
            float stoneScale = isPlant ? 1f : vocabulary.stoneScale;
            Sockets sockets = new Sockets();
            sockets.scale = body.scale * stoneScale;
            sockets.bodyRadius = bodyParts[0].size.x * 0.5f * stoneScale;
            if (isPlant)
            {
                // The sphere sinks a little into the ground, the body's centre sits at 0.84 of its radius
                sockets.body = Vector3.up * (0.84f * sockets.bodyRadius);
                sockets.neck = sockets.body + Vector3.up * (sockets.bodyRadius * 0.8f + stem.length);
            }
            else
            {
                // Stones stand on boulder limbs, the stem band is the limb length
                sockets.body = Vector3.up * (stem.limbLength * sockets.scale + sockets.bodyRadius * 0.8f);
                sockets.neck = sockets.body + Vector3.up * (sockets.bodyRadius * 0.75f);
            }

            sockets.hip = sockets.body;
            return sockets;
        }

        // Where an accessory socket sits, every one on the unit's right so the accessory stands on lit grass
        public static Vector3 Socket(AccessorySocket socket, Sockets sockets)
        {
            float radius = sockets.bodyRadius;
            switch (socket)
            {
                case AccessorySocket.NeckOrbit:
                case AccessorySocket.Crook:
                    return sockets.neck;
                case AccessorySocket.Shoulder:
                    return sockets.body + new Vector3(radius * 0.7f, radius * 0.7f, 0f);
                case AccessorySocket.Flank:
                    return sockets.hip + Vector3.right * radius;
                default:
                    return sockets.hip;
            }
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

        static PartList Parts(UnitChannels channels, LookVocabulary vocabulary, int copies, int seed, out Sockets sockets)
        {
            PartList parts = new PartList(vocabulary.Unit(channels.side));
            sockets = Place(channels, vocabulary);
            LookVocabulary.BodyEntry body = vocabulary.bodies[channels.mass];
            LookVocabulary.StemEntry stem = vocabulary.stems[channels.stem];
            bool isPlant = channels.side == LookSide.Plant;
            float scale = sockets.scale;
            Color stemColour = vocabulary.Colour(ColourRole.Stem, channels.accent, channels.side);
            if (isPlant)
            {
                Fragment(parts, vocabulary, channels, body.plant, sockets.body, 1f, CountBand.One, seed);
                Vector3 stemFoot = sockets.body + Vector3.up * (sockets.bodyRadius * 0.8f);
                parts.Link("Stem", stemFoot, sockets.neck, stem.thickness, stemColour, PartRole.Stem);
            }
            else
            {
                Fragment(parts, vocabulary, channels, body.stone, sockets.body, vocabulary.stoneScale, CountBand.One, seed);
                Limbs(parts, stem.limbLength * scale, sockets.bodyRadius, scale, stemColour, seed);
            }

            LookVocabulary.HeadEntry head = vocabulary.heads[channels.head];
            LookPart[] headParts = isPlant ? head.plant : head.stone;
            if (head.carriesCount)
            {
                Fragment(parts, vocabulary, channels, headParts, sockets.neck, scale, channels.count, seed);
            }
            else if (copies == 1)
            {
                Fragment(parts, vocabulary, channels, headParts, sockets.neck, scale, CountBand.One, seed);
            }
            else
            {
                // Three or five smaller heads on a branching neck, a stone carries them side by side
                float copyScale = copies == 3 ? 0.72f : 0.55f;
                for (int i = 0; i < copies; i++)
                {
                    Vector3 end = Branch(parts, sockets.neck, i, copies, isPlant, scale, stemColour);
                    Fragment(parts, vocabulary, channels, headParts, end, copyScale * scale, CountBand.One, seed);
                }
            }

            if (channels.accessory != AccessoryKind.None)
            {
                parts.accessoryStart = parts.count;
                LookVocabulary.AccessoryEntry accessory = vocabulary.accessories[channels.accessory];
                Vector3 socket = Socket(accessory.socket, sockets);
                Fragment(parts, vocabulary, channels, isPlant ? accessory.plant : accessory.stone, socket, scale, CountBand.One, seed);
                if (channels.accessory == AccessoryKind.MiniHead)
                {
                    LookVocabulary.HeadEntry mini = vocabulary.heads[channels.accessoryHead];
                    Fragment(parts, vocabulary, channels, isPlant ? mini.plant : mini.stone, socket + accessory.miniHeadAt * scale,
                        accessory.miniHeadScale * scale, CountBand.One, seed);
                }
            }
            return parts;
        }

        // A fragment's parts around a socket, those its count band allows; a stone part takes a variant from the seed
        static void Fragment(PartList parts, LookVocabulary vocabulary, UnitChannels channels, LookPart[] fragment, Vector3 at,
            float scale, CountBand band, int seed)
        {
            foreach (LookPart part in fragment)
            {
                if (part.minCount > band)
                {
                    continue;
                }

                Color colour = vocabulary.Colour(part.colour, channels.accent, channels.side);
                parts.Add(part.id, part.primitive, at + part.position * scale, part.size * scale, colour, part.euler, part.glow,
                    part.role, Variant(seed, parts.count));
            }
        }

        // Each accessory part is sampled at its centre and its six face centres, the body and head as the ellipsoids in their boxes
        static float OutlineReach(PartList parts, float bodyUnit)
        {
            Vector3 right = boardCamera * Vector3.right;
            Vector3 up = boardCamera * Vector3.up;
            float reach = 0f;
            for (int i = Mathf.Max(0, parts.accessoryStart); i < parts.count; i++)
            {
                LookPart part = parts.Source(i);
                Quaternion rotation = Quaternion.Euler(part.euler);
                for (int k = 0; k < 7; k++)
                {
                    Vector3 point = part.position;
                    if (k > 0)
                    {
                        int axis = (k - 1) / 2;
                        Vector3 offset = Vector3.zero;
                        offset[axis] = (k % 2 == 0 ? -0.5f : 0.5f) * part.size[axis];
                        point += rotation * offset;
                    }

                    Vector2 screen = new Vector2(Vector3.Dot(point, right), Vector3.Dot(point, up));
                    float nearest = float.MaxValue;
                    for (int j = 0; j < parts.accessoryStart; j++)
                    {
                        nearest = Mathf.Min(nearest, Outside(parts.Source(j), screen, right, up));
                    }

                    reach = Mathf.Max(reach, nearest);
                }
            }
            return reach * bodyUnit;
        }

        // How far a screen point lies outside a part's screen rectangle
        static float Outside(LookPart part, Vector2 point, Vector3 right, Vector3 up)
        {
            Quaternion inverse = Quaternion.Inverse(Quaternion.Euler(part.euler));
            Vector3 half = part.size * 0.5f;
            float halfX = Extent(half, inverse * right);
            float halfY = Extent(half, inverse * up);
            float dx = Mathf.Max(0f, Mathf.Abs(point.x - Vector3.Dot(part.position, right)) - halfX);
            float dy = Mathf.Max(0f, Mathf.Abs(point.y - Vector3.Dot(part.position, up)) - halfY);
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        static float Extent(Vector3 half, Vector3 direction)
        {
            Vector3 scaled = Vector3.Scale(half, direction);
            return scaled.magnitude;
        }

        // Two boulder legs under a stone body, from the ground up into it
        static void Limbs(PartList parts, float limb, float bodyRadius, float scale, Color colour, int seed)
        {
            float height = limb + bodyRadius * 0.5f;
            for (int i = -1; i <= 1; i += 2)
            {
                Vector3 foot = new Vector3(0.36f * i * scale, height * 0.5f, 0.05f);
                parts.Add("Limb", Primitive.Stone, foot, new Vector3(0.42f * scale, height, 0.45f * scale), colour,
                    new Vector3(0f, 25f * i, 0f), 0f, PartRole.Limb, Variant(seed, parts.count));
            }
        }

        // One branch of a fanned neck, returns where its head sits
        static Vector3 Branch(PartList parts, Vector3 top, int index, int copies, bool isPlant, float scale, Color colour)
        {
            float angle = (index - (copies - 1) * 0.5f) * (copies == 3 ? 40f : 28f);
            Vector3 direction = Quaternion.Euler(0f, 0f, -angle) * Vector3.up;
            Vector3 end = top + direction * ((isPlant ? 0.5f : 0.35f) * scale);
            if (isPlant)
            {
                parts.Link("Branch", top, end, 0.12f * scale, colour, PartRole.Stem);
            }
            else
            {
                end.y = top.y;
            }
            return end;
        }

        // Lianas from the neck: four coils of 48 half-cell links reach across the 16-cell board
        static void Arms(CreatureRecipe recipe, Vector3 neck, int armCount, float bodyUnit, Color colour, Color accent)
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
                    colour = colour,
                    tipColour = accent
                };
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
