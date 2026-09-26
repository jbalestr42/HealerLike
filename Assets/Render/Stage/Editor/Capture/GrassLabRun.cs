using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grass;
using HealerLike.Render.Look;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Stage
{
    // A scripted grass scene stepped at a fixed 60 Hz, whatever the editor's frame rate: an ally walks through
    // the carpet past a stone enemy and a second ally, a heal blooms, a launch crosses, a gust blows. The
    // creatures are the game's own rigs pressing the grass as bodies. Writes a filmstrip, a contact sheet and the
    // ground's lean and flatness at a few probes on every frame to grass-lab/ under the capture folder.
    public class GrassLabRun : AStageRun
    {
        static readonly int frameWidth = 720;
        static readonly int frameHeight = 480;
        static readonly int sheetColumns = 6;
        static readonly int sheetScale = 2;
        static readonly int mapSize = 240;
        static readonly int labLayer = 30;
        static readonly float step = 1f / 60f;
        static readonly int frames = 270;
        static readonly int filmEvery = 9;
        static readonly string pipelinePath = "Assets/Settings/Very High_PipelineAsset.asset";
        static readonly string lookShaderPath = "Assets/Render/Shaders/Look.shader";
        static readonly Rect area = new Rect(-4f, -4f, 8f, 8f);

        // Where the probes read the ground: on the walk, beside it, inside the heal, on the launch path
        static readonly Vector2[] probes =
        {
            new Vector2(0f, 0.5f), new Vector2(0f, 1.4f), new Vector2(-1.5f, -0.6f), new Vector2(1f, -1.6f)
        };

        readonly List<Object> _owned = new List<Object>();
        readonly List<LabCreature> _creatures = new List<LabCreature>();

        // A cosmetic creature and the body it presses into the grass
        class LabCreature : IZoneBody
        {
            public Transform anchor;
            public CreaturePreview preview = new CreaturePreview();
            readonly BodyMeshes _meshes = new BodyMeshes();

            public void Refresh()
            {
                _meshes.Refresh(preview.rig != null ? preview.rig.root : null);
            }

            public int AppendCapsules(BodyCapsule[] into, int start)
            {
                return _meshes.Append(into, start, anchor.position.y + TrampleZone.BodyReach, TrampleZone.MaxCapsules);
            }
        }

        protected override bool shouldStartGame { get { return false; } }

        protected override IEnumerator Run()
        {
            // The stage's own grass would publish its ground over the lab's
            _manager.enabled = false;
            foreach (LookController owner in Object.FindObjectsByType<LookController>())
            {
                owner.enabled = false;
            }

            string folder = Path.Combine(StagePlay.CaptureFolder, "grass-lab");
            Directory.CreateDirectory(folder);
            QualitySettings.renderPipeline = RenderAssets.Load<RenderPipelineAsset>(pipelinePath);
            Camera camera = CreateCamera();
            LookController look = Fixture("GrassLabLook").AddComponent<LookController>();
            LookSettings settings = LookSettings.Default;
            settings.fogStart = 25f;
            settings.fogEnd = 60f;
            look.settings = settings;
            ZoneRegistry registry = Fixture("GrassLabZones").AddComponent<ZoneRegistry>();
            registry.Init();
            CreateGround();
            GrassField field = CreateField(camera, registry);

            Transform walker = Creature("NormalEntity", Entity.EntityType.Player, new Vector3(-3.2f, 0f, 0.5f),
                                        registry);
            Transform stone = Creature("SoldierEntity", Entity.EntityType.Computer, new Vector3(2.2f, 0f, 2.2f),
                                       registry);
            Transform healed = Creature("NormalEntity", Entity.EntityType.Player, new Vector3(-1.5f, 0f, -1.5f),
                                        registry);
            if (_creatures.Count < 3)
            {
                Debug.LogError("[GrassLabRun] The lab needs its three creatures.");
                StagePlay.Finish(this, false);
                yield break;
            }

            List<Texture2D> film = new List<Texture2D>();
            List<Texture2D> maps = new List<Texture2D>();
            StringBuilder csv = new StringBuilder("frame,time");
            for (int i = 0; i < probes.Length; i++)
            {
                csv.Append($",lean{i}x,lean{i}z,crush{i}");
            }
            csv.Append('\n');

            for (int frame = 0; frame < frames; frame++)
            {
                float time = frame * step;
                // The walker crosses in three seconds, then stands
                float walk = Mathf.Clamp01(time / 3f);
                walker.position = new Vector3(Mathf.Lerp(-3.2f, 3.2f, walk), 0f, 0.5f + 0.4f * Mathf.Sin(walk * 5f));
                foreach (LabCreature creature in _creatures)
                {
                    creature.preview.Tick(time, step, new FootFrame(creature.anchor.position, Vector3.up, 1f),
                                          Vector3.back);
                }
                if (frame == 36)
                {
                    registry.AddHealPulse(healed, 1.6f);
                }

                if (frame == 108)
                {
                    registry.AddLaunch(new Vector3(-3.5f, 0f, -3f), new Vector3(3f, 0f, -0.5f));
                }

                // A gust sweeps across from the left for the last second
                float gust = Mathf.Clamp01((time - 3.3f) / 0.9f);
                field.gust = Vector3.right * Mathf.Sin(gust * Mathf.PI);

                registry.PublishFrame(step);
                field.UpdateField(registry, step, time);
                look.ApplyGlobals();
                Color[] motion = field.ground != null ? Read(field.ground.motion) : null;
                Color[] crush = field.ground != null ? Read(field.ground.crush) : null;
                Probe(field.ground, motion, crush, frame, time, csv);
                if (frame % filmEvery == 0)
                {
                    maps.Add(Map(field.ground, motion, crush));
                    Texture2D shot = StageReadback.Render(camera, frameWidth, frameHeight);
                    film.Add(shot);
                    if (frame % (filmEvery * 6) == 0)
                    {
                        File.WriteAllBytes(Path.Combine(folder, $"frame-{frame:D3}.png"), shot.EncodeToPNG());
                    }
                }

                yield return null;
            }

            File.WriteAllText(Path.Combine(folder, "probes.csv"), csv.ToString());
            WriteSheet(film, Path.Combine(folder, "contact.png"), frameWidth / sheetScale, frameHeight / sheetScale);
            WriteSheet(maps, Path.Combine(folder, "ground-contact.png"), mapSize, mapSize);
            bool isPassed = field.ground != null && field.ground.isValid && film.Count > 0;
            Debug.Log($"[GrassLabRun] {film.Count} film frames, ground {(isPassed ? "live" : "missing")} "
                      + $"in {folder}");
            foreach (Texture2D shot in film)
            {
                Object.Destroy(shot);
            }

            foreach (Texture2D map in maps)
            {
                Object.Destroy(map);
            }

            foreach (LabCreature creature in _creatures)
            {
                registry.RemoveBody(creature);
                creature.preview.Dispose();
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
            go.layer = labLayer;
            _owned.Add(go);
            return go;
        }

        Camera CreateCamera()
        {
            Camera camera = Fixture("GrassLabCamera").AddComponent<Camera>();
            camera.cullingMask = 1 << labLayer;
            camera.fieldOfView = 40f;
            camera.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
            camera.transform.position = -camera.transform.forward * 11.5f + Vector3.back * 0.6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.74f, 0.82f, 0.83f);
            camera.enabled = false;
            Light light = Fixture("GrassLabKey").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = StageKeyLight.Aim(StageKeyLight.KeyDirection);
            light.shadows = LightShadows.Soft;
            RenderSettings.sun = light;
            return camera;
        }

        void CreateGround()
        {
            Material material = new Material(RenderAssets.Load<Shader>(lookShaderPath));
            _owned.Add(material);
            material.SetColor(RenderObjects.BaseColorId, ((Color)new Color32(78, 126, 87, 255)).linear);
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _owned.Add(ground);
            ground.layer = labLayer;
            ground.transform.localScale = new Vector3(8f, 0.2f, 8f);
            ground.transform.position = Vector3.down * 0.1f;
            ground.GetComponent<Renderer>().sharedMaterial = material;
        }

        // The game's look for an entity, grown in full on the lab layer and pressing the grass as a body
        Transform Creature(string entity, Entity.EntityType side, Vector3 position, ZoneRegistry registry)
        {
            GameObject anchor = Fixture("GrassLab" + entity);
            anchor.transform.position = position;
            LabCreature creature = new LabCreature { anchor = anchor.transform };
            EntityData data = LookSheetData.LoadEntity(entity);
            if (!creature.preview.Init(_manager.creatureLooks, data, side, _manager.meshes, anchor.transform, 1f))
            {
                return anchor.transform;
            }

            creature.preview.CompleteAppearance();
            creature.preview.Tick(0f, 0f, new FootFrame(position, Vector3.up, 1f), Vector3.back);
            foreach (Transform child in anchor.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = labLayer;
            }

            creature.Refresh();
            registry.AddBody(creature);
            _creatures.Add(creature);
            return anchor.transform;
        }

        GrassField CreateField(Camera camera, ZoneRegistry registry)
        {
            GrassField field = Fixture("GrassLabGrass").AddComponent<GrassField>();
            EnvironmentAuthoring.SetGrass(field);
            EnvironmentAuthoring.SetGround(field);
            field.Init(area, 1f, 0f, camera, registry.buffer, ZonePacker.MaxZones);
            return field;
        }

        static void Probe(GroundMotion ground, Color[] motion, Color[] crush, int frame, float time,
                          StringBuilder csv)
        {
            csv.Append(frame.ToString(CultureInfo.InvariantCulture)).Append(',')
               .Append(time.ToString("0.0000", CultureInfo.InvariantCulture));
            foreach (Vector2 probe in probes)
            {
                Color lean = motion != null ? At(ground.volume, motion, probe) : Color.clear;
                Color flat = crush != null ? At(ground.volume, crush, probe) : Color.clear;
                csv.Append(',').Append(lean.r.ToString("0.00000", CultureInfo.InvariantCulture))
                   .Append(',').Append(lean.g.ToString("0.00000", CultureInfo.InvariantCulture))
                   .Append(',').Append(flat.r.ToString("0.00000", CultureInfo.InvariantCulture));
            }

            csv.Append('\n');
        }

        static Color[] Read(RenderTexture texture)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = texture;
            Texture2D copy = new Texture2D(texture.width, texture.height, TextureFormat.RGBAFloat, false, true);
            copy.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            copy.Apply();
            RenderTexture.active = previous;
            Color[] pixels = copy.GetPixels();
            Object.Destroy(copy);
            return pixels;
        }

        static Color At(GroundVolume volume, Color[] pixels, Vector2 world)
        {
            Vector2 uv = volume.ToUV(world);
            int x = Mathf.Clamp(Mathf.FloorToInt(uv.x * volume.width), 0, volume.width - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(uv.y * volume.height), 0, volume.height - 1);
            return pixels[y * volume.width + x];
        }

        // The ground seen from above, north up: lean east in red and north in green around mid grey, flatness
        // darkening toward blue. The field's own area is outlined.
        static Texture2D Map(GroundMotion ground, Color[] motion, Color[] crush)
        {
            Texture2D map = new Texture2D(mapSize, mapSize, TextureFormat.RGB24, false);
            Color[] pixels = new Color[mapSize * mapSize];
            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    Color colour = Color.black;
                    if (ground != null)
                    {
                        Vector2 world = ground.volume.ToWorld(new Vector2((x + 0.5f) / mapSize, (y + 0.5f) / mapSize));
                        Color lean = At(ground.volume, motion, world);
                        float flat = At(ground.volume, crush, world).r;
                        colour = new Color(0.5f + lean.r, 0.5f + lean.g, 0.5f) * (1f - 0.6f * flat);
                        colour.b += 0.4f * flat;
                        bool isEdge = Mathf.Abs(Mathf.Abs(world.x) - area.xMax) < 0.04f && Mathf.Abs(world.y) < area.yMax
                                      || Mathf.Abs(Mathf.Abs(world.y) - area.yMax) < 0.04f && Mathf.Abs(world.x) < area.xMax;
                        if (isEdge)
                        {
                            colour = Color.white;
                        }
                    }

                    pixels[y * mapSize + x] = colour;
                }
            }

            map.SetPixels(pixels);
            map.Apply();
            return map;
        }

        // Frames at the given cell size in rows of sheetColumns, first frame top left
        static void WriteSheet(List<Texture2D> film, string path, int width, int height)
        {
            int rows = (film.Count + sheetColumns - 1) / sheetColumns;
            Texture2D sheet = new Texture2D(width * sheetColumns, height * rows, TextureFormat.RGB24, false);
            Color[] fill = new Color[sheet.width * sheet.height];
            for (int i = 0; i < fill.Length; i++)
            {
                fill[i] = Color.white;
            }
            sheet.SetPixels(fill);
            for (int i = 0; i < film.Count; i++)
            {
                int column = i % sheetColumns;
                int row = rows - 1 - i / sheetColumns;
                Color[] pixels = new Color[width * height];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        pixels[y * width + x] = film[i].GetPixel(x * film[i].width / width,
                                                                 y * film[i].height / height);
                    }
                }
                sheet.SetPixels(column * width, row * height, width, height, pixels);
            }

            sheet.Apply();
            File.WriteAllBytes(path, sheet.EncodeToPNG());
            Object.Destroy(sheet);
        }
    }
}
