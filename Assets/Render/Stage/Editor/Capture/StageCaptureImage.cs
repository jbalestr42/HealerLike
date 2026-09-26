using System.IO;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // Native screenshots are decoded only for acceptance measurements, then released on every outcome.
    public static class StageCaptureImage
    {
        public static bool MatchesSize(string path, int width, int height)
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D image = new Texture2D(2, 2, TextureFormat.RGB24, false);
            try
            {
                return image.LoadImage(bytes) && image.width == width && image.height == height;
            }
            finally
            {
                RenderObjects.Release(image);
            }
        }
    }
}
