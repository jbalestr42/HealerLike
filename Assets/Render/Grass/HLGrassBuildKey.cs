using System;
using UnityEngine;

namespace HealerLike.Render.Grass
{
    // Everything a built field depends on; any change rebuilds the blades
    public struct HLGrassBuildKey
    {
        public int width;
        public int height;
        public float cellSize;
        public Vector3 origin;
        public float surfaceY;
        public uint seed;
        public int budget;

        public HLGrassBuildKey(GridManager grid, float surfaceY, uint seed, int budget)
        {
            width = grid.width;
            height = grid.height;
            cellSize = grid.size;
            origin = grid.transform.position;
            this.surfaceY = surfaceY;
            this.seed = seed;
            this.budget = budget;
        }

        public bool Matches(HLGrassBuildKey other)
        {
            bool isSameGrid = width == other.width && height == other.height && cellSize == other.cellSize && origin == other.origin;
            return isSameGrid && surfaceY == other.surfaceY && seed == other.seed && budget == other.budget;
        }

        // Returns null when the footprint is not finite
        public HLBladeSeed[] GenerateLayout()
        {
            // HLGrassLayout still validates by exception
            try
            {
                return HLGrassLayout.Generate(width, height, cellSize, origin, surfaceY, budget, seed);
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        public Bounds CalculateBounds()
        {
            return HLGrassBounds.Calculate(width, height, cellSize, origin, surfaceY);
        }

        // xy is the minimum corner and zw the maximum corner of the field, in world XZ
        public Vector4 FieldRect()
        {
            float halfWidth = width * cellSize / 2f;
            float halfHeight = height * cellSize / 2f;
            return new Vector4(origin.x - halfWidth, origin.z - halfHeight, origin.x + halfWidth, origin.z + halfHeight);
        }
    }
}
