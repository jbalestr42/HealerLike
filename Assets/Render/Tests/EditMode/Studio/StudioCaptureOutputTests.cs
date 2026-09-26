using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Studio.Editor
{
    public class StudioCaptureOutputTests
    {
        Texture2D _image;

        [TearDown]
        public void TearDown()
        {
            if (_image != null)
            {
                Object.DestroyImmediate(_image);
            }
        }

        [Test]
        public void Write_FilesystemRejectsDirectory_ReleasesTheOwnedImage()
        {
            _image = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            Assert.Catch(() => StudioCaptureOutput.Write(_image, Path.GetTempPath()));

            Assert.IsTrue(_image == null);
        }
    }
}
