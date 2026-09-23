using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    [CreateAssetMenu(menuName = "HealerLike/Render/Delivery Styles")]
    public class HLDeliveryStyles : ScriptableObject
    {
        [Serializable]
        public struct HLEntry
        {
            public string prefabName;
            public HLDeliveryStyle style;
            public string evidence;
        }

        public List<HLEntry> entries = new List<HLEntry>();

        public HLDeliveryStyle For(GameObject prefab)
        {
            return ForName(prefab ? prefab.name : null);
        }

        public HLDeliveryStyle ForName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return HLDeliveryStyle.Direct;
            }
            name = name.Replace("(Clone)", "").Trim();
            if (name.StartsWith("HL", StringComparison.Ordinal))
            {
                name = name.Substring(2);
            }
            foreach (HLDeliveryStyles.HLEntry entry in entries)
            {
                if (entry.prefabName == name)
                {
                    return entry.style;
                }
            }
            return HLDeliveryStyle.Direct;
        }

        // Behaviour facts are supplied by authoring; no projectile is initialized or moved.
        public static HLDeliveryStyle Classify(bool curve, bool laser, bool swarm, bool bounce, bool chain)
        {
            return chain ? HLDeliveryStyle.ChainSync
                : bounce ? HLDeliveryStyle.Bounce
                : swarm ? HLDeliveryStyle.Swarm
                : laser ? HLDeliveryStyle.Rigid
                : curve ? HLDeliveryStyle.Arc
                : HLDeliveryStyle.Direct;
        }
    }
}
