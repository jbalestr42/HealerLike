using System.Collections;
using System.IO;
using UnityEngine;
using HealerLike.Render.Grass;

namespace HealerLike.Render.Stage
{
    // A fixed-clock grass scene: walks, landings, heals, shots, hits and status effects leave measurable traces.
    public class GrassLabRun : AStageRun
    {
        static readonly int filmEvery = 9;

        protected override bool shouldStartGame { get { return false; } }

        protected override IEnumerator Run()
        {
            string folder = Path.Combine(StagePlay.CaptureFolder, "grass-lab");
            Directory.CreateDirectory(folder);
            bool isPassed = false;
            using (GrassLabScene scene = new GrassLabScene(_manager))
            using (GrassLabImages images = new GrassLabImages())
            {
                if (scene.Init())
                {
                    GrassLabMeasurements measurements = new GrassLabMeasurements(scene.creatures);
                    GrassLabScenario scenario = new GrassLabScenario(scene, measurements);
                    for (int frame = 0; frame < GrassLabScenario.Frames; frame++)
                    {
                        float time = frame * GrassLabScenario.Step;
                        scenario.Tick(frame);
                        scene.registry.PublishFrame(GrassLabScenario.Step);
                        scene.ground.Advance(GrassLabScenario.Step);
                        scene.field.UpdateField(scene.registry, scene.ground, GrassLabScenario.Step, time);
                        scene.look.ApplyGlobals();
                        GroundSimulation simulation = scene.field.simulation;
                        Color[] motion = simulation != null ? StageCaptureTexture.Read(simulation.motion) : null;
                        Color[] crush = simulation != null ? StageCaptureTexture.Read(simulation.crush) : null;
                        measurements.Record(simulation, motion, crush, frame, time);
                        if (frame % filmEvery == 0)
                        {
                            images.Capture(simulation, motion, crush, scene.camera, frame, folder);
                        }
                        yield return null;
                    }

                    measurements.Write(folder);
                    images.Write(folder);
                    isPassed = scene.field.simulation != null && scene.field.simulation.isValid
                               && images.frameCount > 0;
                    Debug.Log($"[GrassLabRun] {images.frameCount} film frames, ground {(isPassed ? "live" : "missing")} "
                              + $"in {folder}");
                }
                else
                {
                    Debug.LogError("[GrassLabRun] The lab needs all four creatures.");
                }
            }

            StagePlay.Finish(this, isPassed);
        }
    }
}
