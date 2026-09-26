using UnityEngine;

namespace HealerLike.Render.Environment
{
    // Short cosmetic wind pushes along a launch, felt only near its path: a plant within Near of the path from
    // shooter to target bends fully, one past Reach not at all. The stage sends the path; nothing here polls
    // gameplay.
    public class EnvironmentGust : MonoBehaviour
    {
        public static readonly int Capacity = 8;
        // In world units from the launch's path: full push within Near, none past Reach
        public static readonly float Near = 0.5f;
        public static readonly float Reach = 2f;

        struct Pulse
        {
            public Vector3 from;
            public Vector3 to;
            public Vector3 direction;
            public float strength;
            public float seconds;
            public double start;
        }

        readonly Pulse[] _pulses = new Pulse[Capacity];
        int _next;

        // Starts now on the game clock, which Sample reads, blowing from from toward to
        public void Gust(Vector3 from, Vector3 to, float strength, float seconds)
        {
            bool isPulseValid = RenderMath.IsPositive(strength) && RenderMath.IsPositive(seconds);
            if (!RenderMath.IsFinite(from) || !RenderMath.IsFinite(to) || !isPulseValid)
            {
                return;
            }

            Vector3 direction = to - from;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.000001f || !float.IsFinite(direction.sqrMagnitude))
            {
                return;
            }

            _pulses[_next] = new Pulse
            {
                from = from,
                to = to,
                direction = direction.normalized,
                strength = Mathf.Clamp01(strength),
                seconds = Mathf.Min(seconds, 10f),
                start = Time.timeAsDouble
            };
            _next = (_next + 1) % Capacity;
        }

        // The push a plant standing at position feels at time
        public Vector3 Sample(double time, Vector3 position)
        {
            if (!double.IsFinite(time) || !RenderMath.IsFinite(position))
            {
                return Vector3.zero;
            }

            Vector3 sum = Vector3.zero;
            for (int i = 0; i < Capacity; i++)
            {
                Pulse pulse = _pulses[i];
                double age = time - pulse.start;
                if (pulse.seconds <= 0f || age <= 0 || age >= pulse.seconds)
                {
                    continue;
                }

                float nearness = 1f - Mathf.Clamp01((DistanceToPath(position, pulse) - Near) / (Reach - Near));
                float envelope = Mathf.Sin((float)(age / pulse.seconds) * Mathf.PI);
                sum += pulse.direction * (pulse.strength * envelope * nearness);
            }

            return Vector3.ClampMagnitude(sum, 1f);
        }

        static float DistanceToPath(Vector3 position, Pulse pulse)
        {
            Vector2 point = new Vector2(position.x, position.z);
            Vector2 from = new Vector2(pulse.from.x, pulse.from.z);
            Vector2 axis = new Vector2(pulse.to.x, pulse.to.z) - from;
            float t = Mathf.Clamp01(Vector2.Dot(point - from, axis) / Mathf.Max(axis.sqrMagnitude, 1e-6f));
            return Vector2.Distance(point, from + axis * t);
        }

        void OnDisable()
        {
            System.Array.Clear(_pulses, 0, Capacity);
            _next = 0;
        }
    }
}
