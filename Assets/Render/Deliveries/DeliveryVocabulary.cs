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
        // TODO: take it from RenderManager once it holds a reference, Resources is the only road from here
        public static readonly string ResourcePath = "DeliveryVocabulary";

        public Dictionary<DeliveryStyle, LookPart[]> tips = new Dictionary<DeliveryStyle, LookPart[]>();

        public LookPalette palette;

        // The pod an area item drops from the tip at contact
        public LookPart splashPod;

        // A shot no view claims draws its tip at this width in world units, with these meshes and material
        public float bulletSize = 0.2f;
        public PrimitiveMeshes meshes;
        public Material material;

        public static DeliveryVocabulary Load()
        {
            return Resources.Load<DeliveryVocabulary>(ResourcePath);
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
