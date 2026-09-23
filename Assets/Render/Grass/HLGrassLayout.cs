using UnityEngine;

namespace HealerLike.Render.Grass
{
    // Stable world-space strata. Never consumes gameplay or Unity random state.
    public static class HLGrassLayout
    {
        public static readonly int DefaultBudget = 65536;
        public static readonly int MaxBudget = 98304;
        public static readonly float RootLift = 0.005f;

        public static bool IsValid(int width, int height, float size, Vector3 origin, float surfaceY)
        {
            bool isGridValid = width > 0 && height > 0 && (long)width * height <= int.MaxValue && float.IsFinite(size) && size > 0f;
            bool isOriginValid = float.IsFinite(origin.x) && float.IsFinite(origin.y) && float.IsFinite(origin.z) && float.IsFinite(surfaceY);
            bool isExtentValid = float.IsFinite(width * size) && float.IsFinite(height * size);
            bool isMaxValid = float.IsFinite(origin.x + width * size) && float.IsFinite(origin.z + height * size);
            bool isMinValid = float.IsFinite(origin.x - width * size) && float.IsFinite(origin.z - height * size);
            return isGridValid && isOriginValid && isExtentValid && isMaxValid && isMinValid;
        }

        public static HLBladeSeed[] Generate(int width, int height, float cellSize, Vector3 gridOrigin, float surfaceY)
        {
            return Generate(width, height, cellSize, gridOrigin, surfaceY, DefaultBudget, 1);
        }

        public static HLBladeSeed[] Generate(int width, int height, float cellSize, Vector3 gridOrigin, float surfaceY, int totalBladeBudget)
        {
            return Generate(width, height, cellSize, gridOrigin, surfaceY, totalBladeBudget, 1);
        }

        // Returns no seeds and logs when the footprint is not finite or the budget is negative
        public static HLBladeSeed[] Generate(int width, int height, float cellSize, Vector3 gridOrigin, float surfaceY, int totalBladeBudget, uint seed)
        {
            if (!IsValid(width, height, cellSize, gridOrigin, surfaceY) || totalBladeBudget < 0)
            {
                Debug.LogError($"[HLGrassLayout] Rejected a {width} x {height} grid of size {cellSize} with budget {totalBladeBudget}.");
                return new HLBladeSeed[0];
            }

            int count = System.Math.Min(totalBladeBudget, MaxBudget);
            HLBladeSeed[] result = new HLBladeSeed[count];
            int cells = width * height;
            int quotient = count / cells;
            int remainder = count % cells;
            int index = 0;
            Vector2 minimum = new Vector2(gridOrigin.x - width * cellSize * 0.5f, gridOrigin.z - height * cellSize * 0.5f);
            for (int cell = 0; cell < cells && index < count; cell++)
            {
                int quota = quotient + (cell < remainder ? 1 : 0);
                if (quota > 0)
                {
                    index = FillCell(result, index, cell, quota, width, cellSize, minimum, surfaceY, seed);
                }
            }

            return result;
        }

        // Density is blades per square world unit; the absolute budget still caps large grids
        public static HLBladeSeed[] Generate(Rect rect, float surfaceY, float density)
        {
            return Generate(rect, surfaceY, density, 1, DefaultBudget);
        }

        public static HLBladeSeed[] Generate(Rect rect, float surfaceY, float density, uint seed, int budget)
        {
            bool isRectValid = rect.width > 0f && rect.height > 0f && IsValid(1, 1, 1f, new Vector3(rect.xMin, 0f, rect.yMin), surfaceY);
            bool isFarCornerValid = float.IsFinite(rect.xMax) && float.IsFinite(rect.yMax);
            if (!float.IsFinite(density) || density < 0f || budget < 0 || !isRectValid || !isFarCornerValid)
            {
                Debug.LogError($"[HLGrassLayout] Rejected rect {rect} with density {density} and budget {budget}.");
                return new HLBladeSeed[0];
            }

            double desired = System.Math.Floor((double)rect.width * rect.height * density);
            int count = (int)System.Math.Min(System.Math.Min(desired, budget), MaxBudget);
            HLBladeSeed[] result = Generate(1, 1, 1f, Vector3.zero, surfaceY, count, seed);
            for (int i = 0; i < result.Length; i++)
            {
                Vector4 position = result[i].positionYaw;
                position.x = rect.xMin + (position.x + 0.5f) * rect.width;
                position.z = rect.yMin + (position.z + 0.5f) * rect.height;
                result[i].positionYaw = position;
            }

            return result;
        }

        // Fixed avalanche hash; channels decouple position, yaw, height, phase and width
        static uint Hash(uint x)
        {
            x ^= x >> 16;
            x *= 0x7feb352du;
            x ^= x >> 15;
            x *= 0x846ca68bu;
            return x ^ (x >> 16);
        }

        static float Sample(uint seed, int cell, int blade, uint channel)
        {
            uint key = seed ^ ((uint)cell * 0x9e3779b9u) ^ ((uint)blade * 0x85ebca6bu) ^ (channel * 0xc2b2ae35u);
            return (Hash(key) >> 8) * (1f / 16777216f);
        }

        static int FillCell(HLBladeSeed[] result, int index, int cell, int quota, int width, float cellSize, Vector2 minimum, float surfaceY, uint seed)
        {
            // Sparse budgets keep their exact per-cell quota; ordinary cells split into clumps of 3 to 7
            int clumps = Mathf.Max(1, (quota + 4) / 5);
            int rows = Mathf.CeilToInt(Mathf.Sqrt(clumps));
            // Rows that get one clump more than their neighbours move from cell to cell
            int rowShift = (int)(Sample(seed, cell, 0, 9) * rows);
            int remaining = quota;
            int patchSize = 2 + (int)(Hash(seed) % 3);
            int patchColumns = (width + patchSize - 1) / patchSize;
            int patch = cell % width / patchSize + (cell / width / patchSize) * patchColumns;
            float hue = Sample(seed, patch, 0, 7);
            for (int clump = 0; clump < clumps; clump++)
            {
                int left = clumps - clump - 1;
                int blades = remaining;
                if (left > 0)
                {
                    int wanted = 3 + (int)(Sample(seed, cell, clump, 8) * 5);
                    blades = Mathf.Clamp(wanted, Mathf.Max(3, remaining - 7 * left), Mathf.Min(7, remaining - 3 * left));
                }

                remaining -= blades;

                // Every row holds an even share of the clumps and jitters across its whole stratum,
                // so no cell leaves a sparse last row or empty gaps between rows
                int row = clump * rows / clumps;
                int rowStart = (row * clumps + rows - 1) / rows;
                int rowEnd = ((row + 1) * clumps + rows - 1) / rows;
                float x = (clump - rowStart + Sample(seed, cell, clump, 1)) / (rowEnd - rowStart);
                float z = ((row + rowShift) % rows + Sample(seed, cell, clump, 2)) / rows;
                float yaw = Sample(seed, cell, clump, 3) * Mathf.PI * 2f;
                float phase = Sample(seed, cell, clump, 5) * Mathf.PI * 2f;
                float heightScale = 0.6f + 0.8f * Sample(seed, cell, clump, 4);
                Vector3 root = new Vector3(minimum.x + (cell % width + x) * cellSize, surfaceY + RootLift, minimum.y + (cell / width + z) * cellSize);
                for (int blade = 0; blade < blades; blade++)
                {
                    float fan = blades == 1 ? 0f : ((float)blade / (blades - 1) - 0.5f) * 1.8f;
                    float height = 0.30f * heightScale * (1f - 0.055f * Mathf.Abs(fan));
                    if (quota < 3)
                    {
                        height = 0.22f + 0.14f * Sample(seed, cell, clump, 4);
                    }

                    float bladeWidth = 0.095f + 0.055f * Sample(seed, cell, blade, 6);
                    result[index].positionYaw = new Vector4(root.x, root.y, root.z, Mathf.Repeat(yaw + fan, Mathf.PI * 2f));
                    // Phase is also the shared rest-lean heading; w is the seeded cell-patch hue
                    result[index].heightPhaseWidthRandom = new Vector4(height, phase, bladeWidth, hue);
                    index++;
                }
            }

            return index;
        }
    }
}
