using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stage
{

public class LookSheetImageTests
{
    [Test]
    public void Deuteranope_Grey_StaysGrey()
    {
        Color32 grey = new Color32(128, 128, 128, 255);

        Color32 simulated = LookSheetImage.Deuteranope(new Color32[] { grey })[0];

        Assert.AreEqual(128, simulated.r, 1);
        Assert.AreEqual(128, simulated.g, 1);
        Assert.AreEqual(128, simulated.b, 1);
    }

    [Test]
    public void Deuteranope_PureRed_TakesTheMatrixFirstColumnInLinear()
    {
        Color32 red = new Color32(255, 0, 0, 255);

        Color32 simulated = LookSheetImage.Deuteranope(new Color32[] { red })[0];

        Assert.AreEqual(163, simulated.r, 1); // linear 0.367 encoded
        Assert.AreEqual(144, simulated.g, 1); // linear 0.280 encoded
        Assert.AreEqual(0, simulated.b); // -0.012 clamped
    }

    [Test]
    public void Greyscale_PureRed_IsItsRec709Luminance()
    {
        Color32[] pixels = { new Color32(255, 0, 0, 255) };

        Color32[] grey = LookSheetImage.Greyscale(pixels);

        Assert.AreEqual(54, grey[0].r); // 0.2126 * 255
        Assert.AreEqual(grey[0].r, grey[0].b);
    }

    [Test]
    public void Crop_PastTheFrame_IsBlackOutside()
    {
        Color32 white = new Color32(255, 255, 255, 255);
        Color32[] pixels = { white, white, white, white };

        Color32[] crop = LookSheetImage.Crop(pixels, 2, 2, 1, 1, 2);

        Assert.AreEqual(white, crop[0]);
        Assert.AreEqual(new Color32(0, 0, 0, 255), crop[1]);
        Assert.AreEqual(new Color32(0, 0, 0, 255), crop[3]);
    }
}

}
