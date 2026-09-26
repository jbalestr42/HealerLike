using System.IO;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // Terminal read/write operations own their temporary CPU texture, even when encoding or file I/O fails.
    public static class StageCaptureTexture
    {
        public static void SaveAndRelease(Texture2D texture, string path)
        {
            try
            {
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                RenderObjects.Release(texture);
            }
        }

        public static Color32[] PixelsAndRelease(Texture2D texture)
        {
            try
            {
                return texture.GetPixels32();
            }
            finally
            {
                RenderObjects.Release(texture);
            }
        }

        public static Color[] Read(RenderTexture source)
        {
            Texture2D copy = null;
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = source;
                copy = new Texture2D(source.width, source.height, TextureFormat.RGBAFloat, false, true);
                copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                copy.Apply();
                return copy.GetPixels();
            }
            finally
            {
                RenderTexture.active = previous;
                RenderObjects.Release(copy);
            }
        }
    }
}
