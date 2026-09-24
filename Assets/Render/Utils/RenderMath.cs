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
    }
}
