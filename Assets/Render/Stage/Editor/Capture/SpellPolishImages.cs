using System;
using System.IO;
using HealerLike.Render.Spells;
using HealerLike.Render.Grass;
using System.Text;
using System.Globalization;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // Each row is one family; columns are opening, readable peak and departure at identical framing.
    public sealed class SpellPolishImages : IDisposable
    {
        const int Width = 960;
        const int Height = 720;
        const int TileWidth = 320;
        const int TileHeight = 260;
        readonly string folder;
        readonly Texture2D sheet;
        readonly StringBuilder metrics = new StringBuilder(
            "element,moment,age,maxLean,maxAsh,minVitality,maxVitality,minLight,maxLight,maxBlight\n");

        public SpellPolishImages(string folder)
        {
            this.folder = folder;
            sheet = new Texture2D(TileWidth * 3, TileHeight * 14, TextureFormat.RGB24, false);
            Color32[] pixels = new Color32[sheet.width * sheet.height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(20, 28, 28, 255);
            sheet.SetPixels32(pixels);
        }

        public void Capture(Camera camera, EffectElement element, int moment, float age)
        {
            Texture2D shot = StageReadback.Render(camera, Width, Height);
            try
            {
                string label = element + "  " + age.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "S";
                int left = moment * TileWidth;
                int bottom = (13 - (int)element) * TileHeight;
                Color32[] source = shot.GetPixels32();
                Color32[] tile = new Color32[TileWidth * 240];
                for (int y = 0; y < 240; y++)
                    for (int x = 0; x < TileWidth; x++) tile[y * TileWidth + x] = source[(y * 3) * Width + x * 3];
                sheet.SetPixels32(left, bottom, TileWidth, 240, tile);
                LookSheetFont.Draw(sheet, label, left + 8, bottom + TileHeight - 5, 1, Color.white);
                LookSheetFont.Draw(shot, label + " (FIXTURE)", 18, Height - 18, 2, Color.white);
                shot.Apply();
                File.WriteAllBytes(Path.Combine(folder, $"{(int)element:D2}-{element}-{moment + 1}.png"), shot.EncodeToPNG());
            }
            finally { RenderObjects.Release(shot); }
        }

        public void Measure(GroundSimulation simulation, EffectElement element, int moment, float age)
        {
            if (simulation == null || !simulation.isValid)
                throw new InvalidOperationException("Spell fixture has no live GPU ground simulation.");
            Color[] motion = StageCaptureTexture.Read(simulation.motion);
            Color[] state = StageCaptureTexture.Read(simulation.state);
            float lean = 0, ash = 0, lowVitality = 0, vitality = 0, lowLight = 0, light = 0, blight = 0;
            foreach (Color pixel in motion) lean = Mathf.Max(lean, new Vector2(pixel.r, pixel.g).magnitude);
            foreach (Color pixel in state)
            {
                ash = Mathf.Max(ash, pixel.r);
                lowVitality = Mathf.Min(lowVitality, pixel.g);
                vitality = Mathf.Max(vitality, pixel.g);
                lowLight = Mathf.Min(lowLight, pixel.b);
                light = Mathf.Max(light, pixel.b);
                blight = Mathf.Max(blight, pixel.a);
            }
            metrics.Append(element).Append(',').Append(moment + 1);
            foreach (float value in new[] { age, lean, ash, lowVitality, vitality, lowLight, light, blight })
                metrics.Append(',').Append(value.ToString("0.00000", CultureInfo.InvariantCulture));
            metrics.AppendLine();
        }

        public void Write()
        {
            File.WriteAllText(Path.Combine(folder, "ground-metrics.csv"), metrics.ToString());
            sheet.Apply();
            File.WriteAllBytes(Path.Combine(folder, "contact.png"), sheet.EncodeToPNG());
        }

        public void Dispose() { RenderObjects.Release(sheet); }
    }
}
