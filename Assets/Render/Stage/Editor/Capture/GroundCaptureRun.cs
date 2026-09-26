using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Grass;
using HealerLike.Render.Look;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Stage
{
    // The grass carpet alone under its own camera, on a layer nothing else draws on: two renders a frame apart
    // must both show it, and once its zone snapshot is revoked the next render must not. Writes
    // render-ground-fixture.png.
    public class GroundCaptureRun : AStageRun
    {
        static readonly int width = 1440;
        static readonly int height = 960;
        static readonly int fixtureLayer = 30;
        static readonly int tuftBudget = 16384;
        static readonly int minimumGreenPixels = 20000;
        static readonly string pipelinePath = "Assets/Settings/Very High_PipelineAsset.asset";
        static readonly string lookShaderPath = "Assets/Render/Shaders/Look.shader";

        readonly List<Object> _owned = new List<Object>();

        protected override IEnumerator Run()
        {
            foreach (LookController owner in Object.FindObjectsByType<LookController>(FindObjectsSortMode.None))
            {
                owner.enabled = false;
            }

            QualitySettings.renderPipeline = RenderAssets.Load<RenderPipelineAsset>(pipelinePath);
            Camera camera = CreateCamera();
            LookController look = Fixture("GroundFixtureLook").AddComponent<LookController>();
            LookSettings settings = LookSettings.Default;
            settings.fogStart = 25f;
            settings.fogEnd = 60f;
            settings.shadowTint = new Color32(63, 91, 148, 255);
            settings.inkStrength = 0.75f;
            look.settings = settings;
            ZoneRegistry registry = Fixture("GroundFixtureZones").AddComponent<ZoneRegistry>();
            registry.Init();
            Ground ground = new Ground();
            GrassField field = CreateField(camera, registry, ground);
            AddZones(registry);

            look.ApplyGlobals();
            field.UpdateField(registry, ground);
            int firstGreen = GreenPixels(camera, false);
            yield return null;

            // An editor repaint can come on another frame without a field update, the prepared buffers draw again
            look.ApplyGlobals();
            int repaintGreen = GreenPixels(camera, true);
            field.SetZoneSnapshot(null, 0);
            yield return null;

            int revokedGreen = GreenPixels(camera, false);
            bool isPassed = firstGreen > minimumGreenPixels && repaintGreen > firstGreen * 0.9f
                            && revokedGreen < firstGreen * 0.1f && field.tuftCount == tuftBudget;
            Debug.Log($"[GroundCaptureRun] green first {firstGreen} repaint {repaintGreen} revoked {revokedGreen}"
                      + $" tufts {field.tuftCount}");
            if (!isPassed)
            {
                Debug.LogError("[GroundCaptureRun] The carpet did not draw across repaints or outlived its snapshot.");
            }

            foreach (Object owned in _owned)
            {
                Object.Destroy(owned);
            }
            StagePlay.Finish(this, isPassed);
        }

        GameObject Fixture(string name)
        {
            GameObject go = new GameObject(name);
            go.layer = fixtureLayer;
            _owned.Add(go);
            return go;
        }

        Camera CreateCamera()
        {
            Camera camera = Fixture("GroundFixtureCamera").AddComponent<Camera>();
            camera.cullingMask = 1 << fixtureLayer;
            camera.fieldOfView = 44f;
            camera.transform.rotation = Quaternion.Euler(48f, 0f, 0f);
            camera.transform.position = -camera.transform.forward * 12f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.74f, 0.82f, 0.83f);
            // Rendered on request only
            camera.enabled = false;
            Light light = Fixture("GroundFixtureKey").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            light.shadows = LightShadows.Soft;
            light.intensity = 1f;
            RenderSettings.sun = light;
            return camera;
        }

        // An eight by eight carpet on a ground slab, three stones trampling it
        GrassField CreateField(Camera camera, ZoneRegistry registry, Ground said)
        {
            Material material = new Material(RenderAssets.Load<Shader>(lookShaderPath));
            _owned.Add(material);
            // The slab's stored value is already linear, as the fixture was tuned, so it stays darker than the carpet
            material.SetColor(RenderObjects.BaseColorId, ((Color)new Color32(78, 126, 87, 255)).linear);
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _owned.Add(ground);
            ground.layer = fixtureLayer;
            ground.transform.localScale = new Vector3(8f, 0.2f, 8f);
            ground.transform.position = Vector3.down * 0.1f;
            ground.GetComponent<Renderer>().sharedMaterial = material;
            for (int i = 0; i < 3; i++)
            {
                GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _owned.Add(stone);
                stone.name = "FixtureStone" + i;
                stone.layer = fixtureLayer;
                stone.transform.position = new Vector3((i - 1) * 2.2f, 0f, 1.6f);
                stone.transform.localScale = Vector3.one * 1.2f;
                stone.GetComponent<Renderer>().sharedMaterial = material;
                said.Hold(said.vocabulary.obstacle).Show(stone.transform.position, 0.8f, 1f);
            }

            GrassField field = Fixture("GroundFixtureGrass").AddComponent<GrassField>();
            EnvironmentAuthoring.SetGrass(field);
            EnvironmentAuthoring.SetGround(field);
            field.Init(new Rect(-4f, -4f, 8f, 8f), 1f, 0f, camera, registry.buffer, ZonePacker.MaxZones);
            field.tuftBudget = tuftBudget;
            return field;
        }

        static void AddZones(ZoneRegistry registry)
        {
            registry.Add(ZoneKind.Heal, new Vector3(-1.7f, 0f, -1.2f), 1.3f, 1f);
            registry.Add(ZoneKind.Hostile, new Vector3(1.7f, 0f, -0.9f), 1.2f, 0.85f);
            registry.PublishFrame(0.32f);
        }

        // Carpet pixels are clearly greener than they are red or blue
        static int GreenPixels(Camera camera, bool isWritten)
        {
            Texture2D texture = StageReadback.Render(camera, width, height);
            int count = 0;
            foreach (Color32 pixel in texture.GetPixels32())
            {
                if (pixel.g > 100 && pixel.g > pixel.r * 1.1f && pixel.g > pixel.b * 1.3f)
                {
                    count++;
                }
            }

            if (isWritten)
            {
                Directory.CreateDirectory(StagePlay.CaptureFolder);
                File.WriteAllBytes(StagePlay.CaptureFolder + "render-ground-fixture.png", texture.EncodeToPNG());
            }
            Object.Destroy(texture);
            return count;
        }
    }
}
