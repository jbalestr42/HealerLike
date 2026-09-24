using UnityEngine;

namespace HealerLike.Render
{
    // Guards for the numbers the render layer takes from gameplay and from assets
    public static class RenderMath
    {
        public static bool IsFinite(Vector3 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        }

        // A NaN or an infinity from an asset or a setter falls back to the given value
        public static float FiniteOr(float value, float fallback)
        {
            if (float.IsFinite(value))
            {
                return value;
            }

            return fallback;
        }

        // Finite and above zero, the guard for sizes, durations and cell sizes
        public static bool IsPositive(float value)
        {
            return float.IsFinite(value) && value > 0f;
        }

        // The sign of each axis for one of the eight corners of a box, bit 0 for x, 1 for y, 2 for z
        public static Vector3 CornerSign(int corner)
        {
            Vector3 sign = -Vector3.one;
            if ((corner & 1) != 0)
            {
                sign.x = 1f;
            }

            if ((corner & 2) != 0)
            {
                sign.y = 1f;
            }

            if ((corner & 4) != 0)
            {
                sign.z = 1f;
            }

            return sign;
        }

        public static Vector3 Corner(Bounds box, int corner)
        {
            return box.center + Vector3.Scale(box.extents, CornerSign(corner));
        }
    }
}
