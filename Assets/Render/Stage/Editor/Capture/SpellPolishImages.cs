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

        public void Capture(Camera camera, EffectKey element, int moment, float age)
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

        public void Measure(GroundSimulation simulation, EffectKey element, int moment, float age)
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

        // Readability at the shipped portrait size. Effect pixels are the ones the effect's own renderers change:
        // the frame is rendered with everything, then with the effect's renderers off; a pixel is the effect's when
        // any channel moves by more than EffectThreshold. The grass behind and around is a third render with the
        // effect's and the creature's renderers off, read over those pixels and a RingPixels band around them.
        const int PhoneWidth = 1080;
        const int PhoneHeight = 1920;
        const int EffectThreshold = 6;
        const int RingPixels = 24;
        readonly StringBuilder readability = new StringBuilder("element,age,cycleSeconds,lifetimeSeconds,effectPixels," +
            "backgroundPixels,effectR,effectG,effectB,grassR,grassG,grassB,effectLuma,grassLuma,lumaDifference,rgbDistance\n");
        Color32[] manaUp;
        Color32[] manaDown;
        string resolved = "";

        public void Resolved(EffectKey gain, EffectKey drain)
        {
            resolved = $"ManaOnRoundEndItem resolves to {gain}, SiphonItem (DrainCharacterMana) resolves to {drain}";
        }

        public void Readability(Camera camera, GameObject creature, GameObject effectHost, EffectKey element, float age,
                                EffectRecipe recipe, float lifetime)
        {
            Renderer[] effectRenderers = Visible(effectHost), creatureRenderers = Visible(creature);
            Texture2D full = StageReadback.Render(camera, PhoneWidth, PhoneHeight);
            Texture2D bare = null, grass = null;
            try
            {
                Show(effectRenderers, false);
                bare = StageReadback.Render(camera, PhoneWidth, PhoneHeight);
                Show(creatureRenderers, false);
                grass = StageReadback.Render(camera, PhoneWidth, PhoneHeight);
                Show(effectRenderers, true);
                Show(creatureRenderers, true);
                Color32[] a = full.GetPixels32(), b = bare.GetPixels32(), c = grass.GetPixels32();
                int[] sum = new int[(PhoneWidth + 1) * (PhoneHeight + 1)];
                bool[] isEffect = new bool[a.Length];
                for (int y = 0; y < PhoneHeight; y++)
                    for (int x = 0; x < PhoneWidth; x++)
                    {
                        int i = y * PhoneWidth + x;
                        isEffect[i] = Mathf.Max(Mathf.Abs(a[i].r - b[i].r), Mathf.Abs(a[i].g - b[i].g),
                            Mathf.Abs(a[i].b - b[i].b)) > EffectThreshold;
                        sum[(y + 1) * (PhoneWidth + 1) + x + 1] = (isEffect[i] ? 1 : 0) + sum[y * (PhoneWidth + 1) + x + 1]
                            + sum[(y + 1) * (PhoneWidth + 1) + x] - sum[y * (PhoneWidth + 1) + x];
                    }
                double[] effect = new double[3], ground = new double[3];
                int effectCount = 0, groundCount = 0;
                for (int y = 0; y < PhoneHeight; y++)
                    for (int x = 0; x < PhoneWidth; x++)
                    {
                        int i = y * PhoneWidth + x;
                        if (isEffect[i]) { effectCount++; Add(effect, a[i]); }
                        int x0 = Mathf.Max(0, x - RingPixels), x1 = Mathf.Min(PhoneWidth, x + RingPixels + 1);
                        int y0 = Mathf.Max(0, y - RingPixels), y1 = Mathf.Min(PhoneHeight, y + RingPixels + 1);
                        int near = sum[y1 * (PhoneWidth + 1) + x1] - sum[y0 * (PhoneWidth + 1) + x1]
                            - sum[y1 * (PhoneWidth + 1) + x0] + sum[y0 * (PhoneWidth + 1) + x0];
                        if (near > 0) { groundCount++; Add(ground, c[i]); }
                    }
                for (int k = 0; k < 3; k++)
                {
                    effect[k] /= Mathf.Max(1, effectCount);
                    ground[k] /= Mathf.Max(1, groundCount);
                }
                double effectLuma = Luma(effect), groundLuma = Luma(ground);
                double distance = System.Math.Sqrt(System.Math.Pow(effect[0] - ground[0], 2) + System.Math.Pow(effect[1] - ground[1], 2)
                    + System.Math.Pow(effect[2] - ground[2], 2));
                readability.Append(element);
                foreach (double value in new double[] { age, recipe.cycleSeconds, lifetime, effectCount, groundCount,
                    effect[0], effect[1], effect[2], ground[0], ground[1], ground[2], effectLuma, groundLuma,
                    effectLuma - groundLuma, distance })
                    readability.Append(',').Append(value.ToString("0.###", CultureInfo.InvariantCulture));
                readability.AppendLine();
                if (element == EffectKey.ManaUp) manaUp = a;
                if (element == EffectKey.ManaDown) manaDown = a;
                File.WriteAllBytes(Path.Combine(folder, $"{(int)element:D2}-{element}-peak.png"), full.EncodeToPNG());
                if (element == EffectKey.Burst)
                {
                    File.WriteAllBytes(Path.Combine(folder, "00-Burst-no-effect.png"), bare.EncodeToPNG());
                    File.WriteAllBytes(Path.Combine(folder, "00-Burst-grass-only.png"), grass.EncodeToPNG());
                }
            }
            finally
            {
                Show(effectRenderers, true);
                Show(creatureRenderers, true);
                RenderObjects.Release(full);
                RenderObjects.Release(bare);
                RenderObjects.Release(grass);
            }
        }

        public void WriteReadability()
        {
            File.WriteAllText(Path.Combine(folder, "readability.csv"), readability.ToString());
            File.WriteAllText(Path.Combine(folder, "mana-resolution.txt"), resolved + "\n");
            if (manaUp == null || manaDown == null) return;
            Texture2D pair = new Texture2D(PhoneWidth * 2, PhoneHeight, TextureFormat.RGB24, false);
            try
            {
                pair.SetPixels32(0, 0, PhoneWidth, PhoneHeight, manaUp);
                pair.SetPixels32(PhoneWidth, 0, PhoneWidth, PhoneHeight, manaDown);
                LookSheetFont.Draw(pair, "MANAUP (GAIN)", 24, PhoneHeight - 24, 3, Color.white);
                LookSheetFont.Draw(pair, "MANADOWN (DRAIN)", PhoneWidth + 24, PhoneHeight - 24, 3, Color.white);
                pair.Apply();
                File.WriteAllBytes(Path.Combine(folder, "mana-up-vs-down.png"), pair.EncodeToPNG());
            }
            finally { RenderObjects.Release(pair); }
        }

        static Renderer[] Visible(GameObject root)
        {
            return System.Array.FindAll(root.GetComponentsInChildren<Renderer>(false), renderer => renderer.enabled);
        }

        static void Show(Renderer[] renderers, bool isShown)
        {
            foreach (Renderer renderer in renderers) renderer.enabled = isShown;
        }

        static void Add(double[] total, Color32 pixel)
        {
            total[0] += pixel.r;
            total[1] += pixel.g;
            total[2] += pixel.b;
        }

        // Rec. 709 weights on the stored sRGB bytes, 0 to 255, as the phone displays them
        static double Luma(double[] rgb) { return .2126 * rgb[0] + .7152 * rgb[1] + .0722 * rgb[2]; }

        public void Dispose() { RenderObjects.Release(sheet); }
    }
}
