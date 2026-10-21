using UnityEngine;

namespace HealerLike.Render.Stage
{
    // Fits a world box into the free screen area, including a sidebar that shifts its horizontal centre.
    public static class StageViewport
    {
        public static Rect Inset(Rect viewport, float margin)
        {
            float inset = Mathf.Min(margin, Mathf.Min(viewport.width, viewport.height) * 0.2f);
            return Rect.MinMaxRect(viewport.xMin + inset, viewport.yMin + inset,
                viewport.xMax - inset, viewport.yMax - inset);
        }

        public static Pose Fit(Bounds bounds, Quaternion rotation, float fov, float aspect, Rect viewport)
        {
            Quaternion inverse = Quaternion.Inverse(rotation);
            float vertical = Mathf.Tan(fov * Mathf.Deg2Rad * 0.5f);
            float horizontal = vertical * aspect;
            float shiftX = viewport.center.x * 2f - 1f;
            float shiftY = viewport.center.y * 2f - 1f;
            float right = viewport.xMax * 2f - 1f;
            float left = 1f - viewport.xMin * 2f;
            float top = viewport.yMax * 2f - 1f;
            float bottom = 1f - viewport.yMin * 2f;
            float distance = 2f;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 local = inverse * Vector3.Scale(bounds.extents, RenderMath.CornerSign(corner));
                distance = Mathf.Max(distance, (local.x - right * horizontal * local.z) / ((right - shiftX) * horizontal));
                distance = Mathf.Max(distance, (-local.x - left * horizontal * local.z) / ((left + shiftX) * horizontal));
                distance = Mathf.Max(distance, (local.y - top * vertical * local.z) / ((top - shiftY) * vertical));
                distance = Mathf.Max(distance, (-local.y - bottom * vertical * local.z) / ((bottom + shiftY) * vertical));
            }

            Vector3 offset = new Vector3(shiftX * horizontal * distance, shiftY * vertical * distance, distance);
            return new Pose(bounds.center - rotation * offset, rotation);
        }
    }
}
