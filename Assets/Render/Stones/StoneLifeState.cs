using System;
using HealerLike.Render.Zones;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    // Bounded snapshot comparison: no gameplay queries and no per-frame allocations.
    public class StoneLifeState
    {
        readonly Zone[] _previous = new Zone[64];
        readonly bool[] _used = new bool[64];
        int _count;
        float _health = 1f;
        float _remaining;
        float _next;

        public bool PollHealth(float fraction, float dt)
        {
            if (!float.IsFinite(fraction))
            {
                return false;
            }

            fraction = Mathf.Clamp01(fraction);
            if (fraction <= 0f)
            {
                _remaining = 0f;
                _health = fraction;
                return false;
            }

            if (_health >= 0.5f && fraction < 0.5f)
            {
                _remaining = 3f;
                _next = 0f;
            }
            if (fraction >= 0.5f)
            {
                _remaining = 0f;
            }

            _health = fraction;
            if (_remaining <= 0f)
            {
                return false;
            }

            if (float.IsFinite(dt))
            {
                dt = Mathf.Max(0f, dt);
            }
            else
            {
                dt = 0f;
            }
            _remaining = Mathf.Max(0f, _remaining - dt);
            _next -= dt;
            if (_next > 0f || _remaining <= 0f)
            {
                return false;
            }

            _next = 0.18f;
            return true;
        }

        public int PollZones(ReadOnlySpan<Zone> zones, Vector3 center, float radius)
        {
            Array.Clear(_used, 0, _used.Length);
            int pulses = 0;
            int length = Mathf.Min(64, zones.Length);
            for (int i = 0; i < length; i++)
            {
                Zone zone = zones[i];
                if (zone.kind != (int)ZoneKind.Hostile || zone.strength <= 0f || !Overlaps(zone, center, radius))
                {
                    continue;
                }

                bool seen = false;
                for (int j = 0; j < _count; j++)
                {
                    Zone previous = _previous[j];
                    if (_used[j] || previous.kind != zone.kind || previous.position != zone.position
                        || previous.radius != zone.radius || zone.age < previous.age
                        || zone.strength > previous.strength || !Overlaps(previous, center, radius))
                    {
                        continue;
                    }

                    _used[j] = true;
                    seen = true;
                    break;
                }
                if (!seen)
                {
                    pulses++;
                }
            }
            zones.Slice(0, length).CopyTo(_previous);
            _count = length;
            return pulses;
        }

        static bool Overlaps(Zone zone, Vector3 position, float radius)
        {
            float x = zone.position.x - position.x;
            float y = zone.position.z - position.z;
            float r = zone.radius + radius;
            return x * x + y * y <= r * r;
        }

        public static float Wobble(float age)
        {
            if (age < 0f || age >= 1.2f)
            {
                return 0f;
            }
            return 7f * Mathf.Sin(age * 24f) * Mathf.Pow(1f - age / 1.2f, 2f);
        }

        public static bool Ochre(uint seed)
        {
            return seed % 5 == 0;
        }
    }
}
