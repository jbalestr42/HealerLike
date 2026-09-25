using System;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // Stored in recipes and vocabulary assets. Zero preserves the existing baked primitive.
    public enum ShapeKind { Legacy, Bulb, Segment, Leaf, Block, Shard, Ring }

    [Serializable]
    public struct ShapeProfile : IEquatable<ShapeProfile>
    {
        public ShapeKind kind;
        public int radialSegments;
        public int lengthSegments;
        public float fullness;
        public float taper;
        // The long axis is Y; positive bend moves the upper end toward local +X.
        public float bend;
        public float bevel;
        public float asymmetry;
        public float tubeRatio;
        public bool faceted;

        public bool isProcedural => kind != ShapeKind.Legacy;

        static ShapeProfile Defaults(ShapeKind kind)
        {
            return new ShapeProfile
            {
                kind = kind, radialSegments = 12, lengthSegments = 10,
                fullness = 1f, bevel = 0.18f, tubeRatio = 0.2f
            };
        }

        public static ShapeProfile Bulb(float fullness = 1f, float taper = 0f)
        {
            ShapeProfile shape = Defaults(ShapeKind.Bulb);
            shape.fullness = fullness;
            shape.taper = taper;
            return shape;
        }

        public static ShapeProfile Segment(float taper = 0.35f, float fullness = 0.22f, float bend = 0f)
        {
            ShapeProfile shape = Defaults(ShapeKind.Segment);
            shape.taper = taper;
            shape.fullness = fullness;
            shape.bend = bend;
            shape.lengthSegments = 6;
            return shape;
        }

        public static ShapeProfile Leaf(float bend = 0.3f, float fullness = 0.8f)
        {
            ShapeProfile shape = Defaults(ShapeKind.Leaf);
            shape.bend = bend;
            shape.fullness = fullness;
            shape.taper = 0.35f;
            return shape;
        }

        public static ShapeProfile Block(float bevel = 0.18f, float taper = 0.08f, float asymmetry = 0.06f)
        {
            ShapeProfile shape = Defaults(ShapeKind.Block);
            shape.bevel = bevel;
            shape.taper = taper;
            shape.asymmetry = asymmetry;
            shape.faceted = true;
            return shape;
        }

        public static ShapeProfile Shard(float taper = 0.9f, float bend = 0.12f)
        {
            ShapeProfile shape = Block(0.16f, taper, 0.05f);
            shape.kind = ShapeKind.Shard;
            shape.bend = bend;
            return shape;
        }

        public static ShapeProfile Ring(float tubeRatio = 0.2f, bool faceted = false)
        {
            ShapeProfile shape = Defaults(ShapeKind.Ring);
            shape.tubeRatio = tubeRatio;
            shape.faceted = faceted;
            shape.radialSegments = faceted ? 8 : 16;
            shape.lengthSegments = faceted ? 4 : 8;
            return shape;
        }

        public bool IsValid()
        {
            if (kind == ShapeKind.Legacy)
            {
                return true;
            }
            return kind >= ShapeKind.Bulb && kind <= ShapeKind.Ring
                && radialSegments >= 6 && radialSegments <= 32
                && lengthSegments >= 4 && lengthSegments <= 24
                && Range(fullness, 0.05f, 3f) && Range(taper, -0.8f, 0.95f)
                && Range(bend, -1f, 1f) && Range(bevel, 0.02f, 0.4f)
                && Range(asymmetry, 0f, 0.15f) && Range(tubeRatio, 0.06f, 0.45f);
        }

        static bool Range(float value, float low, float high)
        {
            return float.IsFinite(value) && value >= low && value <= high;
        }

        public bool Equals(ShapeProfile other)
        {
            return kind == other.kind && radialSegments == other.radialSegments
                && lengthSegments == other.lengthSegments && fullness.Equals(other.fullness)
                && taper.Equals(other.taper) && bend.Equals(other.bend) && bevel.Equals(other.bevel)
                && asymmetry.Equals(other.asymmetry) && tubeRatio.Equals(other.tubeRatio)
                && faceted == other.faceted;
        }

        public override bool Equals(object obj) => obj is ShapeProfile other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)kind;
                hash = hash * 397 ^ radialSegments;
                hash = hash * 397 ^ lengthSegments;
                hash = hash * 397 ^ fullness.GetHashCode();
                hash = hash * 397 ^ taper.GetHashCode();
                hash = hash * 397 ^ bend.GetHashCode();
                hash = hash * 397 ^ bevel.GetHashCode();
                hash = hash * 397 ^ asymmetry.GetHashCode();
                hash = hash * 397 ^ tubeRatio.GetHashCode();
                return hash * 397 ^ faceted.GetHashCode();
            }
        }
    }
}
