using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Grammar
{

public class LookPaletteTests
{
    static readonly string palettePath = "Assets/Render/Grammar/Data/LookPalette.asset";

    static Color Hex(int rgb)
    {
        return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);
    }

    [TestCase(EffectFamily.Damage, 0xf6969a)]
    [TestCase(EffectFamily.Heal, 0xd6f84c)]
    [TestCase(EffectFamily.Rot, 0x8e5bd6)]
    [TestCase(EffectFamily.Renew, 0xd6f84c)]
    [TestCase(EffectFamily.Boon, 0xdfaf34)]
    [TestCase(EffectFamily.Bane, 0x203b64)]
    public void Accent_LiveAsset_ReturnsTheStudyAccent(EffectFamily family, int rgb)
    {
        LookPalette palette = AssetDatabase.LoadAssetAtPath<LookPalette>(palettePath);
        Assert.NotNull(palette, palettePath);

        Color accent = palette.Accent(family);

        Color expected = Hex(rgb);
        Assert.AreEqual(expected.r, accent.r, 0.002f);
        Assert.AreEqual(expected.g, accent.g, 0.002f);
        Assert.AreEqual(expected.b, accent.b, 0.002f);
    }
}

}
