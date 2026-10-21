using UnityEngine;

namespace HealerLike.Render.Studio
{

// Pixel counts over a studio capture, against its background colour
public static class StudioTestImages
{
    // Pixels further than threshold from the pixel at backgroundIndex, summed over r, g and b in 0..255
    public static int CountDifferent(Texture2D image, int backgroundIndex, int threshold)
    {
        Color32[] pixels = image.GetPixels32();
        Color32 background = pixels[backgroundIndex];
        int count = 0;
        foreach (Color32 pixel in pixels)
        {
            if (Distance(pixel, background) > threshold)
            {
                count++;
            }
        }
        return count;
    }

    // The same count inside rows yFrom..yTo and columns xFrom..xTo, against the top right pixel
    public static int CountRowsDifferent(Texture2D image, int yFrom, int yTo, int xFrom, int xTo, int threshold)
    {
        Color32[] pixels = image.GetPixels32();
        Color32 background = pixels[pixels.Length - 1];
        int count = 0;
        for (int y = yFrom; y < yTo; y++)
        {
            for (int x = xFrom; x < xTo; x++)
            {
                if (Distance(pixels[y * image.width + x], background) > threshold)
                {
                    count++;
                }
            }
        }
        return count;
    }

    // Pixels clearly green: brighter in green than in red and blue
    public static int CountGreen(Texture2D image)
    {
        int count = 0;
        foreach (Color32 pixel in image.GetPixels32())
        {
            if (pixel.g > pixel.r * 1.15f && pixel.g > pixel.b * 1.2f && pixel.g > 65)
            {
                count++;
            }
        }
        return count;
    }

    static int Distance(Color32 a, Color32 b)
    {
        return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
    }
}

}
