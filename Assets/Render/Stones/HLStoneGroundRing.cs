using UnityEngine;

namespace HealerLike.Render.Stones
{
    // removed in D2: HLStageBeautyWiring still looks this type up, the bare earth colour now lives on StoneGroundDisc
    public class HLStoneGroundRing : MonoBehaviour
    {
        public Color groundColour { get; set; }
    }
}
