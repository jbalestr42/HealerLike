using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HealerLike.Render.Grass
{
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = Stride)]
    public struct HLBladeSeed
    {
        public const int Stride = 32;
        public Vector4 positionYaw;
        public Vector4 heightPhaseWidthRandom;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = Stride)]
    public struct HLBladeState
    {
        public const int Stride = 32;
        public Vector4 leanHeightSpike;
        public Vector4 rampHealReserved;
    }

    /// <summary>Stable world-space strata. Never consumes gameplay or Unity random state.</summary>
    public static class HLGrassLayout
    {
        public const int DefaultBudget = 65536;
        public const int MaxBudget = 98304;
        public const float RootLift = 0.005f;

        internal static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        internal static void Validate(int width, int height, float size, Vector3 origin, float surfaceY)
        {
            if (width <= 0 || height <= 0 || (long)width * height > int.MaxValue ||
                !Finite(size) || size <= 0 || !Finite(origin.x) || !Finite(origin.y) || !Finite(origin.z) ||
                !Finite(surfaceY) || !Finite(width * size) || !Finite(height * size) ||
                !Finite(origin.x + width * size) || !Finite(origin.z + height * size) ||
                !Finite(origin.x - width * size) || !Finite(origin.z - height * size))
                throw new ArgumentOutOfRangeException(nameof(size), "Grass requires a finite axis-aligned footprint.");
        }

        // Fixed avalanche hash; channels decouple position, yaw, height, phase and width.
        static uint Hash(uint x)
        {
            unchecked { x ^= x >> 16; x *= 0x7feb352du; x ^= x >> 15; x *= 0x846ca68bu; return x ^ (x >> 16); }
        }
        static float Sample(uint seed, int cell, int blade, uint channel)
        {
            unchecked { return (Hash(seed ^ ((uint)cell * 0x9e3779b9u) ^ ((uint)blade * 0x85ebca6bu) ^ channel * 0xc2b2ae35u) >> 8) * (1f / 16777216f); }
        }

        public static HLBladeSeed[] Generate(int width, int height, float cellSize, Vector3 gridOrigin,
            float surfaceY, int totalBladeBudget = DefaultBudget, uint seed = 1)
        {
            Validate(width, height, cellSize, gridOrigin, surfaceY);
            if (totalBladeBudget < 0) throw new ArgumentOutOfRangeException(nameof(totalBladeBudget));
            int count = System.Math.Min(totalBladeBudget, MaxBudget);
            var result = new HLBladeSeed[count];
            int cells = width * height, quotient = count / cells, remainder = count % cells, index = 0;
            float minX = gridOrigin.x - width * cellSize * 0.5f, minZ = gridOrigin.z - height * cellSize * 0.5f;
            for (int cell = 0; cell < cells && index < count; cell++)
            {
                int quota = quotient + (cell < remainder ? 1 : 0);
                if (quota == 0) continue;
                // Sparse budgets retain their exact per-cell quota; ordinary cells partition into 3..7.
                int clumps = Mathf.Max(1, (quota + 4) / 5);
                int columns = Mathf.CeilToInt(Mathf.Sqrt(clumps));
                int rows = (clumps + columns - 1) / columns;
                int remaining = quota;
                int patchSize = 2 + (int)(Hash(seed) % 3);
                int patch = cell % width / patchSize + (cell / width / patchSize) * ((width + patchSize - 1) / patchSize);
                float hue = Sample(seed, patch, 0, 7);
                for (int clump = 0; clump < clumps; clump++)
                {
                    int left = clumps - clump - 1;
                    int blades = left == 0 ? remaining : Mathf.Clamp(3 + (int)(Sample(seed, cell, clump, 8) * 5),
                        Mathf.Max(3, remaining - 7 * left), Mathf.Min(7, remaining - 3 * left));
                    remaining -= blades;
                    float x = (clump % columns + 0.1f + 0.8f * Sample(seed, cell, clump, 1)) / columns;
                    float z = (clump / columns + 0.1f + 0.8f * Sample(seed, cell, clump, 2)) / rows;
                    float yaw = Sample(seed, cell, clump, 3) * Mathf.PI * 2;
                    float phase = Sample(seed, cell, clump, 5) * Mathf.PI * 2;
                    float heightScale = 0.6f + 0.8f * Sample(seed, cell, clump, 4);
                    for (int blade = 0; blade < blades; blade++)
                    {
                        float fan = blades == 1 ? 0 : ((float)blade / (blades - 1) - 0.5f) * 1.8f;
                        result[index++] = new HLBladeSeed {
                            positionYaw = new Vector4(minX + (cell % width + x) * cellSize, surfaceY + RootLift,
                                minZ + (cell / width + z) * cellSize, Mathf.Repeat(yaw + fan, Mathf.PI * 2)),
                            // Phase is also the shared rest-lean heading. W is the seeded cell-patch hue.
                            heightPhaseWidthRandom = new Vector4(quota < 3 ? 0.22f + 0.14f * Sample(seed, cell, clump, 4)
                                : 0.30f * heightScale * (1 - 0.055f * Mathf.Abs(fan)),
                                phase, 0.095f + 0.055f * Sample(seed, cell, blade, 6), hue)
                        };
                    }
                }
            }
            return result;
        }

        /// <summary>Density is blades per square world unit; the absolute budget still caps large grids.</summary>
        public static HLBladeSeed[] Generate(Rect rect, float surfaceY, float density, uint seed = 1, int budget = DefaultBudget)
        {
            if (!Finite(density) || density < 0 || budget < 0 || !Finite(rect.width) || !Finite(rect.height) || rect.width <= 0 || rect.height <= 0)
                throw new ArgumentOutOfRangeException(nameof(density));
            double desired = System.Math.Floor((double)rect.width * rect.height * density);
            int count = (int)System.Math.Min(System.Math.Min(desired, budget), MaxBudget);
            var result = Generate(1, 1, 1, Vector3.zero, surfaceY, count, seed);
            Validate(1, 1, 1, new Vector3(rect.xMin, 0, rect.yMin), surfaceY);
            if (!Finite(rect.xMax) || !Finite(rect.yMax)) throw new ArgumentOutOfRangeException(nameof(rect));
            for (int i = 0; i < result.Length; i++)
            {
                var p = result[i].positionYaw;
                p.x = rect.xMin + (p.x + 0.5f) * rect.width;
                p.z = rect.yMin + (p.z + 0.5f) * rect.height;
                result[i].positionYaw = p;
            }
            return result;
        }
    }
}
