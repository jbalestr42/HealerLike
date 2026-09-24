using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Deliveries
{
    // How an arm draws one delivery style: a rod telescopes straight at its shot, the widths scale the arm and its
    // leaves, and a rod may snap back faster than the arm's own retract
    [Serializable]
    public class ArmStyle
    {
        public bool isRod;
        public float width = 1f;
        public float leafWidth = 1f;
        // Zero keeps the arm's own retract time
        public float retractSeconds;
    }

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
