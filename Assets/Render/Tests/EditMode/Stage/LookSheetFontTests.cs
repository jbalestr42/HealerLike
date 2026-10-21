using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stage
{

public class LookSheetFontTests
{
    readonly List<Object> _created = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (Object created in _created)
        {
            if (created != null)
            {
                Object.DestroyImmediate(created);
            }
        }
        _created.Clear();
    }

    [Test]
    public void Draw_OneGlyph_LightsEachDotAsAScaledSquare()
    {
        Texture2D texture = new Texture2D(64, 32, TextureFormat.RGB24, false);
        _created.Add(texture);
        texture.SetPixels32(new Color32[64 * 32]);

        LookSheetFont.Draw(texture, "I", 2, 30, 2, Color.white);

        int lit = 0;
        foreach (Color32 pixel in texture.GetPixels32())
        {
            lit += pixel.r == 255 ? 1 : 0;
        }
        Assert.AreEqual(60, lit); // the I glyph has 15 dots, each 2 by 2 pixels
        Assert.AreEqual(Color.black, texture.GetPixel(63, 2));
    }
}

}
