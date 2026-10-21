#if UNITY_EDITOR
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // Observe production LateUpdates before requesting the actual Game screenshot for this frame.
    [DefaultExecutionOrder(32000)]
    [AddComponentMenu("")]
    public class StagePresentationFrame : StageCaptureFrame
    {
        void LateUpdate() { onFrame?.Invoke(); }
    }
}
#endif
