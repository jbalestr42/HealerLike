#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.EventSystems;

namespace HealerLike.Render.Stage
{
    // Native capture drives the actual legacy EventSystem module with frame-by-frame touch samples.
    [AddComponentMenu("")]
    public class StageCaptureInput : BaseInput
    {
        public Touch[] samples = System.Array.Empty<Touch>();
        public int samplesRead { get; private set; }
        public override bool touchSupported { get { return true; } }
        public override int touchCount { get { return samples.Length; } }
        public override Touch GetTouch(int index)
        {
            samplesRead++;
            return samples[index];
        }
        public override bool mousePresent { get { return false; } }
        public override bool GetButtonDown(string buttonName) { return false; }
        public override float GetAxisRaw(string axisName) { return 0f; }
    }
}
#endif
