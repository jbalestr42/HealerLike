using UnityEngine;

namespace HealerLike.Render.Grass
{
    // The air over the grass: a prevailing lean with gust fronts rolling along it and a slower cross flutter,
    // plus the cosmetic gust pulses. Lean mirrors HLGroundWindLean in GroundCommon.hlsl.
    public static class GroundWind
    {
        // Along the tufts' rest heading, a little off axis so the fronts cross the board's rows
        public static readonly Vector2 Direction = new Vector2(0.3f, 1f).normalized;
        // The lean full-strength wind adds, in radians
        public static readonly float MaxStrength = 0.12f;
        // The lean a full EnvironmentGust pulse adds, in radians
        public static readonly float GustLean = 0.55f;

        // xy direction, z strength in radians, w time in seconds: _HLGroundWind
        public static Vector4 Shader(float strength, float time)
        {
            float lean = MaxStrength * Mathf.Clamp01(RenderMath.FiniteOr(strength, 0f));
            return new Vector4(Direction.x, Direction.y, lean, RenderMath.FiniteOr(time, 0f));
        }

        // A gust pulse in world space, at most a unit long, as the lean it adds on XZ
        public static Vector2 Gust(Vector3 pulse)
        {
            if (!RenderMath.IsFinite(pulse))
            {
                return Vector2.zero;
            }

            return Vector2.ClampMagnitude(new Vector2(pulse.x, pulse.z), 1f) * GustLean;
        }

        public static Vector2 Lean(Vector2 point, Vector4 wind, Vector2 gust)
        {
            Vector2 direction = new Vector2(wind.x, wind.y);
            Vector2 across = new Vector2(-direction.y, direction.x);
            float time = wind.w;
            float along = Vector2.Dot(point, direction);
            float side = Vector2.Dot(point, across);
            float front = Mathf.Sin(along * 0.9f - time * 1.7f + 1.3f * Mathf.Sin(side * 0.6f + time * 0.4f));
            float flutter = Mathf.Sin(Vector2.Dot(point, new Vector2(0.67f, 0.43f)) * 1.3f + time * 1.1f);
            return (direction * (0.55f + 0.45f * front) + across * (0.3f * flutter)) * wind.z + gust;
        }
    }
}
