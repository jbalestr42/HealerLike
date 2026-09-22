using HealerLike.Render.Grass;
using HealerLike.Render.Zones;
using UnityEngine;

namespace HealerLike.Render.Environment
{
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
            var buffer = zoneRegistry ? zoneRegistry.Buffer : null;
            foreach (var field in fields) if (field) field.SetZoneSnapshot(buffer, 0);
        }

        void OnDisable() { foreach (var field in fields) if (field) field.SetZoneSnapshot(null, 0); }
    }
}
