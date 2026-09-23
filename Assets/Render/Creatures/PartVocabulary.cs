using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{
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

        public PartList(float unit)
        {
            _unit = unit;
        }

        // Size is the part's bounding box in body units, whatever the mesh's own pivot
        public int Add(string id, Primitive primitive, Vector3 centre, Vector3 size, Color colour, Vector3 euler = default,
            float glow = 0f, PartRole role = PartRole.Body)
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
            if (primitive == Primitive.Boulder)
            {
                // The baked boulder spans two units across and 1.7 from -0.7 to 1
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
                role = role
            });
            _positions.Add(pivot);
            return _parts.Count - 1;
        }

        // A capsule from one point to another
        public int Link(string id, Vector3 from, Vector3 to, float thickness, Color colour, PartRole role = PartRole.Stem)
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

        public Color Colour(int index)
        {
            return _parts[index].colour;
        }

        public CreaturePart[] ToArray()
        {
            return _parts.ToArray();
        }
    }

    // The heads and accessories of the look grammar, each as a plant part and a stone part
    public static class PartVocabulary
    {
        public static readonly Color PlantBody = new Color(0.50f, 0.79f, 0.25f);
        public static readonly Color PlantStem = new Color(0.18f, 0.49f, 0.31f);
        public static readonly Color StoneBody = new Color(0.56f, 0.58f, 0.63f);
        public static readonly Color StoneLimb = new Color(0.45f, 0.48f, 0.55f);
        public static readonly Color Moss = new Color(0.47f, 0.72f, 0.30f);
        public static readonly float TipGlow = 0.6f;

        // The study's accents, Rot's violet is proposed and Bane takes the hostile spike's shade
        public static Color Accent(EffectFamily family)
        {
            switch (family)
            {
                case EffectFamily.Heal:
                case EffectFamily.Renew:
                    return Hex(0xd6f84c);
                case EffectFamily.Rot:
                    return Hex(0x8e5bd6);
                case EffectFamily.Boon:
                    return Hex(0xdfaf34);
                case EffectFamily.Bane:
                    return Hex(0x203b64);
                default:
                    return Hex(0xf6969a);
            }
        }

        public static Color Body(LookSide side)
        {
            return side == LookSide.Plant ? PlantBody : StoneBody;
        }

        // One head at a point, the arch and the cairn carry their count themselves
        public static void Head(PartList parts, HeadKind kind, LookSide side, Vector3 at, float scale, int pods, Color accent)
        {
            Color body = Body(side);
            bool isPlant = side == LookSide.Plant;
            switch (kind)
            {
                case HeadKind.Spear:
                    if (isPlant)
                    {
                        parts.Add("Spear", Primitive.Cone, at + Vector3.up * (0.5f * scale), new Vector3(0.3f, 1f, 0.3f) * scale, body);
                    }
                    else
                    {
                        parts.Add("Spire", Primitive.Pyramid, at + Vector3.up * (0.55f * scale), new Vector3(0.45f, 1.1f, 0.45f) * scale, body);
                    }

                    Tip(parts, at + Vector3.up * (isPlant ? 1f : 1.1f) * scale, 0.16f * scale, accent);
                    break;
                case HeadKind.Arch:
                    if (isPlant)
                    {
                        Arch(parts, at, scale, pods, accent);
                    }
                    else
                    {
                        Cairn(parts, at, scale, pods, accent);
                    }
                    break;
                case HeadKind.Conductor:
                    Vector3 stack = at;
                    float[] sizes = { 0.55f, 0.42f, 0.3f };
                    for (int i = 0; i < sizes.Length; i++)
                    {
                        float size = sizes[i] * scale;
                        stack += Vector3.up * (size * 0.5f);
                        if (i == sizes.Length - 1)
                        {
                            Tip(parts, stack, size, accent);
                        }
                        else
                        {
                            Primitive primitive = isPlant ? Primitive.Sphere : Primitive.Boulder;
                            parts.Add("Stack", primitive, stack, Vector3.one * size, body);
                        }
                        stack += Vector3.up * (size * 0.4f);
                    }

                    parts.Add("Collar", Primitive.Torus, at + Vector3.up * (0.3f * scale), new Vector3(0.95f, 0.22f, 0.95f) * scale,
                        isPlant ? PlantStem : StoneLimb);
                    break;
                case HeadKind.Fork:
                    for (int i = -1; i <= 1; i += 2)
                    {
                        Vector3 bulb = at + new Vector3(0.24f * i, 0.35f, 0f) * scale;
                        Primitive primitive = isPlant ? Primitive.Sphere : Primitive.Boulder;
                        Vector3 size = isPlant ? new Vector3(0.28f, 0.6f, 0.34f) : new Vector3(0.3f, 0.8f, 0.45f);
                        parts.Add("Prong", primitive, bulb, size * scale, body);
                        Tip(parts, bulb + Vector3.up * (0.32f * scale), 0.14f * scale, accent);
                    }
                    break;
                case HeadKind.GiftHeal:
                    if (!isPlant)
                    {
                        parts.Add("Mound", Primitive.Boulder, at + Vector3.up * (0.15f * scale), new Vector3(0.6f, 0.35f, 0.6f) * scale, body);
                    }

                    for (int i = 0; i < 3; i++)
                    {
                        float angle = i * Mathf.PI * 2f / 3f;
                        Vector3 end = at + new Vector3(Mathf.Cos(angle) * 0.3f, 0.6f, Mathf.Sin(angle) * 0.3f) * scale;
                        parts.Link("Stalk", at, end, 0.06f * scale, isPlant ? PlantStem : Moss);
                        Tip(parts, end, 0.24f * scale, accent);
                    }
                    break;
                case HeadKind.GiftBoonDefence:
                case HeadKind.Ward:
                    bool isClosed = kind == HeadKind.Ward;
                    int plates = isClosed ? 5 : 4;
                    Vector3 centre = isClosed ? at + Vector3.down * (0.6f * scale) : at;
                    for (int i = 0; i < plates; i++)
                    {
                        float angle = i * 360f / plates;
                        Quaternion around = Quaternion.Euler(0f, angle, 0f);
                        Vector3 offset = around * new Vector3(isClosed ? 0.42f : 0.22f, 0.45f, 0f) * scale;
                        Vector3 euler = (around * Quaternion.Euler(0f, 0f, isClosed ? 8f : -35f)).eulerAngles;
                        Primitive primitive = isPlant ? Primitive.Leaf : Primitive.Boulder;
                        Vector3 size = isPlant ? new Vector3(0.1f, 0.9f, 0.4f) : new Vector3(0.18f, 0.9f, 0.45f);
                        parts.Add("Plate", primitive, centre + offset, size * scale, body, euler);
                    }

                    Tip(parts, at + Vector3.up * (0.35f * scale), 0.28f * scale, accent);
                    break;
                case HeadKind.GiftBoonOffence:
                    parts.Add("Crown", Primitive.Torus, at + Vector3.up * (0.7f * scale), new Vector3(0.8f, 0.2f, 0.8f) * scale, accent,
                        default, TipGlow);
                    Tip(parts, at + Vector3.up * (0.25f * scale), 0.45f * scale, body);
                    break;
                case HeadKind.GiftBane:
                    parts.Add("Core", isPlant ? Primitive.Sphere : Primitive.Boulder, at + Vector3.up * (0.3f * scale),
                        Vector3.one * (0.45f * scale), body);
                    for (int i = 0; i < 5; i++)
                    {
                        float angle = i * Mathf.PI * 2f / 5f;
                        Vector3 point = at + new Vector3(Mathf.Cos(angle) * 0.36f, 0.35f, Mathf.Sin(angle) * 0.36f) * scale;
                        Vector3 size = new Vector3(0.14f, 0.45f, 0.14f) * scale;
                        Vector3 euler = new Vector3(0f, 0f, 180f);
                        parts.Add("Barb", isPlant ? Primitive.Cone : Primitive.Pyramid, point, size, accent, euler, TipGlow * 0.5f);
                    }
                    break;
                case HeadKind.Pulse:
                    parts.Add("Cap", isPlant ? Primitive.Sphere : Primitive.Boulder, at + Vector3.up * (0.12f * scale),
                        new Vector3(1.25f, 0.28f, 1.25f) * scale, body);
                    Tip(parts, at + Vector3.up * (0.3f * scale), 0.22f * scale, accent);
                    break;
                case HeadKind.SelfTick:
                    for (int i = 0; i < 7; i++)
                    {
                        float angle = i * 0.9f;
                        float radius = 0.32f * (1f - i * 0.1f);
                        Vector3 point = at + new Vector3(Mathf.Sin(angle) * radius, 0.1f + i * 0.13f, Mathf.Cos(angle) * radius * 0.4f) * scale;
                        float size = (0.3f - i * 0.025f) * scale;
                        if (i == 6)
                        {
                            Tip(parts, point, size, accent);
                        }
                        else
                        {
                            parts.Add("Curl", isPlant ? Primitive.Sphere : Primitive.Boulder, point, Vector3.one * size, body);
                        }
                    }
                    break;
                default:
                    parts.Add("Head", isPlant ? Primitive.Sphere : Primitive.Boulder, at + Vector3.up * (0.3f * scale),
                        Vector3.one * ((isPlant ? 0.6f : 0.75f) * scale), body);
                    Tip(parts, at + Vector3.up * (0.62f * scale), 0.26f * scale, accent);
                    break;
            }
        }

        // Where a mini head sits from its socket, and its scale
        public static readonly Vector3 MiniHeadAt = new Vector3(-0.35f, 0.4f, 0f);
        public static readonly float MiniHeadScale = 0.5f;
        // The body radius the accessories were drawn around, a sturdy plant's plus half the accessory reach
        static readonly float accessoryRadius = 0.725f;

        public static AccessorySocket Socket(AccessoryKind kind)
        {
            switch (kind)
            {
                case AccessoryKind.SmallTorus:
                case AccessoryKind.ConeCrown:
                    return AccessorySocket.NeckOrbit;
                case AccessoryKind.TierRings:
                case AccessoryKind.ThornCollar:
                    return AccessorySocket.HipOrbit;
                case AccessoryKind.DripBeads:
                case AccessoryKind.Hook:
                    return AccessorySocket.Crook;
                case AccessoryKind.TwinSeeds:
                case AccessoryKind.ShardBarbs:
                    return AccessorySocket.Flank;
                default:
                    return AccessorySocket.Shoulder;
            }
        }

        // The accessory around its socket, offsets fixed so the same parts fit every body; a mini head is added by the composer
        public static void Accessory(PartList parts, AccessoryKind kind, LookSide side, Vector3 socket, Color accent)
        {
            bool isPlant = side == LookSide.Plant;
            Color body = Body(side);
            Color stem = isPlant ? PlantStem : StoneLimb;
            PartRole role = PartRole.Accessory;
            float radius = accessoryRadius;
            switch (kind)
            {
                case AccessoryKind.MiniHead:
                    parts.Link("Offshoot", socket + new Vector3(radius * 0.4f, 0f, 0f), socket + MiniHeadAt, 0.12f, stem, role);
                    break;
                case AccessoryKind.Hook:
                    Vector3 bend = socket + new Vector3(-0.3f, -0.05f, 0f);
                    parts.Link("Hook", socket, bend, 0.1f, body, role);
                    parts.Add("Barb", isPlant ? Primitive.Cone : Primitive.Pyramid, bend + new Vector3(-0.1f, -0.15f, 0f),
                        new Vector3(0.14f, 0.4f, 0.14f), body, new Vector3(0f, 0f, 150f), 0f, role);
                    break;
                case AccessoryKind.Antenna:
                    Vector3 top = socket + new Vector3(-0.15f, 1.3f, 0f);
                    parts.Link("Antenna", socket, top, 0.06f, stem, role);
                    Tip(parts, top, 0.2f, accent);
                    break;
                case AccessoryKind.ThornCollar:
                    for (int i = 0; i < 6; i++)
                    {
                        float angle = i * 60f;
                        Quaternion around = Quaternion.Euler(0f, angle, 0f);
                        Vector3 point = socket + around * (Vector3.right * (radius + 0.12f));
                        Vector3 euler = (around * Quaternion.Euler(0f, 0f, -90f)).eulerAngles;
                        parts.Add("Thorn", isPlant ? Primitive.Cone : Primitive.Pyramid, point, new Vector3(0.12f, 0.36f, 0.12f), body,
                            euler, 0f, role);
                    }
                    break;
                case AccessoryKind.TierRings:
                    for (int i = 0; i < 3; i++)
                    {
                        float width = radius * 2.4f - i * 0.18f;
                        parts.Add("Tier", Primitive.Torus, socket + Vector3.up * (0.12f + i * 0.2f), new Vector3(width, 0.14f, width),
                            accent, default, TipGlow * 0.5f, role);
                    }
                    break;
                case AccessoryKind.TwinSeeds:
                    for (int i = -1; i <= 1; i += 2)
                    {
                        Vector3 seed = socket + new Vector3((radius + 0.2f) * i, -0.15f, 0.1f);
                        parts.Add("Seed", isPlant ? Primitive.Sphere : Primitive.Boulder, seed, Vector3.one * 0.26f, body, default, 0f,
                            role);
                    }
                    break;
                case AccessoryKind.StalkBeads:
                    Vector3 stalkTop = socket + new Vector3(-0.2f, 0.7f, 0f);
                    parts.Link("Stalk", socket, stalkTop, 0.05f, isPlant ? PlantStem : Moss, role);
                    for (int i = 0; i < 3; i++)
                    {
                        Tip(parts, Vector3.Lerp(socket, stalkTop, 0.45f + i * 0.27f), 0.14f, accent);
                    }
                    break;
                case AccessoryKind.SmallTorus:
                    parts.Add("Halo", Primitive.Torus, socket + Vector3.down * 0.1f, new Vector3(0.75f, 0.16f, 0.75f),
                        Accent(EffectFamily.Boon), new Vector3(18f, 0f, 12f), TipGlow, role);
                    break;
                case AccessoryKind.ConeCrown:
                    for (int i = 0; i < 4; i++)
                    {
                        float angle = i * Mathf.PI * 0.5f + 0.4f;
                        Vector3 point = socket + new Vector3(Mathf.Cos(angle) * 0.3f, -0.1f, Mathf.Sin(angle) * 0.3f);
                        parts.Add("Spike", isPlant ? Primitive.Cone : Primitive.Pyramid, point, new Vector3(0.12f, 0.34f, 0.12f),
                            Accent(EffectFamily.Bane), new Vector3(0f, 0f, 180f), TipGlow * 0.5f, role);
                    }
                    break;
                case AccessoryKind.DripBeads:
                    Vector3 arm = socket + new Vector3(-0.55f, 0.05f, 0f);
                    parts.Link("Dropper", socket, arm, 0.07f, stem, role);
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3 drop = arm + new Vector3(-0.05f * i, -0.2f - i * 0.2f, 0f);
                        parts.Add("Drip", Primitive.Sphere, drop, new Vector3(0.14f, 0.24f, 0.14f), Accent(EffectFamily.Rot),
                            default, TipGlow, role);
                    }
                    break;
                case AccessoryKind.ShardBarbs:
                    for (int i = 0; i < 3; i++)
                    {
                        Quaternion around = Quaternion.Euler(0f, 150f + i * 30f, 0f);
                        Vector3 point = socket + around * (Vector3.right * (radius + 0.1f));
                        Vector3 euler = (around * Quaternion.Euler(0f, 0f, -70f)).eulerAngles;
                        parts.Add("Shard", Primitive.Pyramid, point, new Vector3(0.16f, 0.42f, 0.16f), Accent(EffectFamily.Bane), euler,
                            TipGlow * 0.5f, role);
                    }
                    break;
            }
        }

        // The accent sits on the tip alone, lit so it separates from the body by an edge and not only by hue
        static void Tip(PartList parts, Vector3 at, float size, Color accent)
        {
            parts.Add("Bud", Primitive.Sphere, at, Vector3.one * size, accent, default, TipGlow, PartRole.Tip);
        }

        // A bent neck with 1, 3 or 5 pods hanging from it
        static void Arch(PartList parts, Vector3 at, float scale, int pods, Color accent)
        {
            Vector3 previous = at;
            Vector3[] points = new Vector3[6];
            pods = Mathf.Clamp(pods, 1, points.Length - 1);
            points[0] = at;
            for (int i = 1; i < points.Length; i++)
            {
                float angle = Mathf.PI - i * Mathf.PI / (points.Length - 1);
                points[i] = at + new Vector3(0.45f + Mathf.Cos(angle) * 0.45f, Mathf.Sin(angle) * 0.6f, 0f) * scale;
                parts.Link("Arch", previous, points[i], 0.13f * scale, PlantBody);
                previous = points[i];
            }

            for (int i = 0; i < pods; i++)
            {
                Vector3 hang = points[points.Length - pods + i];
                Vector3 pod = hang + Vector3.down * (0.32f * scale);
                parts.Link("PodStem", hang, pod, 0.04f * scale, PlantStem);
                Tip(parts, pod, 0.24f * scale, accent);
            }
        }

        // Five stones stacked into a cairn, the top two from three copies up, and one accent pebble per copy on their flanks
        static void Cairn(PartList parts, Vector3 at, float scale, int pods, Color accent)
        {
            Vector3 point = at;
            Vector3[] flanks = new Vector3[5];
            for (int i = 0; i < flanks.Length; i++)
            {
                float size = (0.7f - i * 0.1f) * scale;
                point += Vector3.up * (size * 0.22f);
                if (i < 3 || pods >= 3)
                {
                    parts.Add("Cairn", Primitive.Boulder, point, new Vector3(size, size * 0.45f, size), StoneBody,
                        new Vector3(0f, i * 40f, 0f), 0f, PartRole.Head);
                }

                flanks[i] = point + new Vector3(size * 0.5f, size * 0.1f, 0f);
                point += Vector3.up * (size * 0.22f);
            }

            // The third stone carries the one pebble, the upper two join at three, the lower two at five
            for (int i = 0; i < flanks.Length; i++)
            {
                bool isShown = i == 2 || (i > 2 && pods >= 3) || pods >= 5;
                if (isShown)
                {
                    Tip(parts, flanks[i], 0.2f * scale, accent);
                }
            }
        }

        static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);
        }
    }
}
