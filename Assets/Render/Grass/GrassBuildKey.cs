using UnityEngine;

namespace HealerLike.Render.Grass
{
    // Everything a built field depends on; any change rebuilds the blades
    public struct GrassBuildKey
    {
        public int width;
        public int height;
        public float cellSize;
        public Vector3 origin;
        public float surfaceY;
        public uint seed;
        public int budget;

        // The area is in world XZ and holds whole cells
        public GrassBuildKey(Rect area, float cellSize, float surfaceY, uint seed, int budget)
        {
            width = Mathf.RoundToInt(area.width / cellSize);
            height = Mathf.RoundToInt(area.height / cellSize);
            this.cellSize = cellSize;
            origin = new Vector3(area.center.x, 0f, area.center.y);
            this.surfaceY = surfaceY;
            this.seed = seed;
            this.budget = budget;
        }

        public bool Matches(GrassBuildKey other)
        {
            bool isSameGrid = width == other.width && height == other.height && cellSize == other.cellSize
                              && origin == other.origin;
            return isSameGrid && surfaceY == other.surfaceY && seed == other.seed && budget == other.budget;
        }

        // Returns null when the footprint is not finite
        public BladeSeed[] GenerateLayout()
        {
            if (!GrassLayout.IsValid(width, height, cellSize, origin, surfaceY) || budget < 0)
            {
                return null;
            }

            return GrassLayout.Generate(width, height, cellSize, origin, surfaceY, budget, seed);
        }

        public Bounds CalculateBounds()
        {
            return GrassBounds.Calculate(width, height, cellSize, origin, surfaceY);
        }

        // xy is the minimum corner and zw the maximum corner of the field, in world XZ
        public Vector4 FieldRect()
        {
            float halfWidth = width * cellSize / 2f;
            float halfHeight = height * cellSize / 2f;
            return new Vector4(origin.x - halfWidth, origin.z - halfHeight,
                               origin.x + halfWidth, origin.z + halfHeight);
        }
    }
}
