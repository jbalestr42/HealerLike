using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // Reuse the game's semantic glyphs without the generated circular icon frames.
    public sealed class StageSpellSymbols : IDisposable
    {
        readonly Dictionary<DataIconSymbol, Texture2D> _images = new Dictionary<DataIconSymbol, Texture2D>();

        public Texture2D Get(object source)
        {
            DataIconSymbol symbol = DataIconDescriptor.From(source).symbol;
            if (_images.TryGetValue(symbol, out Texture2D image))
            {
                return image;
            }

            const int size = 80;
            var pixels = new Color[size * size];
            Color ink = symbol == DataIconSymbol.Poison ? new Color32(91, 112, 46, 255)
                : symbol == DataIconSymbol.Bolt || symbol == DataIconSymbol.Flame ? new Color32(148, 83, 52, 255)
                : new Color32(37, 100, 86, 255);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
            {
                int covered = 0;
                for (int sy = 0; sy < 2; sy++)
                    {
                        for (int sx = 0; sx < 2; sx++)
                        {
                            if (DataIconGlyph.Contains(DataIconKind.Spell, symbol,
                        ((x + (sx + 0.5f) / 2f) / size * 2f - 1f) * 0.7f,
                        ((y + (sy + 0.5f) / 2f) / size * 2f - 1f) * 0.7f))
                            {
                                covered++;
                            }
                        }
                    }

                    Color colour = ink;
                colour.a = covered / 4f;
                pixels[y * size + x] = colour;
            }
            }

            image = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Render spell " + symbol, hideFlags = HideFlags.DontSave,
                filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
            };
            image.SetPixels(pixels);
            image.Apply(false, true);
            _images.Add(symbol, image);
            return image;
        }

        public void Dispose()
        {
            foreach (Texture2D image in _images.Values)
            {
                RenderObjects.Release(image);
            }

            _images.Clear();
        }
    }
}
