using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // How much two units' silhouettes cover the same pixels, over boolean masks of the same size
    public static class SilhouetteOverlap
    {
        // Two masks above this read as one silhouette
        public static readonly float CollisionThreshold = 0.85f;

        public class Pair
        {
            public int first;
            public int second;
            public float iou;
        }

        // Intersection over union; two empty masks share nothing
        public static float IoU(bool[] a, bool[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
            {
                Debug.LogError("[SilhouetteOverlap] IoU needs two masks of the same size");
                return 0f;
            }

            int intersection = 0;
            int union = 0;
            for (int i = 0; i < a.Length; i++)
            {
                intersection += a[i] && b[i] ? 1 : 0;
                union += a[i] || b[i] ? 1 : 0;
            }

            if (union == 0)
            {
                return 0f;
            }
            return (float)intersection / union;
        }

        // A pixel belongs to the unit when it differs from the ground colour by more than the tolerance,
        // summed over the three channels
        public static bool[] Mask(Color32[] pixels, Color32 ground, int tolerance)
        {
            bool[] mask = new bool[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                mask[i] = Difference(pixels[i], ground) > tolerance;
            }
            return mask;
        }

        // The same against the ground as rendered without the unit, pixel by pixel, so shading and fog cancel
        public static bool[] Mask(Color32[] pixels, Color32[] ground, int tolerance)
        {
            if (ground == null || ground.Length != pixels.Length)
            {
                Debug.LogError("[SilhouetteOverlap] The ground render must match the cell's size");
                return new bool[pixels.Length];
            }

            bool[] mask = new bool[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                mask[i] = Difference(pixels[i], ground[i]) > tolerance;
            }
            return mask;
        }

        public static int Count(bool[] mask)
        {
            int count = 0;
            foreach (bool isSet in mask)
            {
                count += isSet ? 1 : 0;
            }
            return count;
        }

        // Every pair of masks above the threshold, the closest first
        public static List<Pair> Collisions(List<bool[]> masks, float threshold)
        {
            List<Pair> pairs = new List<Pair>();
            for (int i = 0; i < masks.Count; i++)
            {
                for (int j = i + 1; j < masks.Count; j++)
                {
                    float iou = IoU(masks[i], masks[j]);
                    if (iou > threshold)
                    {
                        pairs.Add(new Pair { first = i, second = j, iou = iou });
                    }
                }
            }

            pairs.Sort((a, b) => b.iou.CompareTo(a.iou));
            return pairs;
        }

        static int Difference(Color32 a, Color32 b)
        {
            return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
        }
    }
}
