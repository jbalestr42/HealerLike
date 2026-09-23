using System.Collections.Generic;
using HealerLike.Render.Grass;
using HealerLike.Render.Stage;
using HealerLike.Render.Zones;
using UnityEngine;

namespace HealerLike.Render.Environment
{
    // Grass carpet around the board: one GrassField per strip, copied from the strip template.
    // The strips borrow the zone buffer with a count of zero, so zones stay on the board.
    public class EnvironmentGrass : MonoBehaviour
    {
        public static readonly float[] DefaultWidths = { 3f, 5f, 16f };
        public static readonly float[] DefaultFractions = { 0.85f, 0.6f, 0.15f };
        public static readonly float BoardDensity = 256f;

        [SerializeField] GrassField _stripTemplate;
        [SerializeField] float[] _widths = { 3f, 5f, 16f };
        [SerializeField] float[] _fractions = { 0.85f, 0.6f, 0.15f };

        List<GrassField> _strips = new List<GrassField>();

        public IReadOnlyList<GrassField> strips { get { return _strips; } }

        public void Init(Rect board, float cellSize, float surfaceY, Camera camera, ZoneRegistry zones, RenderManager manager)
        {
            foreach (GrassField strip in _strips)
            {
                Destroy(strip.gameObject);
            }

            _strips.Clear();
            if (_stripTemplate == null || zones == null)
            {
                Debug.LogError("[EnvironmentGrass] Init needs the strip template and the zone registry.");
                return;
            }

            float boardDensity = GrassLayout.DefaultBudget / (board.width * board.height);
            RingStrip[] bands = Bands(board, _widths, _fractions, boardDensity);
            for (int i = 0; i < bands.Length; i++)
            {
                GrassField strip = Instantiate(_stripTemplate, transform);
                strip.name = "GrassStrip" + i + "_band" + bands[i].band;
                strip.bladeBudget = bands[i].budget;
                strip.seed = (uint)(11 + i);
                strip.Init(bands[i].rect, cellSize, surfaceY, camera, zones.buffer, GrassField.MaxZones);
                strip.gameObject.SetActive(true);
                _strips.Add(strip);
            }
        }

        public void UpdateStrips(ZoneRegistry zones)
        {
            GraphicsBuffer buffer = zones != null ? zones.buffer : null;
            foreach (GrassField strip in _strips)
            {
                strip.UpdateField(buffer, 0);
            }
        }

        void OnDisable()
        {
            foreach (GrassField strip in _strips)
            {
                if (strip)
                {
                    strip.SetZoneSnapshot(null, 0);
                }
            }
        }

        // Top, bottom, left and right strips of the given width; they tile the ring without overlap
        public static Rect[] Strips(Rect grid, float ring)
        {
            if (!float.IsFinite(ring) || ring <= 0f)
            {
                Debug.LogError($"[EnvironmentGrass] Rejected ring width {ring}.");
                return new Rect[0];
            }

            return new Rect[]
            {
                new Rect(grid.xMin - ring, grid.yMax, grid.width + 2f * ring, ring),
                new Rect(grid.xMin - ring, grid.yMin - ring, grid.width + 2f * ring, ring),
                new Rect(grid.xMin - ring, grid.yMin, ring, grid.height),
                new Rect(grid.xMax, grid.yMin, ring, grid.height)
            };
        }

        public static int Budget(Rect strip, float density)
        {
            int blades = Mathf.RoundToInt(strip.width * strip.height * Mathf.Max(0f, density));
            return Mathf.Clamp(blades, 0, GrassLayout.MaxBudget);
        }

        public static RingStrip[] Bands(Rect grid)
        {
            return Bands(grid, DefaultWidths, DefaultFractions, BoardDensity);
        }

        // Concentric bands of whole-cell widths, each as four strips at boardDensity * fraction.
        // A strip over the grass budget is cut along its length into whole-cell pieces so no density is lost.
        // Returns no strips and logs when an input is not valid.
        public static RingStrip[] Bands(Rect grid, float[] widths, float[] densityFractions, float boardDensity)
        {
            if (widths == null || densityFractions == null || widths.Length == 0 || widths.Length != densityFractions.Length)
            {
                Debug.LogError("[EnvironmentGrass] Widths and density fractions must be non-empty and of equal length.");
                return new RingStrip[0];
            }

            if (!float.IsFinite(boardDensity) || boardDensity <= 0f)
            {
                Debug.LogError($"[EnvironmentGrass] Rejected board density {boardDensity}.");
                return new RingStrip[0];
            }

            bool isWholeGrid = Mathf.Approximately(grid.width, Mathf.Round(grid.width)) && Mathf.Approximately(grid.height, Mathf.Round(grid.height));
            if (grid.width <= 0f || grid.height <= 0f || !isWholeGrid)
            {
                Debug.LogError($"[EnvironmentGrass] Rejected grid {grid}, it needs a whole number of cells.");
                return new RingStrip[0];
            }

            for (int band = 0; band < widths.Length; band++)
            {
                float width = widths[band];
                float fraction = densityFractions[band];
                if (!Mathf.Approximately(width, Mathf.Round(width)) || width < 1f || !float.IsFinite(fraction) || fraction <= 0f)
                {
                    Debug.LogError($"[EnvironmentGrass] Rejected band {band}: width {width}, density fraction {fraction}.");
                    return new RingStrip[0];
                }
            }

            List<RingStrip> result = new List<RingStrip>();
            Rect inner = grid;
            for (int band = 0; band < widths.Length; band++)
            {
                float width = widths[band];
                foreach (Rect strip in Strips(inner, width))
                {
                    Split(strip, boardDensity * densityFractions[band], band, result);
                }

                inner = new Rect(inner.xMin - width, inner.yMin - width, inner.width + 2f * width, inner.height + 2f * width);
            }

            return result.ToArray();
        }

        static void Split(Rect strip, float density, int band, List<RingStrip> into)
        {
            bool isWide = strip.width >= strip.height;
            int length = Mathf.RoundToInt(isWide ? strip.width : strip.height);
            int across = Mathf.RoundToInt(isWide ? strip.height : strip.width);
            int pieces = Mathf.Clamp(Mathf.CeilToInt(length * across * density / GrassLayout.MaxBudget), 1, length);
            while (pieces < length && (length + pieces - 1) / pieces * across * density > GrassLayout.MaxBudget)
            {
                pieces++;
            }

            int start = 0;
            for (int i = 0; i < pieces; i++)
            {
                int end = (i + 1) * length / pieces;
                Rect rect;
                if (isWide)
                {
                    rect = new Rect(strip.xMin + start, strip.yMin, end - start, strip.height);
                }
                else
                {
                    rect = new Rect(strip.xMin, strip.yMin + start, strip.width, end - start);
                }

                into.Add(new RingStrip
                {
                    rect = rect,
                    band = band,
                    density = density,
                    budget = Budget(rect, density)
                });
                start = end;
            }
        }
    }
}
