using UnityEngine;

namespace HealerLike.Render.Stage
{
    // The pixel arithmetic the readability harness shares: which pixels a render change moved, how many of a
    // creature's own pixels an effect leaves untouched, and the mean colour, luma and distance of a pixel set.
    // Colours are the stored sRGB bytes, 0 to 255, as the phone displays them.
    public static class ReadabilityPixels
    {
        // A pixel belongs to what was toggled between two renders when any channel moves by more than this
        public const int Threshold = 6;

        public static bool IsChanged(Color32 a, Color32 b, int threshold = Threshold)
        {
            return Mathf.Max(Mathf.Abs(a.r - b.r), Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b)) > threshold;
        }

        public static bool[] Changed(Color32[] a, Color32[] b, int threshold = Threshold)
        {
            bool[] changed = new bool[a.Length];
            for (int i = 0; i < a.Length; i++)
            {
                changed[i] = IsChanged(a[i], b[i], threshold);
            }

            return changed;
        }

        // The fraction of the mask's pixels that read the same with and without the effect; 1 when the mask is empty,
        // since an effect cannot hide a creature that has no pixels
        public static double Survival(bool[] mask, Color32[] withEffect, Color32[] withoutEffect, int threshold = Threshold)
        {
            int total = 0, kept = 0;
            for (int i = 0; i < mask.Length; i++)
            {
                if (!mask[i])
                {
                    continue;
                }

                total++;
                if (!IsChanged(withEffect[i], withoutEffect[i], threshold))
                {
                    kept++;
                }
            }
            return total == 0 ? 1.0 : (double)kept / total;
        }

        // The mask grown by radius pixels in a square, the mask included: the field behind and around a shape
        public static bool[] Grow(bool[] mask, int width, int height, int radius)
        {
            int[] sum = new int[(width + 1) * (height + 1)];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    sum[(y + 1) * (width + 1) + x + 1] = (mask[y * width + x] ? 1 : 0) + sum[y * (width + 1) + x + 1]
                        + sum[(y + 1) * (width + 1) + x] - sum[y * (width + 1) + x];
                }
            }

            bool[] grown = new bool[mask.Length];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int x0 = Mathf.Max(0, x - radius), x1 = Mathf.Min(width, x + radius + 1);
                    int y0 = Mathf.Max(0, y - radius), y1 = Mathf.Min(height, y + radius + 1);
                    grown[y * width + x] = sum[y1 * (width + 1) + x1] - sum[y0 * (width + 1) + x1]
                        - sum[y1 * (width + 1) + x0] + sum[y0 * (width + 1) + x0] > 0;
                }
            }

            return grown;
        }

        public static int Count(bool[] mask)
        {
            int count = 0;
            foreach (bool value in mask)
            {
                count += value ? 1 : 0;
            }

            return count;
        }

        // Mean colour of the masked pixels, black when the mask is empty
        public static double[] Mean(Color32[] pixels, bool[] mask)
        {
            double[] total = new double[3];
            int count = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                if (!mask[i])
                {
                    continue;
                }

                count++;
                total[0] += pixels[i].r;
                total[1] += pixels[i].g;
                total[2] += pixels[i].b;
            }
            for (int k = 0; k < 3; k++)
            {
                total[k] /= Mathf.Max(1, count);
            }

            return total;
        }

        // Rec. 709 weights on the stored sRGB bytes
        public static double Luma(double[] rgb) { return .2126 * rgb[0] + .7152 * rgb[1] + .0722 * rgb[2]; }

        public static double Distance(double[] a, double[] b)
        {
            return System.Math.Sqrt((a[0] - b[0]) * (a[0] - b[0]) + (a[1] - b[1]) * (a[1] - b[1])
                + (a[2] - b[2]) * (a[2] - b[2]));
        }

        // A shape read against what is behind it: both means, their luma difference and RGB distance
        public struct Contrast
        {
            public int pixels;
            public double[] shape;
            public double[] field;
            public double shapeLuma;
            public double fieldLuma;
            public double lumaDifference;
            public double rgbDistance;
        }

        // shape pixels from the frame that shows it, field pixels from the frame without it, over the shape and a
        // band of ringPixels around it
        public static Contrast Measure(bool[] shape, Color32[] withShape, Color32[] withoutShape, int width, int height,
                                       int ringPixels)
        {
            Contrast result = new Contrast
            {
                pixels = Count(shape),
                shape = Mean(withShape, shape),
                field = Mean(withoutShape, Grow(shape, width, height, ringPixels))
            };
            result.shapeLuma = Luma(result.shape);
            result.fieldLuma = Luma(result.field);
            result.lumaDifference = result.shapeLuma - result.fieldLuma;
            result.rgbDistance = Distance(result.shape, result.field);
            return result;
        }
    }
}
