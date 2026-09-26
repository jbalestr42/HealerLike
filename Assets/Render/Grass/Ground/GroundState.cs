using UnityEngine;

namespace HealerLike.Render.Grass
{
    // How fast the ground's slow state follows the auras: burning is quick and regrowth slow, lush and dead grass
    // follow their creature closely, a heal's glow and a slow's frost fade fast, poison's blight lingers a little.
    // All rates are per second.
    [System.Serializable]
    public struct GroundStateSettings
    {
        public float burn;
        public float regrow;
        public float vitalityIn;
        public float vitalityOut;
        // Toward more glow or frost, and back to neither
        public float glowIn;
        public float glowOut;
        public float blightIn;
        public float blightOut;

        public static GroundStateSettings Default
        {
            get
            {
                return new GroundStateSettings
                {
                    burn = 1.6f,
                    regrow = 0.35f,
                    vitalityIn = 1.2f,
                    vitalityOut = 0.8f,
                    glowIn = 8f,
                    glowOut = 2f,
                    blightIn = 1.5f,
                    blightOut = 0.6f
                };
            }
        }

        // _HLGroundStateRates and _HLGroundGlowRates
        public Vector4 ShaderRates()
        {
            return new Vector4(burn, regrow, vitalityIn, vitalityOut);
        }

        public Vector4 ShaderLightRates()
        {
            return new Vector4(glowIn, glowOut, blightIn, blightOut);
        }
    }

    // One step of the ground state, the CPU mirror of HLGroundStateStep: x ash, y vitality from dead at -1 to
    // lush at 1, z light from frost at -1 to glow at 1, w blight. aura is the summed GroundStamp.State over the
    // texel.
    public static class GroundState
    {
        public static Vector4 Step(Vector4 state, Vector4 aura, float step, GroundStateSettings settings)
        {
            Vector4 asked = new Vector4(Mathf.Clamp01(aura.x), Mathf.Clamp(aura.y, -1f, 1f),
                                        Mathf.Clamp(aura.z, -1f, 1f), Mathf.Clamp01(aura.w));
            float ashRate = asked.x > state.x ? settings.burn : settings.regrow;
            float vitalityRate = Mathf.Abs(asked.y) > Mathf.Abs(state.y) ? settings.vitalityIn : settings.vitalityOut;
            float lightRate = Mathf.Abs(asked.z) > Mathf.Abs(state.z) ? settings.glowIn : settings.glowOut;
            float blightRate = asked.w > state.w ? settings.blightIn : settings.blightOut;
            Vector4 next = new Vector4(
                Ease(state.x, asked.x, ashRate, step), Ease(state.y, asked.y, vitalityRate, step),
                Ease(state.z, asked.z, lightRate, step), Ease(state.w, asked.w, blightRate, step));
            return new Vector4(Mathf.Clamp01(next.x), Mathf.Clamp(next.y, -1f, 1f), Mathf.Clamp(next.z, -1f, 1f),
                               Mathf.Clamp01(next.w));
        }

        static float Ease(float value, float target, float rate, float step)
        {
            return value + (target - value) * (1f - Mathf.Exp(-rate * step));
        }
    }
}
