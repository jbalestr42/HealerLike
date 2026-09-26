using UnityEngine;
using System.Collections.Generic;

namespace HealerLike.Render.Environment
{
    // Fits distant scenery crowns below the top HUD while keeping their ground anchors outside the board.
    public static class EnvironmentFraming
    {
        public static void FrameCrowns(Transform root, IReadOnlyList<EnvironmentItem> items, Camera camera)
        {
            if (root == null || camera == null)
            {
                return;
            }
            for (int i = 0; i < items.Count; i++)
            {
                EnvironmentKind kind = items[i].kind;
                if (kind != EnvironmentKind.MushroomTree && kind != EnvironmentKind.Monolith)
                {
                    continue;
                }
                Transform pivot = root.GetChild(i);
                pivot.gameObject.SetActive(true);
                pivot.localScale = Vector3.one;
                Renderer[] parts = pivot.GetComponentsInChildren<Renderer>(true);
                if (parts.Length == 0)
                {
                    continue;
                }
                Bounds bounds = parts[0].bounds;
                foreach (Renderer part in parts)
                {
                    bounds.Encapsulate(part.bounds);
                }
                float scale = EnvironmentFraming.CrownScale(bounds, pivot.position, camera.transform.position,
                    camera.transform.rotation, camera.fieldOfView, camera.aspect);
                pivot.localScale = Vector3.one * scale;
                pivot.gameObject.SetActive(scale > 0f);
            }
        }

        public const float CrownCeiling = 0.88f;

        public static float CrownScale(Bounds bounds, Vector3 anchor, Vector3 eye, Quaternion rotation,
            float fov, float aspect)
        {
            float left = float.PositiveInfinity;
            float right = float.NegativeInfinity;
            for (int i = 0; i < 8; i++)
            {
                Vector2 point = EnvironmentForeground.ToViewport(RenderMath.Corner(bounds, i), eye, rotation,
                    fov, aspect);
                left = Mathf.Min(left, point.x);
                right = Mathf.Max(right, point.x);
            }
            // Scenery outside the visible field retains its authored size.
            if (right < 0f || left > 1f || Top(bounds, anchor, 1f, eye, rotation, fov, aspect) <= CrownCeiling)
                return 1f;
            if (EnvironmentForeground.ToViewport(anchor, eye, rotation, fov, aspect).y >= CrownCeiling)
                return 0f;

            float low = 0f;
            float high = 1f;
            for (int i = 0; i < 16; i++)
            {
                float scale = (low + high) * 0.5f;
                if (Top(bounds, anchor, scale, eye, rotation, fov, aspect) <= CrownCeiling) low = scale;
                else high = scale;
            }
            // A distant object too small to retain its silhouette can leave the frame to the fog ridge.
            return low >= 0.25f ? low : 0f;
        }

        static float Top(Bounds bounds, Vector3 anchor, float scale, Vector3 eye, Quaternion rotation,
            float fov, float aspect)
        {
            float top = float.NegativeInfinity;
            for (int i = 0; i < 8; i++)
            {
                Vector3 world = anchor + (RenderMath.Corner(bounds, i) - anchor) * scale;
                Vector2 point = EnvironmentForeground.ToViewport(world, eye, rotation, fov, aspect);
                top = Mathf.Max(top, point.y);
            }
            return top;
        }
    }
}
