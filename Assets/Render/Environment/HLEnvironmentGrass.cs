using System;
using System.Collections.Generic;
using HealerLike.Render.Grass;
using HealerLike.Render.Zones;
using UnityEngine;
using UnityEngine.Serialization;

namespace HealerLike.Render.Environment
{
    // Grass carpet around the grid: HLGrassField strips, each bound to a proxy GridManager
    // that the stage owns and never generates.
    // The strips borrow the zone buffer with a count of zero, so zones stay on the board.
    [DefaultExecutionOrder(0)]
    public class HLEnvironmentGrass : MonoBehaviour
    {
        public static readonly float[] DefaultWidths = { 3f, 5f, 16f };
        public static readonly float[] DefaultFractions = { 0.8f, 0.5f, 0.3f };
        public static readonly float BoardDensity = 256f;

        [FormerlySerializedAs("zoneRegistry")]
        [SerializeField] HLZoneRegistry _zoneRegistry;
        [FormerlySerializedAs("proxies")]
        [SerializeField] GridManager[] _proxies = new GridManager[0];
        [FormerlySerializedAs("fields")]
        [SerializeField] HLGrassField[] _fields = new HLGrassField[0];

        public void Configure(HLZoneRegistry zones, GridManager[] proxyGrids, HLGrassField[] grassFields)
        {
            _zoneRegistry = zones;
            _proxies = proxyGrids != null ? proxyGrids : new GridManager[0];
            _fields = grassFields != null ? grassFields : new HLGrassField[0];
        }

        void Start()
        {
            foreach (GridManager proxy in _proxies)
            {
                EnsureCells(proxy);
            }
        }

        void LateUpdate()
        {
            GraphicsBuffer buffer = _zoneRegistry ? _zoneRegistry.Buffer : null;
            foreach (HLGrassField field in _fields)
            {
                if (field)
                {
                    field.SetZoneSnapshot(buffer, 0);
                }
            }
        }

        void OnDisable()
        {
            foreach (HLGrassField field in _fields)
            {
                if (field)
                {
                    field.SetZoneSnapshot(null, 0);
                }
            }
        }

        // Top, bottom, left and right strips of the given width; they tile the ring without overlap
        public static Rect[] Strips(Rect grid, float ring)
        {
            if (!(ring > 0f) || float.IsInfinity(ring))
            {
                throw new ArgumentOutOfRangeException(nameof(ring));
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
            return Mathf.Clamp(blades, 0, HLGrassLayout.MaxBudget);
        }

        public static HLRingStrip[] Bands(Rect grid)
        {
            return Bands(grid, DefaultWidths, DefaultFractions, BoardDensity);
        }

        // Concentric bands of whole-cell widths, each as four strips at boardDensity * fraction.
        // A strip over the grass budget is cut along its length into whole-cell pieces so no density is lost.
        public static HLRingStrip[] Bands(Rect grid, float[] widths, float[] densityFractions, float boardDensity)
        {
            if (widths == null || densityFractions == null || widths.Length == 0
                || widths.Length != densityFractions.Length)
            {
                throw new ArgumentException("Widths and density fractions must be non-empty and of equal length.");
            }
            if (!(boardDensity > 0f) || float.IsInfinity(boardDensity))
            {
                throw new ArgumentOutOfRangeException(nameof(boardDensity));
            }
            if (!(grid.width > 0f) || !(grid.height > 0f) || !IsWhole(grid.width) || !IsWhole(grid.height))
            {
                throw new ArgumentOutOfRangeException(nameof(grid));
            }

            List<HLRingStrip> result = new List<HLRingStrip>();
            Rect inner = grid;
            for (int band = 0; band < widths.Length; band++)
            {
                float width = widths[band];
                float fraction = densityFractions[band];
                if (!IsWhole(width) || width < 1f)
                {
                    throw new ArgumentOutOfRangeException(nameof(widths));
                }
                if (!(fraction > 0f) || float.IsInfinity(fraction))
                {
                    throw new ArgumentOutOfRangeException(nameof(densityFractions));
                }

                foreach (Rect strip in Strips(inner, width))
                {
                    Split(strip, boardDensity * fraction, band, result);
                }
                inner = new Rect(inner.xMin - width, inner.yMin - width,
                    inner.width + 2f * width, inner.height + 2f * width);
            }
            return result.ToArray();
        }

        public static void EnsureCells(GridManager proxy)
        {
            if (!proxy || proxy.width <= 0 || proxy.height <= 0)
            {
                return;
            }
            if (proxy.cells != null && proxy.cells.Length == proxy.width * proxy.height)
            {
                return;
            }

            GridCell[] cells = new GridCell[proxy.width * proxy.height];
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = new GridCell { coord = new Vector2Int(i % proxy.width, i / proxy.width) };
            }
            proxy.cells = cells;
        }

        static bool IsWhole(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && Mathf.Abs(value - Mathf.Round(value)) < 0.0001f;
        }

        static void Split(Rect strip, float density, int band, List<HLRingStrip> into)
        {
            bool isWide = strip.width >= strip.height;
            int length = Mathf.RoundToInt(isWide ? strip.width : strip.height);
            int across = Mathf.RoundToInt(isWide ? strip.height : strip.width);
            int pieces = Mathf.Clamp(Mathf.CeilToInt(length * across * density / HLGrassLayout.MaxBudget), 1, length);
            while (pieces < length && (length + pieces - 1) / pieces * across * density > HLGrassLayout.MaxBudget)
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
                into.Add(new HLRingStrip
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
