using UnityEngine;

namespace HealerLike.Render.Grass
{
    // How fast the ground's slow state follows the auras: burning is quick and regrowth slow, a heal's lush green
    // lingers and its glow fades. All rates are per second.
    [System.Serializable]
    public struct GroundStateSettings
    {
        public float burn;
        public float regrow;
        public float vitalityIn;
        public float vitalityOut;
        public float glowIn;
        public float glowOut;

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
                    glowOut = 2f
                };
            }
        }

        // _HLGroundStateRates and _HLGroundGlowRates
        public Vector4 ShaderRates()
        {
            return new Vector4(burn, regrow, vitalityIn, vitalityOut);
        }

        public Vector2 ShaderGlowRates()
        {
            return new Vector2(glowIn, glowOut);
        }
    }

    // One step of the ground state, the CPU mirror of HLGroundStateStep: x ash, y vitality from dead at -1 to
    // lush at 1, z glow. aura is the summed GroundStamp.State over the texel.
    public static class GroundState
    {
        public static Vector3 Step(Vector3 state, Vector4 aura, float step, GroundStateSettings settings)
        {
            float cover = Mathf.Clamp01(aura.w);
            float inverse = 1f / Mathf.Max(aura.w, 1e-4f);
            Vector3 asked = new Vector3(aura.x, aura.y, aura.z) * (cover * inverse);
            float ashRate = asked.x > state.x ? settings.burn : settings.regrow;
            float vitalityRate = Mathf.Abs(asked.y) > Mathf.Abs(state.y) ? settings.vitalityIn : settings.vitalityOut;
            float glowRate = asked.z > state.z ? settings.glowIn : settings.glowOut;
            float ash = state.x + (asked.x - state.x) * (1f - Mathf.Exp(-ashRate * step));
            float vitality = state.y + (asked.y - state.y) * (1f - Mathf.Exp(-vitalityRate * step));
            float glow = state.z + (asked.z - state.z) * (1f - Mathf.Exp(-glowRate * step));
            return new Vector3(Mathf.Clamp01(ash), Mathf.Clamp(vitality, -1f, 1f), Mathf.Clamp01(glow));
        }
    }
}
