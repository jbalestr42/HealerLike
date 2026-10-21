using UnityEngine;

namespace HealerLike.Render.Stage
{
    // Pixel differences inside a named region. HUD and actor movement outside it cannot satisfy this check.
    public static class StageMotionMeasure
    {
        public static Vector2 Difference(Color32[] first, Color32[] second, int width, int height, Rect region)
        {
            if (width < 1 || height < 1 || first == null || second == null
                || first.Length != width * height || second.Length != first.Length)
            {
                return new Vector2(-1f, -1f);
            }

            int left = Mathf.Clamp(Mathf.FloorToInt(region.xMin * width), 0, width);
            int right = Mathf.Clamp(Mathf.CeilToInt(region.xMax * width), 0, width);
            int bottom = Mathf.Clamp(Mathf.FloorToInt(region.yMin * height), 0, height);
            int top = Mathf.Clamp(Mathf.CeilToInt(region.yMax * height), 0, height);
            long total = 0;
            int changed = 0;
            int count = 0;
            for (int y = bottom; y < top; y++)
            {
                for (int x = left; x < right; x++)
                {
                    Color32 a = first[y * width + x];
                    Color32 b = second[y * width + x];
                    int difference = Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
                    total += difference;
                    changed += difference > 9 ? 1 : 0;
                    count++;
                }
            }

            return count > 0 ? new Vector2((float)total / (count * 3f), (float)changed / count)
                : new Vector2(-1f, -1f);
        }

        public static bool Pass(float control, Vector2 motion, bool cameraFixed)
        {
            return cameraFixed && control >= 0f && motion.x > Mathf.Max(0.3f, control * 4f)
                && motion.y > 0.02f;
        }
    }
}
