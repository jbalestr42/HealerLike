using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Stage
{
    public class StageCaptureTextureTests
    {
        [Test]
        public void SaveAndRelease_DestinationMissing_ReleasesEncodedTexture()
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "shot.png");
            try
            {
                Assert.Throws<DirectoryNotFoundException>(() => StageCaptureTexture.SaveAndRelease(texture, path));
                Assert.IsTrue(texture == null, "A failed write must still release its CPU readback.");
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void PixelsAndRelease_ReturnsCopiedPixelsWithoutRetainingTexture()
        {
            Texture2D texture = new Texture2D(2, 1, TextureFormat.RGBA32, false);
            texture.SetPixels(new[] { Color.red, Color.blue });
            texture.Apply();
            Color32[] pixels = StageCaptureTexture.PixelsAndRelease(texture);
            Assert.IsTrue(texture == null);
            CollectionAssert.AreEqual(new Color32[] { Color.red, Color.blue }, pixels);
        }

        [Test]
        public void Read_MissingSource_RestoresPreviousActiveTarget()
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture target = new RenderTexture(2, 2, 0);
            try
            {
                RenderTexture.active = target;
                Assert.Throws<NullReferenceException>(() => StageCaptureTexture.Read(null));
                Assert.AreSame(target, RenderTexture.active);
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(target);
            }
        }
    }
}
