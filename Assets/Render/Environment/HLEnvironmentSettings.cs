using System;

namespace HealerLike.Render.Environment
{
    [Serializable]
    public struct HLEnvironmentSettings
    {
        public static readonly HLEnvironmentSettings Default = new HLEnvironmentSettings
        {
            seed = 1707,
            marginCells = 1f,
            ringDistance = 17f,
            falloff = 3.2f,
            counts = HLEnvironmentCounts.Default
        };

        public int seed;
        // Empty cells kept around the grid, nothing is placed closer
        public float marginCells;
        // How far past the margin the ring reaches, in world units
        public float ringDistance;
        // Density falls as exp(-falloff * t), t = 0 at the margin and 1 at the outer edge of the ring
        public float falloff;
        public HLEnvironmentCounts counts;
    }
}
