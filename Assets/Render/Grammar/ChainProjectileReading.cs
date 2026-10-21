using System;
using UnityEngine;

namespace HealerLike.Render.Grammar
{
    // Compatibility with the private serialized mode until gameplay exposes a read-only accessor.
    public static class ChainProjectileReading
    {
        // ChainLightningProjectile keeps its mode private, it is read through its own serialization
        // TODO: read ChainLightningProjectile.effectMode once ChainLightningProjectile exposes it
        public static bool IsHeld(ChainLightningProjectile chain)
        {
            ChainModeReading reading = new ChainModeReading();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(chain), reading);
            return reading._effectMode == ChainLightningProjectile.EffectMode.AttackRateDuration;
        }

        [Serializable]
        class ChainModeReading
        {
            // Named as the serialized field of ChainLightningProjectile so the JSON matches it
            public ChainLightningProjectile.EffectMode _effectMode =
                ChainLightningProjectile.EffectMode.FixedDuration;
        }
    }
}
