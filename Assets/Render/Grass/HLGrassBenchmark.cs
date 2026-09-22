using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace HealerLike.Render.Grass
{
    /// <summary>Attach for each staged scenario. Measures whole-frame wall time, not grass GPU time.</summary>
    public sealed class HLGrassBenchmark : MonoBehaviour
    {
        public const int WarmupFrames = 120;
        public const int SampleFrames = 300;
        [SerializeField] string scenario = "0 zones / middle orbit";
        [SerializeField] HLGrassField field;
        readonly double[] samples = new double[SampleFrames];
        int frames;
        double previousTime, total;
        public int CollectedFrames { get; private set; }
        public double AverageFrameMilliseconds => CollectedFrames == 0 ? 0 : total / CollectedFrames;
        public bool Complete => CollectedFrames == SampleFrames;
        public void ResetCapture() { frames = 0; CollectedFrames = 0; total = 0; previousTime = Time.realtimeSinceStartupAsDouble; }
        void OnEnable() => ResetCapture();
        void Update()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            double elapsed = (now - previousTime) * 1000; previousTime = now;
            if (field != null && !field.IsReady) { ResetCapture(); return; }
            if (Complete || !RecordFrame(elapsed)) return;
            Array.Sort(samples);
            int msaa = GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset pipeline ? pipeline.msaaSampleCount : QualitySettings.antiAliasing;
            Debug.Log($"HLGrassBenchmark [{scenario}]: {SampleFrames} frames after {WarmupFrames} warmup; average {AverageFrameMilliseconds:F3} ms; p95 {samples[284]:F3} ms; " +
                $"blades={(field != null ? field.BladeCount : 0)}; {Screen.width}x{Screen.height}; MSAA={msaa}; zones={(field != null ? field.ActiveZoneCount : 0)}; " +
                $"GPU={SystemInfo.graphicsDeviceName}; CPU={SystemInfo.processorType}; Unity={Application.unityVersion}; development={Debug.isDebugBuild}; version={Application.version}. " +
                "Whole-frame wall time only; grass GPU/main-thread budgets require a profiler capture.", this);
        }
        /// <returns>True exactly on the final sample. Useful for deterministic harness tests.</returns>
        public bool RecordFrame(double milliseconds)
        {
            if (double.IsNaN(milliseconds) || double.IsInfinity(milliseconds) || milliseconds < 0) throw new ArgumentOutOfRangeException(nameof(milliseconds));
            if (Complete || frames++ < WarmupFrames) return false;
            samples[CollectedFrames++] = milliseconds; total += milliseconds;
            return Complete;
        }
    }
}
