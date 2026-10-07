using System;
using UnityEngine;

namespace HealerLike.Render.Deliveries
{
    [Serializable]
    public class DeliveryPathLook
    {
        public float width = 0.085f;
        public float deviation = 0.15f;
        public int segmentsPerLeg = 9;
        public float pulseFrequency = 7f;
        public float releaseSeconds = 0.18f;
        public float coreWidth = 0.32f;

        public bool IsValid()
        {
            return float.IsFinite(width) && width > 0f && width <= 1f
                && float.IsFinite(deviation) && deviation >= 0f && deviation <= 1f
                && segmentsPerLeg >= 2 && segmentsPerLeg <= 24
                && float.IsFinite(pulseFrequency) && pulseFrequency >= 0f && pulseFrequency <= 30f
                && float.IsFinite(releaseSeconds) && releaseSeconds > 0f && releaseSeconds <= 2f
                && float.IsFinite(coreWidth) && coreWidth > 0f && coreWidth <= 1f;
        }
    }
}
