#if UNITY_EDITOR
using System;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // Editor capture automation that needs Screen and input coordinates runs inside the real game frame.
    [AddComponentMenu("")]
    public class StageCaptureFrame : MonoBehaviour
    {
        public Action onFrame;

        void LateUpdate()
        {
            onFrame?.Invoke();
        }
    }
}
#endif
