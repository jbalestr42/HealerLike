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
                int columns = quota == 128 || quota == 256 ? 16 : quota == 384 ? 24 : Mathf.CeilToInt(Mathf.Sqrt(quota));
                int rows = (quota + columns - 1) / columns;
                // Rotate then reverse the complete slot sequence: a bijection even for non-square quotas.
                int slots = columns * rows, offset = (int)(Hash(seed ^ (uint)cell) % (uint)slots);
                for (int blade = 0; blade < quota; blade++)
                {
                    int slot = (offset + slots - blade) % slots;
                    float x = (slot % columns + 0.1f + 0.8f * Sample(seed, cell, blade, 1)) / columns;
                    float z = (slot / columns + 0.1f + 0.8f * Sample(seed, cell, blade, 2)) / rows;
                    result[index++] = new HLBladeSeed {
                        positionYaw = new Vector4(minX + (cell % width + x) * cellSize, surfaceY + RootLift,
                            minZ + (cell / width + z) * cellSize, Sample(seed, cell, blade, 3) * Mathf.PI * 2),
                        heightPhaseWidthRandom = new Vector4(0.22f + 0.14f * Sample(seed, cell, blade, 4),
                            Sample(seed, cell, blade, 5) * Mathf.PI * 2, 0.035f + 0.015f * Sample(seed, cell, blade, 6),
                            Sample(seed, cell, blade, 7))
                    };
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
