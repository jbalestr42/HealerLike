using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Environment
{
    // Seeded placement of the ring around the grid, data only, no GameObjects.
    public static class EnvironmentLayout
    {
        public static readonly uint Salt = 0x454E5649u;
        public static readonly int MaxPerKind = 512;
        public static readonly int AttemptsPerItem = 48;

        public static bool InsideMargin(Rect grid, float margin, Vector2 point)
        {
            return point.x > grid.xMin - margin && point.x < grid.xMax + margin
                && point.y > grid.yMin - margin && point.y < grid.yMax + margin;
        }

        public static float EdgeDistance(Rect grid, Vector2 point)
        {
            float dx = Mathf.Max(grid.xMin - point.x, 0f, point.x - grid.xMax);
            float dz = Mathf.Max(grid.yMin - point.y, 0f, point.y - grid.yMax);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        // Returns no items and logs when an input is not valid
        public static List<EnvironmentItem> Generate(EnvironmentSettings settings, Rect grid, float cellSize,
            float surfaceY)
        {
            List<EnvironmentItem> result = new List<EnvironmentItem>();
            bool isGridValid = float.IsFinite(cellSize) && cellSize > 0f && grid.width > 0f && grid.height > 0f;
            bool isRingValid = float.IsFinite(settings.ringDistance) && settings.ringDistance > 0f;
            bool isShapeValid = settings.marginCells >= 0f && settings.falloff >= 0f && !float.IsNaN(surfaceY);
            if (!isGridValid || !isRingValid || !isShapeValid)
            {
                Debug.LogError($"[EnvironmentLayout] Rejected grid {grid} with cell size {cellSize} "
                               + $"and ring {settings.ringDistance}.");
                return result;
            }

            uint baseSeed = SeededRandom.ForPart((uint)settings.seed, Salt);
            SeededRandom random = new SeededRandom(baseSeed);
            float margin = settings.marginCells * cellSize;
            float span = settings.ringDistance;
            float xMin = grid.xMin - margin - span;
            float xMax = grid.xMax + margin + span;
            float zMin = grid.yMin - margin - span;
            float zMax = grid.yMax + margin + span;
            Vector2? stoneShoulder = null;
            Vector2? mushroomShoulder = null;
            foreach (EnvironmentKind kind in Enum.GetValues(typeof(EnvironmentKind)))
            {
                int count = Mathf.Clamp(settings.counts[kind], 0, MaxPerKind);
                for (int n = 0; n < count; n++)
                {
                    for (int attempt = 0; attempt < AttemptsPerItem; attempt++)
                    {
                        Vector2 point = new Vector2(random.Range(xMin, xMax), random.Range(zMin, zMax));
                        float accept = random.Next01();
                        float jitter = random.Next01();
                        float yaw = random.Next01();
                        // The first stone and mushroom frame opposite far shoulders. Random distant
                        // landmarks can sit above the focused camera, even after their crowns are fitted.
                        bool isShoulder = n == 0 && IsTall(kind);
                        if (isShoulder)
                        {
                            bool isStone = kind == EnvironmentKind.Monolith;
                            float across = isStone ? -Mathf.Lerp(0.12f, 0.14f, jitter)
                                : Mathf.Lerp(0.20f, 0.24f, jitter);
                            float depth = isStone ? Mathf.Lerp(0.6f, 1.2f, accept)
                                : Mathf.Lerp(1.1f, 1.7f, accept);
                            point = new Vector2(grid.center.x + grid.width * across,
                                grid.yMax + margin + Mathf.Min(depth * cellSize, span * 0.2f));
                        }
                        if (InsideMargin(grid, margin, point))
                        {
                            continue;
                        }

                        float t = (EdgeDistance(grid, point) - margin) / span;
                        if (t < 0f || t > 1f)
                        {
                            continue;
                        }

                        if (IsTall(kind) && point.y < grid.yMin)
                        {
                            continue;
                        }

                        if (kind == EnvironmentKind.Monolith)
                        {
                            if (point.y <= grid.yMax + margin || (!isShoulder && t < 0.3f))
                            {
                                continue;
                            }
                        }
                        else if (!isShoulder && accept > Mathf.Exp(-settings.falloff * t))
                        {
                            continue;
                        }

                        // Keep the two silhouettes readable without clearing any of the meadow tufts.
                        if (kind == EnvironmentKind.BladeRosette || kind == EnvironmentKind.SpiralFern)
                        {
                            float clearanceSquared = 4f * cellSize * cellSize;
                            if ((stoneShoulder.HasValue && (point - stoneShoulder.Value).sqrMagnitude < clearanceSquared)
                                || (mushroomShoulder.HasValue && (point - mushroomShoulder.Value).sqrMagnitude < clearanceSquared))
                                continue;
                        }
                        if (isShoulder)
                        {
                            if (kind == EnvironmentKind.Monolith) stoneShoulder = point;
                            else mushroomShoulder = point;
                        }

                        result.Add(new EnvironmentItem
                        {
                            kind = kind,
                            position = new Vector3(point.x, surfaceY, point.y),
                            yaw = yaw * 360f,
                            scale = Scale(kind, t, jitter),
                            distance01 = t,
                            seed = SeededRandom.ForPart(baseSeed, (uint)((int)kind * 65536 + n)),
                            paletteIndex = (int)(jitter * 4f) & 3
                        });
                        break;
                    }
                }
            }

            return result;
        }

        // Run the same seeded layout in camera-heading coordinates so the near-edge exclusion follows portrait.
        public static List<EnvironmentItem> Generate(EnvironmentSettings settings, Rect grid, float cellSize,
            float surfaceY, float yawDegrees)
        {
            Quaternion heading = Quaternion.Euler(0f, yawDegrees, 0f);
            Vector3 centre = new Vector3(grid.center.x, 0f, grid.center.y);
            Vector3 right = heading * Vector3.right;
            Vector3 forward = heading * Vector3.forward;
            float width = Mathf.Abs(right.x) * grid.width + Mathf.Abs(right.z) * grid.height;
            float depth = Mathf.Abs(forward.x) * grid.width + Mathf.Abs(forward.z) * grid.height;
            Rect localGrid = new Rect(-width * 0.5f, -depth * 0.5f, width, depth);
            List<EnvironmentItem> items = Generate(settings, localGrid, cellSize, surfaceY);
            for (int i = 0; i < items.Count; i++)
            {
                EnvironmentItem item = items[i];
                item.position = centre + heading * item.position;
                item.yaw += yawDegrees;
                items[i] = item;
            }
            return items;
        }

        // Tall kinds stay off the camera side (below the grid in z) so they never stand in front of the board
        static bool IsTall(EnvironmentKind kind)
        {
            return kind == EnvironmentKind.Monolith || kind == EnvironmentKind.MushroomTree;
        }

        static float Scale(EnvironmentKind kind, float t, float jitter)
        {
            if (kind == EnvironmentKind.Monolith)
            {
                return Mathf.Lerp(1.6f, 3.2f, t);
            }

            if (kind == EnvironmentKind.Boulder || kind == EnvironmentKind.Cairn)
            {
                return Mathf.Lerp(0.7f, 2.6f, t) * (0.8f + 0.4f * jitter);
            }

            return Mathf.Lerp(0.8f, 1.6f, t) * (0.8f + 0.4f * jitter);
        }
    }
}
