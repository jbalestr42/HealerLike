using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Grammar;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Creatures
{
    // Builds the recipe of a unit from its channels and the vocabulary asset at spawn, in memory and never saved
    public static class LookComposer
    {
        // How far past the body and head an accessory must reach on screen, in cells; stones need more to tell a
        // group apart
        public static readonly float PlantAccessoryReach = 0.25f;
        public static readonly float StoneAccessoryReach = 0.3f;
        // The longest branch of a fanned neck, in body units
        static readonly float maxBranch = 1.2f;
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

            // The first part of each head copy, the head runs from the first of them to the accessory
            readonly List<int> _headStarts = new List<int>();
            public List<int> headStarts { get { return _headStarts; } }

            public PartList(float unit)
            {
                _unit = unit;
            }

            // Size is the part's bounding box in body units, whatever the mesh's own pivot
            public int Add(string id, Primitive primitive, Vector3 centre, Vector3 size, Color colour, Vector3 euler,
                float glow, PartRole role, int variant = 0)
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
                // The first part is the body, every other part hangs from it
                int parent = 0;
                Vector3 local = pivot - origin;
                if (_parts.Count == 0)
                {
                    parent = -1;
                    local = pivot;
                }

                _parts.Add(new CreaturePart
                {
                    id = uniqueId,
                    parent = parent,
                    primitive = primitive,
                    localPosition = local * _unit,
                    localEuler = euler,
                    dimensions = dimensions * _unit,
                    colour = colour,
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
                Vector3 size = new Vector3(thickness, delta.magnitude + thickness, thickness);
                return Add(id, Primitive.Capsule, (from + to) * 0.5f, size, colour, euler, 0f, role);
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
                    Debug.LogError($"[LookComposer] {recipe.name}: the {channels.accessory} reaches {reach:0.00} cell"
                        + $" past the outline, under {needed}.");
                }
            }

            recipe.parts = parts.ToArray();
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
            out Sockets sockets)
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
                float copyScale = copies == 3 ? 0.72f : 0.55f;
                float spread = copies == 3 ? 40f : 28f;
                float length = 0.35f;
                if (isPlant)
                {
                    copyScale = CopyScale(headParts, copyScale, spread);
                    length = BranchLength(headParts, copyScale, spread);
                }
                for (int i = 0; i < copies; i++)
                {
                    parts.headStarts.Add(parts.count);
                    Vector3 end = Branch(parts, sockets.neck, i, copies, spread, length, isPlant, scale, stemColour);
                    Fragment(parts, vocabulary, channels, headParts, end, copyScale * scale, CountBand.One, seed);
                }
            }

            if (channels.accessory != AccessoryKind.None)
            {
                parts.accessoryStart = parts.count;
                LookVocabulary.AccessoryEntry accessory = vocabulary.accessories[channels.accessory];
                Vector3 socket = Socket(accessory.socket, sockets);
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

        // The head's screen box on the board camera, the smaller of its width and height in cells; a fanned head is
        // measured one copy at a time and the smallest copy counts
        public static float HeadSpan(UnitChannels channels, LookVocabulary vocabulary)
        {
            if (!HasEntries(channels, vocabulary))
            {
                return 0f;
            }

            PartList parts = Parts(channels, vocabulary, Copies(channels.count), Seed(channels), out _);
            float span = float.MaxValue;
            for (int i = 0; i < parts.headStarts.Count; i++)
            {
                Rect box = ScreenBox(parts, parts.headStarts[i], HeadEnd(parts, i));
                span = Mathf.Min(span, Mathf.Min(box.width, box.height));
            }
            return span * vocabulary.Unit(channels.side);
        }

        // The smallest screen gap between two neighbouring head copies in cells, negative when their boxes overlap
        public static float HeadGap(UnitChannels channels, LookVocabulary vocabulary)
        {
            if (!HasEntries(channels, vocabulary))
            {
                return 0f;
            }

            PartList parts = Parts(channels, vocabulary, Copies(channels.count), Seed(channels), out _);
            float gap = float.MaxValue;
            for (int i = 1; i < parts.headStarts.Count; i++)
            {
                Rect previous = ScreenBox(parts, parts.headStarts[i - 1], HeadEnd(parts, i - 1));
                Rect box = ScreenBox(parts, parts.headStarts[i], HeadEnd(parts, i));
                float gapX = Mathf.Max(box.xMin - previous.xMax, previous.xMin - box.xMax);
                float gapY = Mathf.Max(box.yMin - previous.yMax, previous.yMin - box.yMax);
                gap = Mathf.Min(gap, Mathf.Max(gapX, gapY));
            }
            return gap * vocabulary.Unit(channels.side);
        }

        // Where head copy i ends: the next copy's branch, the accessory, or the last part
        static int HeadEnd(PartList parts, int copy)
        {
            if (copy + 1 < parts.headStarts.Count)
            {
                return parts.headStarts[copy + 1];
            }
            if (parts.accessoryStart >= 0)
            {
                return parts.accessoryStart;
            }
            return parts.count;
        }

        // The screen box of parts start to end on the board camera, in body units; a fanned copy's branch is skipped
        static Rect ScreenBox(PartList parts, int start, int end)
        {
            Vector3 right = boardCamera * Vector3.right;
            Vector3 up = boardCamera * Vector3.up;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            for (int i = start; i < end; i++)
            {
                LookPart part = parts.Source(i);
                if (part.id == "Branch")
                {
                    continue;
                }

                Quaternion inverse = Quaternion.Inverse(Quaternion.Euler(part.euler));
                Vector3 half = part.size * 0.5f;
                Vector2 centre = new Vector2(Vector3.Dot(part.position, right), Vector3.Dot(part.position, up));
                Vector2 extent = new Vector2(Extent(half, inverse * right), Extent(half, inverse * up));
                min = Vector2.Min(min, centre - extent);
                max = Vector2.Max(max, centre + extent);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        // Each accessory part is sampled at its centre and its six face centres, the body and head as the ellipsoids
        // in their boxes
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
                        float face = 0.5f;
                        if (k % 2 == 0)
                        {
                            face = -0.5f;
                        }
                        offset[axis] = face * part.size[axis];
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
        static Vector3 Branch(PartList parts, Vector3 top, int index, int copies, float spread, float length,
            bool isPlant, float scale, Color colour)
        {
            float angle = (index - (copies - 1) * 0.5f) * spread;
            Vector3 direction = Quaternion.Euler(0f, 0f, -angle) * Vector3.up;
            Vector3 end = top + direction * (length * scale);
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

        // A fanned head is scaled down when its branches, at their longest, could not keep it clear of its neighbour
        static float CopyScale(LookPart[] head, float copyScale, float spread)
        {
            float fit = maxBranch * Chord(spread) / (HeadWidth(head) * 1.2f);
            return Mathf.Min(copyScale, fit);
        }

        // How long a fanned branch must be for two neighbouring heads to clear each other on screen with a fifth of
        // a head to spare
        static float BranchLength(LookPart[] head, float copyScale, float spread)
        {
            float length = HeadWidth(head) * copyScale * 1.2f / Chord(spread);
            return Mathf.Clamp(length, 0.5f, maxBranch);
        }

        // The screen distance between two neighbouring branch ends per unit of branch length; the outer pairs lean,
        // which the board camera shortens to about 0.85
        static float Chord(float spread)
        {
            return 2f * Mathf.Sin(spread * 0.5f * Mathf.Deg2Rad) * 0.85f;
        }

        // A head's width across the screen at unit scale
        static float HeadWidth(LookPart[] head)
        {
            float width = 0f;
            foreach (LookPart part in head)
            {
                Quaternion inverse = Quaternion.Inverse(Quaternion.Euler(part.euler));
                float half = Extent(part.size * 0.5f, inverse * Vector3.right);
                width = Mathf.Max(width, 2f * (Mathf.Abs(part.position.x) + half));
            }
            return width;
        }

        // Lianas from the neck: four coils of 48 half-cell links reach across the 16-cell board
        static void Arms(CreatureRecipe recipe, Vector3 neck, int armCount, float bodyUnit, Color colour, Color accent)
        {
            Vector3 bodyPivot = recipe.parts[0].localPosition;
            recipe.sourceLocal = new Vector3[armCount];
            recipe.arms = new ArmDefinition[armCount];
            for (int j = 0; j < armCount; j++)
            {
                // The arms leave the neck on either side, the first on the left
                float side = 0.15f;
                if (j % 2 == 0)
                {
                    side = -0.15f;
                }
                recipe.sourceLocal[j] = (neck + Vector3.right * side) * bodyUnit;
                recipe.arms[j] = Arm(recipe.sourceLocal[j] - bodyPivot, colour, accent);
            }
        }

        // One liana at rest: four coils of 48 half-cell links from its root on the body
        public static ArmDefinition Arm(Vector3 rootLocal, Color colour, Color tipColour)
        {
            Vector3[] rest = new Vector3[49];
            for (int i = 0; i < 48; i++)
            {
                float angle = i * Mathf.PI * 2f / 12f;
                rest[i + 1] = rest[i] + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0.015f).normalized * 0.5f;
            }

            return new ArmDefinition
            {
                bodyPart = 0,
                rootLocal = rootLocal,
                segmentCount = 48,
                segmentLength = 0.5f,
                radius = 0.045f,
                restJoints = rest,
                bendPole = Vector3.up,
                colour = colour,
                tipColour = tipColour
            };
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
