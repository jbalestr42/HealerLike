using UnityEngine;

namespace HealerLike.Render.Environment
{
    /// <summary>Bounded cosmetic impulses. Stage supplies real launch direction; no gameplay polling.</summary>
    [DisallowMultipleComponent]
    public sealed class HLEnvironmentGust : MonoBehaviour
    {
        public const int Capacity = 8;
        struct Pulse { public Vector3 direction; public float strength, seconds; public double start; }
        readonly Pulse[] pulses = new Pulse[Capacity];
        int next;
        public void Gust(Vector3 direction, float strength, float seconds)
            => GustAt(direction, strength, seconds, Time.timeAsDouble);
        public void GustAt(Vector3 direction, float strength, float seconds, double time)
        {
            if (!Finite(direction.x) || !Finite(direction.y) || !Finite(direction.z) || !Finite(strength) || !Finite(seconds)
                || double.IsNaN(time) || double.IsInfinity(time) || seconds <= 0 || strength <= 0) return;
            direction.y = 0;
            if (direction.sqrMagnitude < .000001f || float.IsInfinity(direction.sqrMagnitude)) return;
            pulses[next] = new Pulse { direction = direction.normalized, strength = Mathf.Clamp01(strength), seconds = Mathf.Min(seconds, 10), start = time };
            next = (next + 1) % Capacity;
        }
        public Vector3 Sample(double time)
        {
            if (double.IsNaN(time) || double.IsInfinity(time)) return Vector3.zero;
            Vector3 sum = Vector3.zero;
            for (int i = 0; i < Capacity; i++)
            {
                var p = pulses[i];
                double age = time - p.start;
                if (p.seconds <= 0 || age <= 0 || age >= p.seconds) continue;
                sum += p.direction * (p.strength * Mathf.Sin((float)(age / p.seconds) * Mathf.PI));
            }
            return Vector3.ClampMagnitude(sum, 1);
        }
        void OnDisable() { System.Array.Clear(pulses, 0, Capacity); next = 0; }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
