using UnityEngine;

namespace HealerLike.Render.Grass
{
    // Tufts on a jittered regular grid. Never consumes gameplay or Unity random state.
    public static class GrassLayout
    {
        public static readonly int MaxBudget = 98304;
        public static readonly float RootLift = 0.005f;
        // Three times the former tuft in every dimension; the same ratio widens root spacing.
        public static readonly float TuftHeight = 0.7425f;
        // A 3 to 1 spike, 0.29 wide for 0.884 tall
        public static readonly float TuftWidth = TuftHeight * 0.29f / 0.884f;
        // Grid step between roots in cells, 0.2 for the same 0.884 tall tuft
        public static readonly float Spacing = TuftHeight * 0.2f / 0.884f;
        // Tufts per square cell at the wider grid step, about 35.4
        public static readonly float Density = 1f / (Spacing * Spacing);
        // Each root moves by up to this fraction of the grid step on each axis
        public static readonly float Jitter = 0.5f;
        public static readonly float MinScale = 0.8f;
        public static readonly float MaxScale = 1.2f;
        // Rest tilt in radians about the root, every tuft leaning the same way by its own amount
        public static readonly float MaxLean = 0.5f;
        public static readonly Vector2 LeanHeading = Vector2.up;
        // A full heal lifts a tuft to this multiple of its height, HL_HEAL_LIFT in Grass.compute plus one
        public static readonly float HealLift = 1.8f;
        // A hostile spike stands at most this tall in world units whatever the tuft, HL_SPIKE_MIN_HEIGHT plus
        // HL_SPIKE_HEIGHT_RANGE in Grass.compute, and at most this wide either side of its root,
        // HL_SPIKE_HALF_WIDTH in GrassInstancing.hlsl
        public static readonly float SpikeHeight = 0.63f;
        public static readonly float SpikeHalfWidth = 0.065f;

        public static bool IsValid(int width, int height, float size, Vector3 origin, float surfaceY)
        {
            bool isGridValid = width > 0 && height > 0 && (long)width * height <= int.MaxValue
                               && RenderMath.IsPositive(size);
            bool isOriginValid = RenderMath.IsFinite(origin) && float.IsFinite(surfaceY);
            bool isExtentValid = float.IsFinite(width * size) && float.IsFinite(height * size);
            bool isMaxValid = float.IsFinite(origin.x + width * size) && float.IsFinite(origin.z + height * size);
            bool isMinValid = float.IsFinite(origin.x - width * size) && float.IsFinite(origin.z - height * size);
            return isGridValid && isOriginValid && isExtentValid && isMaxValid && isMinValid;
        }

        // Tufts at the grid step, or on a wider grid when the budget is smaller than the area holds
        public static int CountFor(int width, int height, int budget)
        {
            double full = System.Math.Floor(width / Spacing) * System.Math.Floor(height / Spacing);
            return (int)System.Math.Min(System.Math.Min(full, budget), MaxBudget);
        }

        // Returns no seeds and logs when the footprint is not finite or the budget is negative
        public static TuftSeed[] Generate(int width, int height, float cellSize, Vector3 gridOrigin, float surfaceY,
                                           int budget, uint seed)
        {
            if (!IsValid(width, height, cellSize, gridOrigin, surfaceY) || budget < 0)
            {
                Debug.LogError($"[GrassLayout] Rejected a {width} x {height} grid of size {cellSize} "
                               + $"with budget {budget}.");
                return new TuftSeed[0];
            }

            int count = CountFor(width, height, budget);
            if (count == 0)
            {
                return new TuftSeed[0];
            }

            // The widest step that still fits the count, the same on both axes
            float step = Mathf.Max(Spacing, Mathf.Sqrt((float)width * height / count));
            int columns = Mathf.Max(1, Mathf.FloorToInt(width / step + 0.001f));
            int rows = Mathf.Max(1, Mathf.FloorToInt(height / step + 0.001f));
            while ((long)columns * rows > count)
            {
                if (columns >= rows)
                {
                    columns--;
                }
                else
                {
                    rows--;
                }
            }

            float stepX = (float)width / columns;
            float stepZ = (float)height / rows;
            Vector2 minimum = new Vector2(gridOrigin.x - width * cellSize * 0.5f,
                                          gridOrigin.z - height * cellSize * 0.5f);
            TuftSeed[] result = new TuftSeed[columns * rows];
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    int index = column + row * columns;
                    float x = (column + 0.5f + Jitter * Signed(seed, index, 1)) * stepX;
                    float z = (row + 0.5f + Jitter * Signed(seed, index, 2)) * stepZ;
                    float yaw = Sample(seed, index, 3) * Mathf.PI * 2f;
                    float scale = Mathf.Lerp(MinScale, MaxScale, Sample(seed, index, 4));
                    Vector2 lean = LeanHeading * (MaxLean * Sample(seed, index, 5));
                    Vector3 root = new Vector3(minimum.x + x * cellSize, surfaceY + RootLift, minimum.y + z * cellSize);
                    result[index].positionYaw = new Vector4(root.x, root.y, root.z, yaw);
                    float tuftHeight = TuftHeight * cellSize * scale;
                    float tuftWidth = TuftWidth * cellSize * scale;
                    result[index].heightWidthLean = new Vector4(tuftHeight, tuftWidth, lean.x, lean.y);
                }
            }

            return result;
        }

        // Fixed avalanche hash; channels decouple position, yaw, scale and lean
        static uint Hash(uint x)
        {
            x ^= x >> 16;
            x *= 0x7feb352du;
            x ^= x >> 15;
            x *= 0x846ca68bu;
            return x ^ (x >> 16);
        }

        // Uniform in 0..1
        static float Sample(uint seed, int tuft, uint channel)
        {
            uint key = seed ^ ((uint)tuft * 0x85ebca6bu) ^ (channel * 0xc2b2ae35u);
            return (Hash(key) >> 8) * (1f / 16777216f);
        }

        // Uniform in -1..1
        static float Signed(uint seed, int tuft, uint channel)
        {
            return Sample(seed, tuft, channel) * 2f - 1f;
        }
    }
}
