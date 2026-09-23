using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Stage
{

public class LookSheetUnitsTests
{
    readonly List<Object> _created = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (Object created in _created)
        {
            Object.DestroyImmediate(created);
        }
        _created.Clear();
    }

    // The spec's Part 3.3 rows, five units nobody designed, built in memory from his classes and prefabs
    [TestCase("Stormreed", LookSide.Plant, HeadKind.Fork, CountBand.Few, StemBand.Quick, MassBand.Light, AccessoryKind.None, EffectFamily.Damage)]
    [TestCase("Puffball", LookSide.Plant, HeadKind.Pulse, CountBand.Many, StemBand.Steady, MassBand.Sturdy, AccessoryKind.None, EffectFamily.Damage)]
    [TestCase("Old fern", LookSide.Plant, HeadKind.SelfTick, CountBand.One, StemBand.Slow, MassBand.Heavy, AccessoryKind.None, EffectFamily.Renew)]
    [TestCase("Needle stone", LookSide.Stone, HeadKind.Spear, CountBand.Few, StemBand.Quick, MassBand.Light, AccessoryKind.None, EffectFamily.Damage)]
    [TestCase("Storm idol", LookSide.Stone, HeadKind.Conductor, CountBand.One, StemBand.Steady, MassBand.Heavy, AccessoryKind.DripBeads, EffectFamily.Damage)]
    public void Create_UndesignedUnit_DerivesTheSpecRow(string unit, LookSide side, HeadKind head, CountBand count, StemBand stem,
        MassBand mass, AccessoryKind accessory, EffectFamily accent)
    {
        EntityData data = LookSheetUnits.Create(unit, _created);

        UnitChannels channels = LookDerivation.Channels(data, LookSheetUnits.Side(unit));

        Assert.AreEqual(side, channels.side);
        Assert.AreEqual(head, channels.head);
        Assert.AreEqual(count, channels.count);
        Assert.AreEqual(stem, channels.stem);
        Assert.AreEqual(mass, channels.mass);
        Assert.AreEqual(accessory, channels.accessory);
        Assert.AreEqual(accent, channels.accent);
    }

    [TestCase("ROT", EffectFamily.Rot)]
    [TestCase("RENEW", EffectFamily.Renew)]
    [TestCase("BOON", EffectFamily.Boon)]
    [TestCase("BOON DEFENCE", EffectFamily.Boon)]
    [TestCase("BANE", EffectFamily.Bane)]
    public void Handler_Family_DerivesThatFamily(string family, EffectFamily expected)
    {
        ABuffHandlerFactory handler = LookSheetUnits.Handler(family, _created);

        Assert.AreEqual(expected, EffectDerivation.Family(handler, true));
    }

    [Test]
    public void Draw_Label_LightsPixelsInsideItsBox()
    {
        Texture2D texture = new Texture2D(64, 32, TextureFormat.RGB24, false);
        _created.Add(texture);
        texture.SetPixels32(new Color32[64 * 32]);

        SheetFont.Draw(texture, "HEAL", 2, 30, 2, Color.white);

        int lit = 0;
        foreach (Color32 pixel in texture.GetPixels32())
        {
            lit += pixel.r == 255 ? 1 : 0;
        }
        Assert.Greater(lit, 40);
        Assert.AreEqual(Color.black, texture.GetPixel(62, 2));
    }
}

}
