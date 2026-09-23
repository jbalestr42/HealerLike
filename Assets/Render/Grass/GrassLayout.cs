using UnityEngine;

namespace HealerLike.Render.Grass
{
    // Stable world-space strata. Never consumes gameplay or Unity random state.
    public static class GrassLayout
    {
        // 256 tufts per square unit on the 16 by 16 board, about seven across a healer body
        public static readonly int DefaultBudget = 65536;
        public static readonly int MaxBudget = 98304;
        public static readonly float RootLift = 0.005f;
        // Mean tuft size in world units, 0.45 and 0.15 of the healer's 0.55 body sphere
        public static readonly float TuftHeight = 0.25f;
        public static readonly float TuftWidth = 0.083f;
        // Cells between the lattice points of the soft patch lane
        public static readonly int PatchCells = 5;

        public static bool IsValid(int width, int height, float size, Vector3 origin, float surfaceY)
        {
            bool isGridValid = width > 0 && height > 0 && (long)width * height <= int.MaxValue && float.IsFinite(size) && size > 0f;
            bool isOriginValid = float.IsFinite(origin.x) && float.IsFinite(origin.y) && float.IsFinite(origin.z) && float.IsFinite(surfaceY);
            bool isExtentValid = float.IsFinite(width * size) && float.IsFinite(height * size);
            bool isMaxValid = float.IsFinite(origin.x + width * size) && float.IsFinite(origin.z + height * size);
            bool isMinValid = float.IsFinite(origin.x - width * size) && float.IsFinite(origin.z - height * size);
            return isGridValid && isOriginValid && isExtentValid && isMaxValid && isMinValid;
        }

        public static BladeSeed[] Generate(int width, int height, float cellSize, Vector3 gridOrigin, float surfaceY)
        {
            return Generate(width, height, cellSize, gridOrigin, surfaceY, DefaultBudget, 1);
        }

        public static BladeSeed[] Generate(int width, int height, float cellSize, Vector3 gridOrigin, float surfaceY, int totalBladeBudget)
        {
            return Generate(width, height, cellSize, gridOrigin, surfaceY, totalBladeBudget, 1);
        }

        // Returns no seeds and logs when the footprint is not finite or the budget is negative
        public static BladeSeed[] Generate(int width, int height, float cellSize, Vector3 gridOrigin, float surfaceY, int totalBladeBudget, uint seed)
        {
            if (!IsValid(width, height, cellSize, gridOrigin, surfaceY) || totalBladeBudget < 0)
            {
                Debug.LogError($"[GrassLayout] Rejected a {width} x {height} grid of size {cellSize} with budget {totalBladeBudget}.");
                return new BladeSeed[0];
            }

            int count = System.Math.Min(totalBladeBudget, MaxBudget);
            BladeSeed[] result = new BladeSeed[count];
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
        public static BladeSeed[] Generate(Rect rect, float surfaceY, float density)
        {
            return Generate(rect, surfaceY, density, 1, DefaultBudget);
        }

        public static BladeSeed[] Generate(Rect rect, float surfaceY, float density, uint seed, int budget)
        {
            bool isRectValid = rect.width > 0f && rect.height > 0f && IsValid(1, 1, 1f, new Vector3(rect.xMin, 0f, rect.yMin), surfaceY);
            bool isFarCornerValid = float.IsFinite(rect.xMax) && float.IsFinite(rect.yMax);
            if (!float.IsFinite(density) || density < 0f || budget < 0 || !isRectValid || !isFarCornerValid)
            {
                Debug.LogError($"[GrassLayout] Rejected rect {rect} with density {density} and budget {budget}.");
                return new BladeSeed[0];
            }

            double desired = System.Math.Floor((double)rect.width * rect.height * density);
            int count = (int)System.Math.Min(System.Math.Min(desired, budget), MaxBudget);
            BladeSeed[] result = Generate(1, 1, 1f, Vector3.zero, surfaceY, count, seed);
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

        // Smooth value noise over the grid, one lattice point every PatchCells cells, in 0..1
        static float PatchLane(uint seed, float x, float z)
        {
            float latticeX = x / PatchCells;
            float latticeZ = z / PatchCells;
            int column = Mathf.FloorToInt(latticeX);
            int row = Mathf.FloorToInt(latticeZ);
            float blendX = Mathf.SmoothStep(0f, 1f, latticeX - column);
            float blendZ = Mathf.SmoothStep(0f, 1f, latticeZ - row);
            float bottom = Mathf.Lerp(Sample(seed, column, row, 7), Sample(seed, column + 1, row, 7), blendX);
            float top = Mathf.Lerp(Sample(seed, column, row + 1, 7), Sample(seed, column + 1, row + 1, 7), blendX);
            return Mathf.Lerp(bottom, top, blendZ);
        }

        // Tufts spread evenly over the cell in jittered rows, each with its own rest heading
        static int FillCell(BladeSeed[] result, int index, int cell, int quota, int width, float cellSize, Vector2 minimum, float surfaceY, uint seed)
        {
            int rows = Mathf.CeilToInt(Mathf.Sqrt(quota));
            // Rows that get one tuft more than their neighbours move from cell to cell
            int rowShift = (int)(Sample(seed, cell, 0, 9) * rows);
            for (int tuft = 0; tuft < quota; tuft++)
            {
                // Every row holds an even share of the tufts and jitters across its whole stratum,
                // so no cell leaves a sparse last row or empty gaps between rows
                int row = tuft * rows / quota;
                int rowStart = (row * quota + rows - 1) / rows;
                int rowEnd = ((row + 1) * quota + rows - 1) / rows;
                float x = cell % width + (tuft - rowStart + Sample(seed, cell, tuft, 1)) / (rowEnd - rowStart);
                float z = cell / width + ((row + rowShift) % rows + Sample(seed, cell, tuft, 2)) / rows;
                float yaw = Sample(seed, cell, tuft, 3) * Mathf.PI * 2f;
                float phase = Sample(seed, cell, tuft, 5) * Mathf.PI * 2f;
                float height = TuftHeight * (0.92f + 0.16f * Sample(seed, cell, tuft, 4));
                float tuftWidth = TuftWidth * (0.9f + 0.2f * Sample(seed, cell, tuft, 6));
                // A little per-tuft grain over the soft lane, so its blend between greens never shows a line
                float patch = Mathf.Clamp01(PatchLane(seed, x, z) + 0.15f * (Sample(seed, cell, tuft, 7) - 0.5f));
                Vector3 root = new Vector3(minimum.x + x * cellSize, surfaceY + RootLift, minimum.y + z * cellSize);
                result[index].positionYaw = new Vector4(root.x, root.y, root.z, yaw);
                // Phase is also the rest-lean heading; w is the soft patch lane
                result[index].heightPhaseWidthRandom = new Vector4(height, phase, tuftWidth, patch);
                index++;
            }

            return index;
        }
    }
}
