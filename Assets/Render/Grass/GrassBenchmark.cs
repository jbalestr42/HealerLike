using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HealerLike.Render.Grass
{
    // Attach for each staged scenario. Measures whole-frame wall time, not grass GPU time.
    public class GrassBenchmark : MonoBehaviour
    {
        public static readonly int WarmupFrames = 120;
        public static readonly int SampleFrames = 300;

        [SerializeField] string _scenario = "0 zones / middle orbit";
        [SerializeField] GrassField _field;

        double[] _samples = new double[SampleFrames];
        int _frames;
        double _previousTime;
        double _total;

        int _collectedFrames;
        public int collectedFrames { get { return _collectedFrames; } }

        public double averageFrameMilliseconds
        {
            get { return _collectedFrames == 0 ? 0d : _total / _collectedFrames; }
        }

        public bool isComplete { get { return _collectedFrames == SampleFrames; } }

        void OnEnable()
        {
            ResetCapture();
        }

        void Update()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            double elapsed = (now - _previousTime) * 1000;
            _previousTime = now;
            if (_field != null && !_field.isReady)
            {
                ResetCapture();
                return;
            }

            if (isComplete || !RecordFrame(elapsed))
            {
                return;
            }

            Array.Sort(_samples);
            Debug.Log($"[HLGrassBenchmark] {_scenario}: {SampleFrames} frames after {WarmupFrames} warmup; " +
                $"average {averageFrameMilliseconds:F3} ms; p95 {_samples[284]:F3} ms; {Describe()}. " +
                "Whole-frame wall time only; grass GPU and main-thread budgets need a profiler capture.", this);
        }

        public void ResetCapture()
        {
            _frames = 0;
            _collectedFrames = 0;
            _total = 0;
            _previousTime = Time.realtimeSinceStartupAsDouble;
        }

        // True exactly on the final sample
        public bool RecordFrame(double milliseconds)
        {
            if (!double.IsFinite(milliseconds) || milliseconds < 0)
            {
                Debug.LogError($"[HLGrassBenchmark] Ignored invalid frame time {milliseconds}.");
                return false;
            }

            if (isComplete)
            {
                return false;
            }

            _frames++;
            if (_frames <= WarmupFrames)
            {
                return false;
            }

            _samples[_collectedFrames] = milliseconds;
            _collectedFrames++;
            _total += milliseconds;
            return isComplete;
        }

        string Describe()
        {
            int msaa = QualitySettings.antiAliasing;
            UniversalRenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline != null)
            {
                msaa = pipeline.msaaSampleCount;
            }

            int blades = _field != null ? _field.bladeCount : 0;
            int zones = _field != null ? _field.activeZoneCount : 0;
            return $"blades={blades}; {Screen.width}x{Screen.height}; MSAA={msaa}; zones={zones}; " +
                $"GPU={SystemInfo.graphicsDeviceName}; CPU={SystemInfo.processorType}; Unity={Application.unityVersion}; " +
                $"development={Debug.isDebugBuild}; version={Application.version}";
        }
    }
}
