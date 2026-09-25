using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{
    // Authoring helpers only. Every call remains one independently editable mesh in the saved vocabulary.
    public static class GrowthStoneParts
    {
        public static LookPart Part(string id, ShapeProfile shape, Vector3 at, Vector3 size,
            PartRole role = PartRole.Head, Vector3 euler = default, CountBand count = CountBand.One)
        {
            bool tip = role == PartRole.Tip;
            bool mineral = shape.kind == ShapeKind.Block || shape.kind == ShapeKind.Shard;
            if (mineral && shape.fracture == 0f)
            {
                shape.fracture = tip ? 0.52f : 0.74f;
            }
            return new LookPart
            {
                id = id, shape = shape, primitive = mineral ? Primitive.Stone : Primitive.Sphere,
                position = at, size = size, euler = euler, role = role, minCount = count,
                colour = tip ? ColourRole.Accent : ColourRole.Body, glow = tip ? 0.6f : 0f
            };
        }

        public static LookPart Link(string id, ShapeProfile shape, Vector3 from, Vector3 to, float width,
            PartRole role = PartRole.Head, CountBand count = CountBand.One)
        {
            Vector3 delta = to - from;
            return Part(id, shape, (from + to) * 0.5f, new Vector3(width, delta.magnitude, width), role,
                Quaternion.FromToRotation(Vector3.up, delta).eulerAngles, count);
        }

        // Store the attachment, rather than baking an offset that would drift after a profile edit.
        public static LookPart Anchored(string id, ShapeProfile shape, Vector3 baseAt, Vector3 size,
            Quaternion rotation, PartRole role = PartRole.Head)
        {
            LookPart part = Part(id, shape, baseAt, size, role, rotation.eulerAngles);
            part.pivot = ShapeAnchor.Bottom;
            return part;
        }

        public static LookPart Attached(string id, ShapeProfile shape, string parent, ShapeAnchor socket,
            Vector3 offset, Vector3 size, PartRole role)
        {
            LookPart part = Part(id, shape, offset, size, role);
            part.pivot = ShapeAnchor.Bottom;
            part.attachTo = parent;
            part.attachAt = socket;
            return part;
        }

        public static void Joint(List<LookPart> parts, Vector3 at, float size, CountBand count = CountBand.One)
        {
            parts.Add(Part("GrowthJoint", ShapeProfile.Bulb(), at, Vector3.one * size, count: count));
        }

        public static void Growth(List<LookPart> parts, Vector3 from, Vector3 to, float width,
            CountBand count = CountBand.One)
        {
            parts.Add(Link("Growth", ShapeProfile.Segment(0.16f, 0.78f), from, to, width, count: count));
            Joint(parts, to, width * 0.72f, count);
        }
    }
}
