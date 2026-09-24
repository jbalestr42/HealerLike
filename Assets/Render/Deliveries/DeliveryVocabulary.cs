using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Deliveries
{
    // The tip of each delivery style. A tip part is in tip units, one unit being the tip bead's width,
    // with z along the travel and y up.
    [CreateAssetMenu(menuName = "Custom/Data/Render/DeliveryVocabulary")]
    public class DeliveryVocabulary : SerializedScriptableObject
    {
        // The tip width of a shot no view claims when there is no vocabulary to read it from
        public static readonly float DefaultBulletSize = 0.2f;

        // A style without an arm entry draws as a bending arm at full width
        public static readonly ArmStyle BendingArm = new ArmStyle();

        public Dictionary<DeliveryStyle, LookPart[]> tips = new Dictionary<DeliveryStyle, LookPart[]>();

        public Dictionary<DeliveryStyle, ArmStyle> arms = new Dictionary<DeliveryStyle, ArmStyle>();

        public LookPalette palette;

        // The pod an area item drops from the tip at contact
        public LookPart splashPod;

        // A shot no view claims draws its tip at this width in world units, with this material, since it has no
        // rig to borrow one from
        public float bulletSize = DefaultBulletSize;
        public Material material;

        public ArmStyle GetArm(DeliveryStyle style)
        {
            if (arms == null || !arms.ContainsKey(style) || arms[style] == null)
            {
                return BendingArm;
            }
            return arms[style];
        }

        // The accent of the first consumer's family, false when the shot carries none
        public bool TryAccent(List<AConsumerFactory> consumers, out Color accent)
        {
            accent = Color.clear;
            if (consumers == null)
            {
                return false;
            }

            if (!palette)
            {
                Debug.LogError("[DeliveryVocabulary] No palette.");
                accent = Color.magenta;
                return true;
            }

            foreach (AConsumerFactory consumer in consumers)
            {
                if (consumer != null)
                {
                    accent = palette.Colour(ColourRole.Accent, EffectDerivation.ConsumerFamily(consumer, false));
                    return true;
                }
            }
            return false;
        }

        // The family's accent, or the damage accent for a shot that carries no consumer
        public Color ShotColour(List<AConsumerFactory> consumers)
        {
            if (TryAccent(consumers, out Color accent))
            {
                return accent;
            }

            if (palette)
            {
                return palette.Accent(EffectFamily.Damage);
            }
            return Color.white;
        }

        // A style without an entry draws nothing
        public LookPart[] GetTip(DeliveryStyle style)
        {
            if (tips == null || !tips.ContainsKey(style) || tips[style] == null)
            {
                return Array.Empty<LookPart>();
            }
            return tips[style];
        }
    }
}
