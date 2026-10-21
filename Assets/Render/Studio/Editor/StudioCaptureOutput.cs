using System.IO;
using UnityEngine;

namespace HealerLike.Render.Studio.Editor
{
    // The image belongs to the writer once handed over, including when encoding or the filesystem fails.
    public static class StudioCaptureOutput
    {
        public static void Write(Texture2D image, string path)
        {
            if (image == null)
            {
                return;
            }
            try
            {
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(image);
            }
        }
    }
}
