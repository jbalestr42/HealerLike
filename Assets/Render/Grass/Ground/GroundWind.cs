using UnityEngine;

namespace HealerLike.Render.Grass
{
    // The still air over the grass: a slight prevailing lean with soft swells along it and a slower cross flutter,
    // kept faint so the grass only moves clearly where something moves it. Lean mirrors HLGroundWindLean in
    // GroundCommon.hlsl.
    public static class GroundWind
    {
        // Along the tufts' rest heading, a little off axis so the fronts cross the board's rows
        public static readonly Vector2 Direction = new Vector2(0.3f, 1f).normalized;
        // The lean full-strength wind adds, in radians
        public static readonly float MaxStrength = 0.05f;

        // xy direction, z strength in radians, w time in seconds: _HLGroundWind
        public static Vector4 Shader(float strength, float time)
        {
            float lean = MaxStrength * Mathf.Clamp01(RenderMath.FiniteOr(strength, 0f));
            return new Vector4(Direction.x, Direction.y, lean, RenderMath.FiniteOr(time, 0f));
        }

        public static Vector2 Lean(Vector2 point, Vector4 wind)
        {
            Vector2 direction = new Vector2(wind.x, wind.y);
            Vector2 across = new Vector2(-direction.y, direction.x);
            float time = wind.w;
            float along = Vector2.Dot(point, direction);
            float side = Vector2.Dot(point, across);
            float front = Mathf.Sin(along * 0.9f - time * 1.7f + 1.3f * Mathf.Sin(side * 0.6f + time * 0.4f));
            float flutter = Mathf.Sin(Vector2.Dot(point, new Vector2(0.67f, 0.43f)) * 1.3f + time * 1.1f);
            return (direction * (0.55f + 0.45f * front) + across * (0.3f * flutter)) * wind.z;
        }
    }
}
