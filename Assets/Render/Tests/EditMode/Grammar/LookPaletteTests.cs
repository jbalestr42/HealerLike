using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Grammar
{

public class LookPaletteTests
{
    static readonly string palettePath = "Assets/Render/Grammar/Data/LookPalette.asset";

    readonly List<Object> _objects = new List<Object>();

    static Color Hex(int rgb)
    {
        return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object trackedObject in _objects)
        {
            Object.DestroyImmediate(trackedObject);
        }
        _objects.Clear();
    }

    // Every field a different red, so the red a role returns names the field it read
    LookPalette CreatePalette()
    {
        LookPalette palette = ScriptableObject.CreateInstance<LookPalette>();
        _objects.Add(palette);
        palette.plantBody = new Color(0.1f, 0f, 0f);
        palette.plantStem = new Color(0.2f, 0f, 0f);
        palette.stoneBody = new Color(0.3f, 0f, 0f);
        palette.stoneLimb = new Color(0.4f, 0f, 0f);
        palette.stoneOchre = new Color(0.5f, 0f, 0f);
        palette.moss = new Color(0.6f, 0f, 0f);
        palette.damage = new Color(0.7f, 0f, 0f);
        palette.boon = new Color(0.8f, 0f, 0f);
        palette.bane = new Color(0.9f, 0f, 0f);
        palette.rot = new Color(0.95f, 0f, 0f);
        palette.stoneWilt = new Color(0.15f, 0f, 0f);
        palette.baneLit = new Color(0.25f, 0f, 0f);
        palette.mana = new Color(0.35f, 0f, 0f);
        return palette;
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

    [TestCase(ColourRole.Body, LookSide.Plant, 0.1f)] // plantBody
    [TestCase(ColourRole.Body, LookSide.Stone, 0.3f)] // stoneBody
    [TestCase(ColourRole.Stem, LookSide.Plant, 0.2f)] // plantStem
    [TestCase(ColourRole.Stem, LookSide.Stone, 0.4f)] // stoneLimb
    [TestCase(ColourRole.Limb, LookSide.Plant, 0.2f)] // plantStem, a plant has no limb colour of its own
    [TestCase(ColourRole.Limb, LookSide.Stone, 0.4f)] // stoneLimb
    [TestCase(ColourRole.Moss, LookSide.Plant, 0.6f)]
    [TestCase(ColourRole.Moss, LookSide.Stone, 0.6f)]
    [TestCase(ColourRole.Ochre, LookSide.Plant, 0.5f)] // stoneOchre
    [TestCase(ColourRole.Ochre, LookSide.Stone, 0.5f)]
    [TestCase(ColourRole.Accent, LookSide.Plant, 0.7f)] // damage, the accent passed in
    [TestCase(ColourRole.Accent, LookSide.Stone, 0.7f)]
    [TestCase(ColourRole.BoonAccent, LookSide.Plant, 0.8f)]
    [TestCase(ColourRole.BoonAccent, LookSide.Stone, 0.8f)]
    [TestCase(ColourRole.BaneAccent, LookSide.Plant, 0.9f)]
    [TestCase(ColourRole.BaneAccent, LookSide.Stone, 0.9f)]
    [TestCase(ColourRole.RotAccent, LookSide.Plant, 0.95f)]
    [TestCase(ColourRole.RotAccent, LookSide.Stone, 0.95f)]
    [TestCase(ColourRole.Wilt, LookSide.Plant, 0.2f)] // plantStem
    [TestCase(ColourRole.Wilt, LookSide.Stone, 0.15f)] // stoneWilt
    [TestCase(ColourRole.Rim, LookSide.Plant, 0.1f)] // plantBody
    [TestCase(ColourRole.Rim, LookSide.Stone, 0.25f)] // baneLit
    [TestCase(ColourRole.Mana, LookSide.Plant, 0.35f)]
    [TestCase(ColourRole.Mana, LookSide.Stone, 0.35f)]
    public void Colour_RoleAndSide_ReadsTheRolesField(ColourRole role, LookSide side, float expectedRed)
    {
        LookPalette palette = CreatePalette();

        Color colour = palette.Colour(role, EffectFamily.Damage, side);

        Assert.AreEqual(expectedRed, colour.r, 0.0001f);
    }
}

}
