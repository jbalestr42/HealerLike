using UnityEngine;

namespace HealerLike.Render.Grammar
{
    public static partial class EffectDerivation
    {
        // Factory-specific reads live at this one boundary for both polarity and magnitude.
        static BaseData ModifierData(ABuffFactory buff, out float delta)
        {
            BaseData data = null;
            delta = 0f;
            if (buff is FlatModifierFactory flat && flat.data != null)
            {
                data = flat.data;
                delta = flat.data.value;
            }
            else if (buff is UpgradeModifierFactory upgrade && upgrade.data != null)
            {
                data = upgrade.data;
                delta = upgrade.data.value;
            }
            else if (buff is SlowModifierFactory slow && slow.data != null)
            {
                data = slow.data;
                delta = slow.data.value;
            }
            else if (buff is TimeModifierFactory time && time.data != null)
            {
                data = time.data;
                delta = time.data.value;
            }
            else if (buff is CurrentWaveModifierFactory wave && wave.data != null)
            {
                data = wave.data;
                delta = wave.data.value;
            }
            else if (buff is HPBasedModifierFactory hpBased && hpBased.data != null)
            {
                data = hpBased.data;
                delta = hpBased.data.factor;
            }
            else if (buff is HealthThresholdModifierFactory threshold && threshold.data != null)
            {
                data = threshold.data;
                delta = threshold.data.value;
            }
            else if (buff is AlliesOnRelativeCellModifierFactory allies && allies.data != null)
            {
                data = allies.data;
                delta = allies.data.value;
            }

            return data;
        }

        public static bool TryModifier(ABuffFactory buff, out AttributeType type, out float delta)
        {
            BaseData data = ModifierData(buff, out delta);
            type = data != null ? data.type : AttributeType.HealthMax;
            if (data == null || data.modifierType == AttributeModifierType.Override)
            {
                delta = 0f;
            }

            return data != null;
        }

        // Multipliers already express a share (+0.5 means +50%). Additive effects retain the
        // 100-point fallback only for previews or absent/zero target baselines. Sized by its owning class, an
        // added damage-reduction fraction is its own share too: +0.5 PercentArmor halves the damage taken.
        static float ModifierMagnitudeShare(ABuffFactory buff, EffectContext context)
        {
            BaseData data = ModifierData(buff, out float delta);
            if (data == null || data.modifierType == AttributeModifierType.Override || !float.IsFinite(delta))
            {
                return 0f;
            }

            if (data.modifierType == AttributeModifierType.Multiply
                || context.casterBaselines != null && IsDamageFraction(data.type))
            {
                return Mathf.Abs(delta);
            }

            return Mathf.Abs(delta) / AttributeReference(context, data.type);
        }

        // Attributes whose value is already a fraction of the damage taken, ResourceConsumerResolver applies them
        // as (1 - PercentArmor) and (1 + Vulnerability). Only a class-sized context reads them as a share: an enemy
        // Vulnerability +0.3 (Hexer) keeps its 100-point reading so no creature effect moves.
        public static bool IsDamageFraction(AttributeType type)
        {
            return type == AttributeType.PercentArmor || type == AttributeType.Vulnerability;
        }
    }
}
