using System;
using System.Collections.Generic;
using HealerLike.Render.Grass;
using HealerLike.Render.Zones;
using UnityEngine;

namespace HealerLike.Render.Environment
{
    /// <summary>One grass strip of a graded ring: whole-cell rect, band index (0 = next to the grid), blades per square unit, blade budget.</summary>
    public struct HLRingStrip
    {
        public Rect Rect;
        public int Band;
        public float Density;
        public int Budget;
    }

    /// <summary>Carpet around the grid: four HLGrassField strips, each bound to a stage-owned proxy GridManager
    /// that is never generated. The strips borrow the zone buffer with a count of zero, so zones stay on the board.</summary>
    [DefaultExecutionOrder(0)]
    public sealed class HLEnvironmentGrass : MonoBehaviour
    {
        [SerializeField] HLZoneRegistry zoneRegistry;
        [SerializeField] GridManager[] proxies = new GridManager[0];
        [SerializeField] HLGrassField[] fields = new HLGrassField[0];

        public void Configure(HLZoneRegistry zones, GridManager[] proxyGrids, HLGrassField[] grassFields)
        {
            zoneRegistry = zones; proxies = proxyGrids ?? new GridManager[0]; fields = grassFields ?? new HLGrassField[0];
        }

        /// <summary>Top, bottom, left, right strips of the given width around the grid; they tile the ring without overlap.</summary>
        public static Rect[] Strips(Rect grid, float ring)
        {
            if (!(ring > 0) || float.IsInfinity(ring)) throw new System.ArgumentOutOfRangeException(nameof(ring));
            return new[] {
                new Rect(grid.xMin - ring, grid.yMax, grid.width + 2 * ring, ring),
                new Rect(grid.xMin - ring, grid.yMin - ring, grid.width + 2 * ring, ring),
                new Rect(grid.xMin - ring, grid.yMin, ring, grid.height),
                new Rect(grid.xMax, grid.yMin, ring, grid.height) };
        }

        /// <summary>Blades for a strip at the given density (blades per square world unit), capped by the grass budget.</summary>
        public static int Budget(Rect strip, float density)
            => Mathf.Clamp(Mathf.RoundToInt(strip.width * strip.height * Mathf.Max(0, density)), 0, HLGrassLayout.MaxBudget);

        public static readonly float[] DefaultWidths = { 3, 5, 16 };
        public static readonly float[] DefaultFractions = { .8f, .5f, .3f };
        public const float BoardDensity = 256;

        public static HLRingStrip[] Bands(Rect grid) => Bands(grid, DefaultWidths, DefaultFractions, BoardDensity);

        /// <summary>Concentric bands of the given whole-cell widths around the grid, each as four strips (see Strips) at
        /// boardDensity x fraction. A strip over the grass budget is split along its length into whole-cell pieces, so no density is lost.</summary>
        public static HLRingStrip[] Bands(Rect grid, float[] widths, float[] densityFractions, float boardDensity)
        {
            if (widths == null || densityFractions == null || widths.Length == 0 || widths.Length != densityFractions.Length)
                throw new ArgumentException("Widths and density fractions must be non-empty and of equal length.");
            if (!(boardDensity > 0) || float.IsInfinity(boardDensity)) throw new ArgumentOutOfRangeException(nameof(boardDensity));
            if (!(grid.width > 0) || !(grid.height > 0) || !Whole(grid.width) || !Whole(grid.height)) throw new ArgumentOutOfRangeException(nameof(grid));
            var result = new List<HLRingStrip>(); var inner = grid;
            for (int band = 0; band < widths.Length; band++)
            {
                float w = widths[band], f = densityFractions[band];
                if (!Whole(w) || w < 1) throw new ArgumentOutOfRangeException(nameof(widths));
                if (!(f > 0) || float.IsInfinity(f)) throw new ArgumentOutOfRangeException(nameof(densityFractions));
                foreach (var strip in Strips(inner, w)) Split(strip, boardDensity * f, band, result);
                inner = new Rect(inner.xMin - w, inner.yMin - w, inner.width + 2 * w, inner.height + 2 * w);
            }
            return result.ToArray();
        }

        static bool Whole(float v) => !float.IsNaN(v) && !float.IsInfinity(v) && Mathf.Abs(v - Mathf.Round(v)) < 1e-4f;

        static void Split(Rect strip, float density, int band, List<HLRingStrip> into)
        {
            bool wide = strip.width >= strip.height;
            int length = Mathf.RoundToInt(wide ? strip.width : strip.height), across = Mathf.RoundToInt(wide ? strip.height : strip.width);
            int pieces = Mathf.Clamp(Mathf.CeilToInt(length * across * density / HLGrassLayout.MaxBudget), 1, length);
            while (pieces < length && (length + pieces - 1) / pieces * across * density > HLGrassLayout.MaxBudget) pieces++;
            for (int i = 0, start = 0; i < pieces; i++)
            {
                int end = (i + 1) * length / pieces;
                var rect = wide ? new Rect(strip.xMin + start, strip.yMin, end - start, strip.height) : new Rect(strip.xMin, strip.yMin + start, strip.width, end - start);
                into.Add(new HLRingStrip { Rect = rect, Band = band, Density = density, Budget = Budget(rect, density) });
                start = end;
            }
        }

        public static void EnsureCells(GridManager proxy)
        {
            if (!proxy || proxy.width <= 0 || proxy.height <= 0) return;
            if (proxy.cells != null && proxy.cells.Length == proxy.width * proxy.height) return;
            var cells = new GridCell[proxy.width * proxy.height];
            for (int i = 0; i < cells.Length; i++) cells[i] = new GridCell { coord = new Vector2Int(i % proxy.width, i / proxy.width) };
            proxy.cells = cells;
        }

        void Start() { foreach (var proxy in proxies) EnsureCells(proxy); }

        void LateUpdate()
        {
            var buffer = zoneRegistry ? zoneRegistry.buffer : null;
            foreach (var field in fields) if (field) field.SetZoneSnapshot(buffer, 0);
        }

        void OnDisable() { foreach (var field in fields) if (field) field.SetZoneSnapshot(null, 0); }
    }
}
