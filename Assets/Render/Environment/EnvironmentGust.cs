using UnityEngine;

namespace HealerLike.Render.Environment
{
    // Short cosmetic wind pushes. The stage sends the launch direction; nothing here polls gameplay.
    public class EnvironmentGust : MonoBehaviour
    {
        public static readonly int Capacity = 8;

        struct Pulse
        {
            public Vector3 direction;
            public float strength;
            public float seconds;
            public double start;
        }

        readonly Pulse[] _pulses = new Pulse[Capacity];
        int _next;

        // Starts now on the game clock, which Sample reads
        public void Gust(Vector3 direction, float strength, float seconds)
        {
            bool isPulseValid = RenderMath.IsPositive(strength) && RenderMath.IsPositive(seconds);
            if (!RenderMath.IsFinite(direction) || !isPulseValid)
            {
                return;
            }

            direction.y = 0f;
            if (direction.sqrMagnitude < 0.000001f || !float.IsFinite(direction.sqrMagnitude))
            {
                return;
            }

            _pulses[_next] = new Pulse
            {
                direction = direction.normalized,
                strength = Mathf.Clamp01(strength),
                seconds = Mathf.Min(seconds, 10f),
                start = Time.timeAsDouble
            };
            _next = (_next + 1) % Capacity;
        }

        public Vector3 Sample(double time)
        {
            if (!double.IsFinite(time))
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

                sum += pulse.direction * (pulse.strength * Mathf.Sin((float)(age / pulse.seconds) * Mathf.PI));
            }

            return Vector3.ClampMagnitude(sum, 1f);
        }

        void OnDisable()
        {
            System.Array.Clear(_pulses, 0, Capacity);
            _next = 0;
        }
    }
}
