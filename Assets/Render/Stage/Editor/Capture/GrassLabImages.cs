using System.Collections.Generic;
using System.IO;
using UnityEngine;
using HealerLike.Render.Grass;

namespace HealerLike.Render.Stage
{
    // Owns the lab's retained film and map textures until the capture finishes or is cancelled.
    public class GrassLabImages : System.IDisposable
    {
        static readonly int frameWidth = 720;
        static readonly int frameHeight = 480;
        static readonly int sheetColumns = 6;
        static readonly int sheetScale = 2;
        static readonly int mapSize = 240;
        static readonly Rect area = GrassLabScene.Area;
        readonly List<Texture2D> _owned = new List<Texture2D>();
        readonly List<Texture2D> _film = new List<Texture2D>();
        readonly List<Texture2D> _maps = new List<Texture2D>();
        readonly List<Texture2D> _states = new List<Texture2D>();

        public int frameCount { get { return _film.Count; } }

        public void Capture(GroundSimulation ground, Color[] motion, Color[] crush, Camera camera,
                            int frame, string folder)
        {
            _maps.Add(Map(ground, motion, crush));
            _states.Add(StateMap(ground, ground != null ? StageCaptureTexture.Read(ground.state) : null));
            Texture2D shot = Own(StageReadback.Render(camera, frameWidth, frameHeight));
            _film.Add(shot);
            if (frame % 54 == 0)
            {
                File.WriteAllBytes(Path.Combine(folder, $"frame-{frame:D3}.png"), shot.EncodeToPNG());
            }
        }

        public void Write(string folder)
        {
            WriteSheet(_film, Path.Combine(folder, "contact.png"), frameWidth / sheetScale, frameHeight / sheetScale);
            WriteSheet(_maps, Path.Combine(folder, "ground-contact.png"), mapSize, mapSize);
            WriteSheet(_states, Path.Combine(folder, "state-contact.png"), mapSize, mapSize);
        }

        Texture2D Own(Texture2D texture)
        {
            _owned.Add(texture);
            return texture;
        }

        public void Dispose()
        {
            foreach (Texture2D texture in _owned)
            {
                RenderObjects.Release(texture);
            }
            _owned.Clear();
            _film.Clear();
            _maps.Clear();
            _states.Clear();
        }

        Texture2D Map(GroundSimulation ground, Color[] motion, Color[] crush)
        {
            Texture2D map = Own(new Texture2D(mapSize, mapSize, TextureFormat.RGB24, false));
            Color[] pixels = new Color[mapSize * mapSize];
            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    Color colour = Color.black;
                    if (ground != null)
                    {
                        Vector2 world = ground.volume.ToWorld(new Vector2((x + 0.5f) / mapSize, (y + 0.5f) / mapSize));
                        Color lean = GrassLabMeasurements.At(ground.volume, motion, world);
                        float flat = GrassLabMeasurements.At(ground.volume, crush, world).r;
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

        Texture2D StateMap(GroundSimulation ground, Color[] state)
        {
            Texture2D map = Own(new Texture2D(mapSize, mapSize, TextureFormat.RGB24, false));
            Color[] pixels = new Color[mapSize * mapSize];
            Color grass = new Color(0.3f, 0.62f, 0.3f);
            Color ash = new Color(0.55f, 0.55f, 0.53f);
            Color straw = new Color(0.75f, 0.65f, 0.35f);
            Color lush = new Color(0.55f, 0.95f, 0.25f);
            Color glow = new Color(1f, 0.95f, 0.5f);
            Color frost = new Color(0.85f, 0.93f, 1f);
            Color blight = new Color(0.55f, 0.4f, 0.65f);
            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    Color colour = Color.black;
                    if (ground != null && state != null)
                    {
                        Vector2 world = ground.volume.ToWorld(new Vector2((x + 0.5f) / mapSize, (y + 0.5f) / mapSize));
                        Color value = GrassLabMeasurements.At(ground.volume, state, world);
                        colour = Color.Lerp(grass, lush, Mathf.Clamp01(value.g));
                        colour = Color.Lerp(colour, straw, Mathf.Clamp01(-value.g));
                        colour = Color.Lerp(colour, ash, Mathf.Clamp01(value.r));
                        colour = Color.Lerp(colour, blight, Mathf.Clamp01(value.a));
                        colour = Color.Lerp(colour, frost, Mathf.Clamp01(-value.b));
                        colour = Color.Lerp(colour, glow, Mathf.Clamp01(value.b));
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
        void WriteSheet(List<Texture2D> film, string path, int width, int height)
        {
            int rows = (film.Count + sheetColumns - 1) / sheetColumns;
            Texture2D sheet = Own(new Texture2D(width * sheetColumns, height * rows, TextureFormat.RGB24, false));
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
        }
    }
}
