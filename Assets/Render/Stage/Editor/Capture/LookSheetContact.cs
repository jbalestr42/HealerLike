using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // A labelled contact sheet: for each cell its colour, greyscale and deuteranopia crops side by side, one label
    // under the three, and a title across the top. Crops are enlarged by whole pixels to about CellPixels
    public class LookSheetContact
    {
        public static readonly int Columns = 4;
        public static readonly int CellPixels = 330;
        public static readonly int LabelScale = 3;
        public static readonly int Gap = 6;

        static readonly Color32 background = new Color32(24, 26, 30, 255);

        readonly List<string> _labels = new List<string>();
        readonly List<Color32[][]> _panels = new List<Color32[][]>();
        int _size;

        public int count { get { return _labels.Count; } }

        public LookSheetContact(int size)
        {
            _size = size;
        }

        public void Add(string label, Color32[] colour, Color32[] grey, Color32[] deuteranope)
        {
            if (colour.Length != _size * _size || grey.Length != colour.Length || deuteranope.Length != colour.Length)
            {
                Debug.LogError($"[LookSheetContact] {label} has crops that are not {_size} px square");
                return;
            }

            _labels.Add(label);
            _panels.Add(new Color32[][] { colour, grey, deuteranope });
        }

        public Texture2D Build(string title)
        {
            int zoom = Mathf.Max(1, CellPixels / _size);
            int side = _size * zoom;
            int labelHeight = 8 * LabelScale + 12;
            int cellWidth = side * 3 + Gap * 3;
            int cellHeight = side + labelHeight;
            int rows = Mathf.Max(1, Mathf.CeilToInt((float)_labels.Count / Columns));
            int width = cellWidth * Columns;
            int height = cellHeight * rows + labelHeight;
            Color32[] sheet = new Color32[width * height];
            for (int i = 0; i < sheet.Length; i++)
            {
                sheet[i] = background;
            }

            for (int i = 0; i < _labels.Count; i++)
            {
                int left = (i % Columns) * cellWidth;
                int bottom = (rows - 1 - i / Columns) * cellHeight + labelHeight;
                for (int panel = 0; panel < 3; panel++)
                {
                    Blit(_panels[i][panel], _size, zoom, sheet, width, left + panel * (side + Gap), bottom);
                }
            }

            Texture2D texture = LookSheetImage.ToTexture(sheet, width, height);
            LookSheetFont.Draw(texture, title, Gap, height - 6, LabelScale, Color.white);
            for (int i = 0; i < _labels.Count; i++)
            {
                int left = (i % Columns) * cellWidth;
                int bottom = (rows - 1 - i / Columns) * cellHeight;
                LookSheetFont.Draw(texture, _labels[i], left + Gap, bottom + labelHeight - 6, LabelScale, Color.white);
            }
            texture.Apply();
            return texture;
        }

        static void Blit(Color32[] crop, int size, int zoom, Color32[] sheet, int sheetWidth, int left, int bottom)
        {
            for (int row = 0; row < size * zoom; row++)
            {
                for (int column = 0; column < size * zoom; column++)
                {
                    sheet[(bottom + row) * sheetWidth + left + column] = crop[(row / zoom) * size + column / zoom];
                }
            }
        }
    }
}
