using System;
using System.Collections.Generic;
using System.IO;
using HealerLike.Render.Grammar;
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
        // any channel moves by more than ReadabilityPixels.Threshold. The grass behind and around is a third render
        // with the effect's and the creature's renderers off, read over those pixels and a RingPixels band around them.
        // The creature's own pixels are the ones its renderers change between the second and third renders: its
        // contrast against the field is read the same way, and its silhouette survival is the fraction of them the
        // effect leaves unchanged.
        const int PhoneWidth = 1080;
        const int PhoneHeight = 1920;
        const int RingPixels = 24;
        readonly StringBuilder readability = new StringBuilder("element,material,target,age,cycleSeconds,lifetimeSeconds," +
            "effectPixels,backgroundPixels,effectR,effectG,effectB,grassR,grassG,grassB,effectLuma,grassLuma,lumaDifference," +
            "rgbDistance,creaturePixels,silhouetteSurvival,creatureR,creatureG,creatureB,creatureFieldR,creatureFieldG," +
            "creatureFieldB,creatureLuma,creatureFieldLuma,creatureLumaDifference,creatureRgbDistance,stacks\n");
        readonly StringBuilder pairs = new StringBuilder("first,second,firstPixels,secondPixels,firstR,firstG,firstB," +
            "secondR,secondG,secondB,firstLuma,secondLuma,lumaDifference,rgbDistance\n");
        public readonly List<ReadabilityRow> rows = new List<ReadabilityRow>();
        public readonly List<PairRow> pairRows = new List<PairRow>();
        Color32[] manaUp;
        Color32[] manaDown;
        string resolved = "";

        [Serializable]
        public class ReadabilityRow
        {
            public string element, material, target;
            public float age, cycleSeconds, lifetimeSeconds;
            public int stacks;
            public int effectPixels, backgroundPixels, creaturePixels;
            public double effectLuma, grassLuma, lumaDifference, rgbDistance;
            public double silhouetteSurvival;
            public double creatureLuma, creatureFieldLuma, creatureLumaDifference, creatureRgbDistance;
        }

        [Serializable]
        public class PairRow
        {
            public string first, second;
            public int firstPixels, secondPixels;
            public double firstLuma, secondLuma, lumaDifference, rgbDistance;
        }

        public void Resolved(EffectKey gain, EffectKey drain)
        {
            resolved = $"ManaOnRoundEndItem resolves to {gain}, SiphonItem (DrainCharacterMana) resolves to {drain}";
        }

        public void Readability(Camera camera, GameObject creature, GameObject effectHost, EffectKey element, float age,
                                EffectRecipe recipe, float lifetime, LookSide target,
                                int stacks = SpellReadabilityPass.DefaultStacks)
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
                ReadabilityPixels.Contrast effect = ReadabilityPixels.Measure(ReadabilityPixels.Changed(a, b), a, c,
                    PhoneWidth, PhoneHeight, RingPixels);
                bool[] body = ReadabilityPixels.Changed(b, c);
                ReadabilityPixels.Contrast creatureContrast = ReadabilityPixels.Measure(body, b, c, PhoneWidth, PhoneHeight,
                    RingPixels);
                double survival = ReadabilityPixels.Survival(body, a, b);
                int background = ReadabilityPixels.Count(ReadabilityPixels.Grow(ReadabilityPixels.Changed(a, b),
                    PhoneWidth, PhoneHeight, RingPixels));
                readability.Append(element).Append(',').Append(recipe.material).Append(',').Append(target);
                foreach (double value in new double[] { age, recipe.cycleSeconds, lifetime, effect.pixels, background,
                    effect.shape[0], effect.shape[1], effect.shape[2], effect.field[0], effect.field[1], effect.field[2],
                    effect.shapeLuma, effect.fieldLuma, effect.lumaDifference, effect.rgbDistance, creatureContrast.pixels,
                    survival, creatureContrast.shape[0], creatureContrast.shape[1], creatureContrast.shape[2],
                    creatureContrast.field[0], creatureContrast.field[1], creatureContrast.field[2],
                    creatureContrast.shapeLuma, creatureContrast.fieldLuma, creatureContrast.lumaDifference,
                    creatureContrast.rgbDistance })
                    readability.Append(',').Append(value.ToString("0.###", CultureInfo.InvariantCulture));
                readability.Append(',').Append(stacks).AppendLine();
                rows.Add(new ReadabilityRow
                {
                    element = element.ToString(), material = recipe.material.ToString(), target = target.ToString(),
                    age = age, cycleSeconds = recipe.cycleSeconds, lifetimeSeconds = lifetime, stacks = stacks,
                    effectPixels = effect.pixels, backgroundPixels = background, creaturePixels = creatureContrast.pixels,
                    effectLuma = effect.shapeLuma, grassLuma = effect.fieldLuma, lumaDifference = effect.lumaDifference,
                    rgbDistance = effect.rgbDistance, silhouetteSurvival = survival,
                    creatureLuma = creatureContrast.shapeLuma, creatureFieldLuma = creatureContrast.fieldLuma,
                    creatureLumaDifference = creatureContrast.lumaDifference,
                    creatureRgbDistance = creatureContrast.rgbDistance
                });
                bool isStone = recipe.material == LookSide.Stone;
                string suffix = (isStone ? "-stone" : "") + (target == LookSide.Stone ? "-on-stone" : "")
                    + (stacks != SpellReadabilityPass.DefaultStacks ? "-" + stacks + "-stack" : "");
                if (element == EffectKey.ManaUp && target == LookSide.Plant) manaUp = a;
                if (element == EffectKey.ManaDown && target == LookSide.Plant) manaDown = a;
                File.WriteAllBytes(Path.Combine(folder, $"{(int)element:D2}-{element}{suffix}-peak.png"), full.EncodeToPNG());
                if (element == EffectKey.Burst && !isStone)
                {
                    File.WriteAllBytes(Path.Combine(folder, $"00-Burst{suffix}-no-effect.png"), bare.EncodeToPNG());
                    File.WriteAllBytes(Path.Combine(folder, $"00-Burst{suffix}-grass-only.png"), grass.EncodeToPNG());
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

        // Two different elements in one frame on two adjacent creatures: each element's pixels are the ones its own
        // renderers change, and the pair reads apart by the distance between their means
        public void Pair(Camera camera, GameObject firstHost, GameObject secondHost, EffectKey first, EffectKey second)
        {
            Renderer[] firstRenderers = Visible(firstHost), secondRenderers = Visible(secondHost);
            Texture2D full = StageReadback.Render(camera, PhoneWidth, PhoneHeight);
            Texture2D withoutFirst = null, withoutSecond = null;
            try
            {
                Show(firstRenderers, false);
                withoutFirst = StageReadback.Render(camera, PhoneWidth, PhoneHeight);
                Show(firstRenderers, true);
                Show(secondRenderers, false);
                withoutSecond = StageReadback.Render(camera, PhoneWidth, PhoneHeight);
                Show(secondRenderers, true);
                Color32[] a = full.GetPixels32();
                bool[] firstMask = ReadabilityPixels.Changed(a, withoutFirst.GetPixels32());
                bool[] secondMask = ReadabilityPixels.Changed(a, withoutSecond.GetPixels32());
                double[] firstMean = ReadabilityPixels.Mean(a, firstMask), secondMean = ReadabilityPixels.Mean(a, secondMask);
                PairRow row = new PairRow
                {
                    first = first.ToString(), second = second.ToString(),
                    firstPixels = ReadabilityPixels.Count(firstMask), secondPixels = ReadabilityPixels.Count(secondMask),
                    firstLuma = ReadabilityPixels.Luma(firstMean), secondLuma = ReadabilityPixels.Luma(secondMean),
                    rgbDistance = ReadabilityPixels.Distance(firstMean, secondMean)
                };
                row.lumaDifference = row.firstLuma - row.secondLuma;
                pairRows.Add(row);
                pairs.Append(first).Append(',').Append(second);
                foreach (double value in new double[] { row.firstPixels, row.secondPixels, firstMean[0], firstMean[1],
                    firstMean[2], secondMean[0], secondMean[1], secondMean[2], row.firstLuma, row.secondLuma,
                    row.lumaDifference, row.rgbDistance })
                    pairs.Append(',').Append(value.ToString("0.###", CultureInfo.InvariantCulture));
                pairs.AppendLine();
                File.WriteAllBytes(Path.Combine(folder, $"pair-{first}-{second}.png"), full.EncodeToPNG());
            }
            finally
            {
                Show(firstRenderers, true);
                Show(secondRenderers, true);
                RenderObjects.Release(full);
                RenderObjects.Release(withoutFirst);
                RenderObjects.Release(withoutSecond);
            }
        }

        public void WriteReadability()
        {
            File.WriteAllText(Path.Combine(folder, "readability.csv"), readability.ToString());
            File.WriteAllText(Path.Combine(folder, "pairs.csv"), pairs.ToString());
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

        public void Dispose() { RenderObjects.Release(sheet); }
    }
}
