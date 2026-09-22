using System;
using System.Collections.Generic;
using HealerLike.Render.Stones;
using UnityEngine;

namespace HealerLike.Render.Environment
{
    public enum HLEnvironmentKind { Boulder, Cairn, Monolith, MushroomTree, SpiralFern, BladeRosette, SphereCluster }

    public struct HLEnvironmentItem
    {
        public HLEnvironmentKind Kind;
        public Vector3 Position;
        public float Yaw, Scale, Distance01;
        public uint Seed;
        public int PaletteIndex;
    }

    [Serializable]
    public struct HLEnvironmentCounts
    {
        public int boulders, cairns, monoliths, mushroomTrees, spiralFerns, bladeRosettes, sphereClusters;
        public static HLEnvironmentCounts Default => new HLEnvironmentCounts
            { boulders = 120, cairns = 40, monoliths = 3, mushroomTrees = 60, spiralFerns = 80, bladeRosettes = 120, sphereClusters = 70 };
        public int this[HLEnvironmentKind kind]
        {
            get
            {
                switch (kind)
                {
                    case HLEnvironmentKind.Boulder: return boulders;
                    case HLEnvironmentKind.Cairn: return cairns;
                    case HLEnvironmentKind.Monolith: return monoliths;
                    case HLEnvironmentKind.MushroomTree: return mushroomTrees;
                    case HLEnvironmentKind.SpiralFern: return spiralFerns;
                    case HLEnvironmentKind.BladeRosette: return bladeRosettes;
                    default: return sphereClusters;
                }
            }
        }
    }

    [Serializable]
    public struct HLEnvironmentSettings
    {
        public int seed;
        [Tooltip("Empty cells kept around the grid; nothing is placed closer.")] public float marginCells;
        [Tooltip("How far beyond the margin the ring reaches, in world units.")] public float ringDistance;
        [Tooltip("Density falls as exp(-falloff * t), t = 0 at the margin and 1 at the ring's outer edge.")] public float falloff;
        public HLEnvironmentCounts counts;
        public static HLEnvironmentSettings Default => new HLEnvironmentSettings
            { seed = 1707, marginCells = 1, ringDistance = 32, falloff = 2.2f, counts = HLEnvironmentCounts.Default };
    }

    /// <summary>Deterministic placement of the ring around the grid. Pure data, no GameObjects.</summary>
    public static class HLEnvironmentLayout
    {
        public const uint Salt = 0x454E5649u;
        public const int MaxPerKind = 512;
        public const int AttemptsPerItem = 48;

        public static bool InsideMargin(Rect grid, float margin, Vector2 p)
            => p.x > grid.xMin - margin && p.x < grid.xMax + margin && p.y > grid.yMin - margin && p.y < grid.yMax + margin;

        public static float EdgeDistance(Rect grid, Vector2 p)
        {
            float dx = Mathf.Max(grid.xMin - p.x, 0, p.x - grid.xMax), dz = Mathf.Max(grid.yMin - p.y, 0, p.y - grid.yMax);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        // Tall kinds stay off the camera side (below the grid in z) so they never stand in front of the board.
        static bool Tall(HLEnvironmentKind kind) => kind == HLEnvironmentKind.Monolith || kind == HLEnvironmentKind.MushroomTree;

        public static List<HLEnvironmentItem> Generate(in HLEnvironmentSettings settings, Rect grid, float cellSize, float surfaceY)
        {
            if (!(cellSize > 0) || float.IsInfinity(cellSize) || grid.width <= 0 || grid.height <= 0 || !(settings.ringDistance > 0) ||
                float.IsInfinity(settings.ringDistance) || settings.marginCells < 0 || settings.falloff < 0 || float.IsNaN(surfaceY))
                throw new ArgumentOutOfRangeException(nameof(settings));
            var result = new List<HLEnvironmentItem>();
            uint baseSeed = HLStoneSeed.ForPart((uint)settings.seed, Salt);
            var random = new HLStoneRandom(baseSeed);
            float margin = settings.marginCells * cellSize, span = settings.ringDistance;
            float xMin = grid.xMin - margin - span, xMax = grid.xMax + margin + span;
            float zMin = grid.yMin - margin - span, zMax = grid.yMax + margin + span;
            foreach (HLEnvironmentKind kind in Enum.GetValues(typeof(HLEnvironmentKind)))
            {
                int count = Mathf.Clamp(settings.counts[kind], 0, MaxPerKind);
                for (int n = 0; n < count; n++)
                {
                    for (int attempt = 0; attempt < AttemptsPerItem; attempt++)
                    {
                        var p = new Vector2(random.Range(xMin, xMax), random.Range(zMin, zMax));
                        float accept = random.Next01(), jitter = random.Next01(), yaw = random.Next01();
                        if (InsideMargin(grid, margin, p)) continue;
                        float t = (EdgeDistance(grid, p) - margin) / span;
                        if (t < 0 || t > 1) continue;
                        if (Tall(kind) && p.y < grid.yMin) continue;
                        if (kind == HLEnvironmentKind.Monolith) { if (p.y <= grid.yMax + margin || t < .3f) continue; }
                        else if (accept > Mathf.Exp(-settings.falloff * t)) continue;
                        float scale = kind == HLEnvironmentKind.Monolith ? Mathf.Lerp(1.6f, 3.2f, t) :
                            kind == HLEnvironmentKind.Boulder || kind == HLEnvironmentKind.Cairn ? Mathf.Lerp(.7f, 2.6f, t) * (.8f + .4f * jitter) :
                            Mathf.Lerp(.8f, 1.6f, t) * (.8f + .4f * jitter);
                        result.Add(new HLEnvironmentItem {
                            Kind = kind, Position = new Vector3(p.x, surfaceY, p.y), Yaw = yaw * 360, Scale = scale, Distance01 = t,
                            Seed = HLStoneSeed.ForPart(baseSeed, (uint)((int)kind * 65536 + n)), PaletteIndex = (int)(jitter * 4) & 3 });
                        break;
                    }
                }
            }
            return result;
        }
    }
}
