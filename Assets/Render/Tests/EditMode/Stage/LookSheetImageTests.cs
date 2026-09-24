using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stage
{

public class LookSheetImageTests
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

    [Test]
    public void Board_SixteenCellsAcross_DrawsACellAtTheCentreInASixteenthOfTheWidth()
    {
        Quaternion rotation = Quaternion.Euler(52f, 0f, 0f);
        Vector3 centre = new Vector3(3f, 0.5f, -2f);

        Pose pose = LookSheetCamera.Board(centre, 1f, rotation, 40f, 9f / 16f, 1080);

        // The view is BoardCells cells wide at the centre's depth, so a cell spans 1080 / 16 = 67.5 px
        float depth = Vector3.Distance(pose.position, centre);
        float viewWidth = 2f * depth * Mathf.Tan(40f * Mathf.Deg2Rad * 0.5f) * (9f / 16f);
        Assert.AreEqual(LookSheetCamera.BoardCells, viewWidth, 0.001f);
        Assert.AreEqual(rotation, pose.rotation);
    }

    [Test]
    public void Build_OneCell_LaysColourGreyAndDeuteranopiaSideBySide()
    {
        LookSheetContact contact = new LookSheetContact(165);
        Color32[] colour = Fill(new Color32(200, 40, 40, 255));
        Color32[] grey = Fill(new Color32(70, 70, 70, 255));
        Color32[] deuteranope = Fill(new Color32(120, 110, 40, 255));
        contact.Add("MENDER", colour, grey, deuteranope);

        Texture2D sheet = contact.Build("UNITS");
        _created.Add(sheet);

        int side = 165 * 2; // 330 / 165
        int bottom = sheet.height - (8 * LookSheetContact.LabelScale + 12) - side;
        Assert.AreEqual(colour[0], (Color32)sheet.GetPixel(10, bottom + 10));
        Assert.AreEqual(grey[0], (Color32)sheet.GetPixel(side + LookSheetContact.Gap + 10, bottom + 10));
        Assert.AreEqual(deuteranope[0], (Color32)sheet.GetPixel(2 * (side + LookSheetContact.Gap) + 10, bottom + 10));
    }

    static Color32[] Fill(Color32 colour)
    {
        Color32[] pixels = new Color32[165 * 165];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = colour;
        }
        return pixels;
    }
}

}
