using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using HealerLike.Render.Grass;

namespace HealerLike.Render.Stage
{
    // Whole-frame wall time of the stage with its grass, once the game is running: a warmup, then the average and
    // the 95th percentile over the samples. Grass GPU and main-thread budgets need a profiler capture.
    public class GrassBenchRun : AStageRun
    {
        public static readonly int WarmupFrames = 120;
        public static readonly int SampleFrames = 300;
        public static readonly float Percentile = 0.95f;

        protected override IEnumerator Run()
        {
            GrassField field = _manager.grass;
            double[] samples = new double[SampleFrames];
            int frames = 0;
            int collected = 0;
            int lastFrame = Time.frameCount;
            while (collected < SampleFrames)
            {
                yield return NextFrame();
                if (Time.frameCount == lastFrame)
                {
                    continue;
                }

                lastFrame = Time.frameCount;
                if (!field.isReady)
                {
                    frames = 0;
                    collected = 0;
                    continue;
                }

                frames++;
                if (frames > WarmupFrames)
                {
                    samples[collected] = Time.unscaledDeltaTime * 1000d;
                    collected++;
                }
            }

            double total = 0d;
            foreach (double sample in samples)
            {
                total += sample;
            }

            Array.Sort(samples);
            double average = total / SampleFrames;
            double p95 = samples[PercentileIndex(SampleFrames, Percentile)];
            Debug.Log($"[GrassBenchRun] {SampleFrames} frames after {WarmupFrames} warmup; average {average:F3} ms;"
                      + $" p95 {p95:F3} ms; {Describe(field)}");
            StagePlay.Finish(this, double.IsFinite(average) && average > 0d);
        }

        // The sorted sample at or above the given share of the samples
        public static int PercentileIndex(int count, float share)
        {
            return Mathf.Clamp(Mathf.CeilToInt(count * share) - 1, 0, count - 1);
        }

        static string Describe(GrassField field)
        {
            int msaa = QualitySettings.antiAliasing;
            UniversalRenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline != null)
            {
                msaa = pipeline.msaaSampleCount;
            }

            return $"tufts={field.tuftCount}; zones={field.activeZoneCount}; {Screen.width}x{Screen.height}; MSAA={msaa};"
                   + $" GPU={SystemInfo.graphicsDeviceName}; CPU={SystemInfo.processorType};"
                   + $" Unity={Application.unityVersion}";
        }
    }
}
