using System;
using UnityEngine;

namespace HealerLike.Render.Grass
{
    public enum GroundShape
    {
        // Round a point, its radius growing in over grow seconds
        Disc = 0,
        // A front travelling out from a point to its radius over grow seconds
        Ring = 1,
        // Along a segment between two points
        Line = 2
    }

    // One way the grass answers something: its shape, how it comes and goes, how it moves the grass and what it
    // leaves in it. Embeds anywhere a designer tunes a look (a vocabulary, a spell's data, a component) and is
    // played through Ground. It turns into up to three stamps a frame: what it holds, what it throws, what it
    // asks of the slow ground state.
    [Serializable]
    public class GroundEffect
    {
        public GroundShape shape = GroundShape.Disc;

        [Header("Time")]
        [Tooltip("Seconds for a disc's radius to bloom in or a ring to travel out, zero for at once")]
        public float grow;
        [Tooltip("Seconds for its strength to ease in")]
        public float fadeIn;
        [Tooltip("Seconds a one-shot lasts, its strength fading to nothing; a held effect ignores it")]
        public float lifetime = 0.5f;
        [Tooltip("Seconds a ring keeps throwing once it has arrived, easing off")]
        public float release = 0.1f;

        [Header("Shape")]
        [Tooltip("Share of the radius a disc's edge fades over")]
        public float edge = 0.25f;
        [Tooltip("Share of the radius a disc's rim wobbles inward by, so it never reads as a stamp")]
        public float wobble;
        [Tooltip("A ring's front half width as a share of its radius, and its least half width in world units")]
        public float band = 0.15f;
        public float minBand = 0.2f;
        [Tooltip("A line's half width in world units, how far it zigzags either side, and its zigzags per unit")]
        public float width = 0.45f;
        public float swing;
        public float turns;

        [Header("Motion")]
        [Tooltip("Radians of lean a disc holds the grass at, away from its centre")]
        public float hold;
        [Tooltip("Radians the held lean turns from straight outward, counterclockwise seen from above")]
        public float holdTurn;
        [Tooltip("Acceleration the grass is thrown with, radians per second squared: outward from a disc or a " +
                 "ring, sideways away from a line")]
        public float kick;
        [Tooltip("Radians a disc's throw turns from outward; a quarter turn spins the grass")]
        public float kickTurn;
        [Tooltip("Times a second the throw reverses, zero for a steady one: a shiver")]
        public float shiver;
        [Tooltip("How flat a disc lays the grass under it")]
        public float flatten;

        [Header("What it leaves")]
        public float ash;
        [Tooltip("Dead at -1, lush at 1")]
        public float vitality;
        [Tooltip("Frost at -1, a heal's glow at 1")]
        public float light;
        public float blight;

        public bool hasState
        {
            get { return ash != 0f || vitality != 0f || light != 0f || blight != 0f; }
        }

        // A copy a designer can change without touching the one it came from
        public GroundEffect Clone()
        {
            return (GroundEffect)MemberwiseClone();
        }

        // Whether a one-shot this old has played out
        public bool IsOver(float age)
        {
            if (lifetime > 0f && age >= lifetime)
            {
                return true;
            }

            return shape == GroundShape.Ring && age >= grow + release;
        }

        // The strength at an age: eased in, and for a one-shot faded over its lifetime
        public float StrengthAt(float age, float strength, bool isOneShot)
        {
            float easedIn = fadeIn > 0f ? GroundStamp.SmoothStep(0f, fadeIn, age) : 1f;
            float left = isOneShot && lifetime > 0f ? Mathf.Clamp01(1f - age / lifetime) : 1f;
            return Mathf.Clamp01(RenderMath.FiniteOr(strength, 0f)) * easedIn * left;
        }

        // Writes this frame's stamps for the effect at a point, or along from to to for a line, from start on,
        // as many as fit, and returns how many it wrote
        public int Write(GroundStamp[] into, int start, Vector2 from, Vector2 to, float radius, float strength,
                         float age, bool isOneShot)
        {
            if (into == null || start >= into.Length)
            {
                return 0;
            }

            float power = StrengthAt(age, strength, isOneShot);
            if (power <= 0f)
            {
                return 0;
            }

            float grown = grow > 0f ? Mathf.Clamp01(age / grow) : 1f;
            float throwing = kick * power * Shiver(age);
            int count = 0;
            switch (shape)
            {
                case GroundShape.Disc:
                    float size = Mathf.Max(0f, radius) * grown;
                    if (size <= 0f)
                    {
                        return 0;
                    }

                    if (hold != 0f || flatten != 0f)
                    {
                        count += Put(into, start + count, GroundStamp.Disc(from, size, hold * power, flatten * power,
                                                                          edge, wobble, holdTurn));
                    }

                    if (throwing != 0f)
                    {
                        count += Put(into, start + count, GroundStamp.Swirl(from, size, kickTurn, 1f, throwing, edge));
                    }

                    if (hasState)
                    {
                        count += Put(into, start + count, GroundStamp.Aura(from, size, edge, wobble, ash * power,
                                                                          vitality * power, light * power,
                                                                          blight * power));
                    }

                    return count;
                case GroundShape.Ring:
                    // Once the ring has arrived it lets go, rather than pinning the grass at its rim
                    float letGo = 1f - GroundStamp.SmoothStep(grow, grow + release, age);
                    float front = Mathf.Max(0f, radius) * grown;
                    float halfBand = Mathf.Max(minBand, Mathf.Max(0f, radius) * band);
                    if (throwing * letGo != 0f)
                    {
                        count += Put(into, start + count, GroundStamp.Shock(from, front, halfBand, 1f, throwing * letGo));
                    }

                    return count;
                default:
                    if ((to - from).sqrMagnitude < 1e-8f)
                    {
                        return 0;
                    }

                    if (throwing != 0f)
                    {
                        count += Put(into, start + count, GroundStamp.Trail(from, to, width, throwing));
                    }

                    if (hasState)
                    {
                        count += Put(into, start + count, GroundStamp.Streak(from, to, width, swing, turns, ash * power,
                                                                            vitality * power, light * power,
                                                                            blight * power));
                    }

                    return count;
            }
        }

        float Shiver(float age)
        {
            return shiver > 0f ? Mathf.Sin(age * shiver * 2f * Mathf.PI) : 1f;
        }

        static int Put(GroundStamp[] into, int index, GroundStamp stamp)
        {
            if (index >= into.Length)
            {
                return 0;
            }

            into[index] = stamp;
            return 1;
        }
    }
}
