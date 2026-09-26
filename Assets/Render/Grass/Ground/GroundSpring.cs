using UnityEngine;

namespace HealerLike.Render.Grass
{
    // How the ground's grass answers a push: a damped spring toward the stamped lean, coupled to its neighbours
    // so a push travels, and a flatness that falls fast and stands back up slowly
    [System.Serializable]
    public struct GroundSpringSettings
    {
        // Sway cycles per second of an undamped texel
        public float frequency;
        // One settles without overshoot, lower values bounce
        public float dampingRatio;
        // World units over which a held push fades into the grass around it; the same pull carries a sudden
        // push outward as a ripple
        public float spread;
        // The largest lean a texel reaches, in radians
        public float maxLean;
        // Per second rates toward a higher and a lower flatness
        public float crushFall;
        public float crushRise;

        public static GroundSpringSettings Default
        {
            get
            {
                return new GroundSpringSettings
                {
                    frequency = 1.6f,
                    dampingRatio = 0.3f,
                    spread = 0.2f,
                    maxLean = 1.25f,
                    crushFall = 16f,
                    crushRise = 1.1f
                };
            }
        }

        public float stiffness
        {
            get
            {
                float omega = 2f * Mathf.PI * Mathf.Max(0f, frequency);
                return omega * omega;
            }
        }

        public float damping
        {
            get { return 2f * Mathf.Clamp01(dampingRatio) * 2f * Mathf.PI * Mathf.Max(0f, frequency); }
        }

        // The pull toward the neighbours' mean on a grid of this texel size. A held push decays as
        // exp(-distance / spread) around it when the pull is 4 * stiffness * (spread / texel)^2; it stays under the
        // bound that keeps the longest step stable.
        public float Coupling(float texelSize)
        {
            if (!RenderMath.IsPositive(texelSize))
            {
                return 0f;
            }

            float share = Mathf.Max(0f, RenderMath.FiniteOr(spread, 0f)) / texelSize;
            float longest = Mathf.Max(GroundSpring.MaxStep, GroundSpring.MaxFrame / GroundSpring.MaxSteps);
            float stable = Mathf.Max(0f, (3f / (longest * longest) - stiffness) * 0.5f);
            return Mathf.Min(4f * stiffness * share * share, stable);
        }

        // x stiffness, y damping, z neighbour pull, w max lean: _HLGroundSpring
        public Vector4 ShaderSpring(float texelSize)
        {
            return new Vector4(stiffness, damping, Coupling(texelSize), Mathf.Max(0f, maxLean));
        }
    }

    // One step of the texel spring and flatness, the CPU mirror of HLGroundSpringStep and HLGroundCrushStep
    public static class GroundSpring
    {
        // The longest step the ground takes; longer frames split into equal steps, at most MaxSteps of them
        public static readonly float MaxStep = 1f / 60f;
        public static readonly int MaxSteps = 4;
        // A frame longer than this is clamped, so a hitch never throws the grass
        public static readonly float MaxFrame = 0.1f;

        // Equal steps covering the frame, none for a paused or invalid frame
        public static int StepCount(float deltaTime, out float step)
        {
            step = 0f;
            if (!RenderMath.IsPositive(deltaTime))
            {
                return 0;
            }

            float frame = Mathf.Min(deltaTime, MaxFrame);
            int count = Mathf.Clamp(Mathf.CeilToInt(frame / MaxStep - 0.001f), 1, MaxSteps);
            step = frame / count;
            return count;
        }

        // lean and velocity advance by one semi-implicit Euler step; the lean stays within the max lean
        public static void Step(ref Vector2 lean, ref Vector2 velocity, Vector2 target, Vector2 neighbourMean,
                                float step, Vector4 spring)
        {
            Vector2 acceleration = spring.x * (target - lean) - spring.y * velocity + spring.z * (neighbourMean - lean);
            velocity += acceleration * step;
            lean += velocity * step;
            lean *= Mathf.Min(1f, spring.w / Mathf.Max(lean.magnitude, 1e-5f));
        }

        public static float Crush(float crush, float target, float step, float fall, float rise)
        {
            float rate = target > crush ? fall : rise;
            return crush + (target - crush) * (1f - Mathf.Exp(-rate * step));
        }

        // The stamped lean plus the wind, no longer than the max lean
        public static Vector2 Target(Vector2 stamped, Vector2 wind, float maxLean)
        {
            Vector2 sum = stamped + wind;
            return sum * Mathf.Min(1f, maxLean / Mathf.Max(sum.magnitude, 1e-5f));
        }
    }
}
