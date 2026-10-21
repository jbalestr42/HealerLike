using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stage
{

public class LookSheetContactTests
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
