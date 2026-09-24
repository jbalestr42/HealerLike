using UnityEngine;

namespace HealerLike.Render.Stage
{
    // Pixel passes over a captured frame: greyscale, the deuteranopia simulation, and square crops
    public static class LookSheetImage
    {
        // Machado, Oliveira and Fernandes 2009, deuteranopia at severity 1.0, for linear RGB
        public static readonly float[] Deuteranopia =
        {
            0.367322f, 0.860646f, -0.227968f,
            0.280085f, 0.672501f, 0.047413f,
            -0.011820f, 0.042940f, 0.968881f
        };

        static readonly int encodeSteps = 4096;
        // sRGB byte to linear, and linear in encodeSteps steps back to an sRGB byte, built once
        static readonly float[] decodeTable = BuildDecode();
        static readonly byte[] encodeTable = BuildEncode();

        // Rec. 709 luminance of each pixel
        public static Color32[] Greyscale(Color32[] pixels)
        {
            Color32[] grey = new Color32[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 pixel = pixels[i];
                byte luminance = (byte)Mathf.RoundToInt(0.2126f * pixel.r + 0.7152f * pixel.g + 0.0722f * pixel.b);
                grey[i] = new Color32(luminance, luminance, luminance, 255);
            }
            return grey;
        }

        // The frame as a deuteranope sees it: sRGB decoded to linear, the matrix applied, clamped and encoded back
        public static Color32[] Deuteranope(Color32[] pixels)
        {
            float[] matrix = Deuteranopia;
            Color32[] simulated = new Color32[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                float red = decodeTable[pixels[i].r];
                float green = decodeTable[pixels[i].g];
                float blue = decodeTable[pixels[i].b];
                simulated[i] = new Color32(Encode(matrix[0] * red + matrix[1] * green + matrix[2] * blue),
                                           Encode(matrix[3] * red + matrix[4] * green + matrix[5] * blue),
                                           Encode(matrix[6] * red + matrix[7] * green + matrix[8] * blue), 255);
            }
            return simulated;
        }

        // A square of the frame whose bottom-left corner is at the pixel, black outside the frame
        public static Color32[] Crop(Color32[] pixels, int width, int height, int x, int y, int size)
        {
            Color32[] crop = new Color32[size * size];
            Color32 black = new Color32(0, 0, 0, 255);
            for (int row = 0; row < size; row++)
            {
                for (int column = 0; column < size; column++)
                {
                    int sourceX = x + column;
                    int sourceY = y + row;
                    bool isInside = sourceX >= 0 && sourceY >= 0 && sourceX < width && sourceY < height;
                    crop[row * size + column] = isInside ? pixels[sourceY * width + sourceX] : black;
                }
            }
            return crop;
        }

        public static Texture2D ToTexture(Color32[] pixels, int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        static byte Encode(float linear)
        {
            int index = Mathf.RoundToInt(Mathf.Clamp01(linear) * (encodeSteps - 1));
            return encodeTable[index];
        }

        static float[] BuildDecode()
        {
            float[] decode = new float[256];
            for (int i = 0; i < 256; i++)
            {
                float encoded = i / 255f;
                decode[i] = encoded <= 0.04045f ? encoded / 12.92f : Mathf.Pow((encoded + 0.055f) / 1.055f, 2.4f);
            }
            return decode;
        }

        static byte[] BuildEncode()
        {
            byte[] encode = new byte[encodeSteps];
            for (int i = 0; i < encodeSteps; i++)
            {
                float linear = (float)i / (encodeSteps - 1);
                float encoded = linear <= 0.0031308f ? linear * 12.92f : 1.055f * Mathf.Pow(linear, 1f / 2.4f) - 0.055f;
                encode[i] = (byte)Mathf.RoundToInt(Mathf.Clamp01(encoded) * 255f);
            }
            return encode;
        }
    }
}
