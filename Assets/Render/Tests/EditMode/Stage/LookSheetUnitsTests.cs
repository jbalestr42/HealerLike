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
            if (created != null)
            {
                Object.DestroyImmediate(created);
            }
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

        AssertRow(channels, side, head, count, stem, mass, accessory, accent);
    }

    // The spec's Part 3.1 rows the bestiary proposes, built in memory. Bramble and Splitter stand in for a factory
    // his code lacks and no derivation line reads a thorn collar or twin seeds yet, so they pin what their data draws
    [TestCase("Mender", LookSide.Plant, HeadKind.GiftHeal, CountBand.One, StemBand.Slow, MassBand.Light, AccessoryKind.None, EffectFamily.Heal)]
    [TestCase("Warden", LookSide.Plant, HeadKind.GiftBoonDefence, CountBand.One, StemBand.Slow, MassBand.Light, AccessoryKind.None, EffectFamily.Boon)]
    [TestCase("Mortar", LookSide.Plant, HeadKind.Arch, CountBand.Many, StemBand.Slow, MassBand.Light, AccessoryKind.Antenna, EffectFamily.Damage)]
    [TestCase("Flanker", LookSide.Plant, HeadKind.Bud, CountBand.One, StemBand.Steady, MassBand.Light, AccessoryKind.Hook, EffectFamily.Damage)]
    [TestCase("Bramble", LookSide.Plant, HeadKind.Bud, CountBand.One, StemBand.Steady, MassBand.Light, AccessoryKind.SmallTorus, EffectFamily.Damage)]
    [TestCase("Shieldbearer", LookSide.Stone, HeadKind.GiftBoonDefence, CountBand.One, StemBand.Slow, MassBand.Light, AccessoryKind.None, EffectFamily.Boon)]
    [TestCase("Brute", LookSide.Stone, HeadKind.Bud, CountBand.One, StemBand.Slow, MassBand.Heavy, AccessoryKind.None, EffectFamily.Damage)]
    [TestCase("Plague stone", LookSide.Stone, HeadKind.Bud, CountBand.One, StemBand.Steady, MassBand.Sturdy, AccessoryKind.DripBeads, EffectFamily.Damage)]
    [TestCase("Hexer", LookSide.Stone, HeadKind.GiftBane, CountBand.One, StemBand.Slow, MassBand.Light, AccessoryKind.None, EffectFamily.Bane)]
    [TestCase("Mending stone", LookSide.Stone, HeadKind.GiftHeal, CountBand.One, StemBand.Slow, MassBand.Light, AccessoryKind.None, EffectFamily.Heal)]
    [TestCase("Warded idol", LookSide.Stone, HeadKind.Ward, CountBand.One, StemBand.Slow, MassBand.Light, AccessoryKind.None, EffectFamily.Boon)]
    [TestCase("Rising stone", LookSide.Stone, HeadKind.Bud, CountBand.One, StemBand.Steady, MassBand.Sturdy, AccessoryKind.TierRings, EffectFamily.Damage)]
    [TestCase("Splitter", LookSide.Stone, HeadKind.Bud, CountBand.One, StemBand.Steady, MassBand.Sturdy, AccessoryKind.None, EffectFamily.Damage)]
    [TestCase("Runner", LookSide.Stone, HeadKind.Bud, CountBand.One, StemBand.Steady, MassBand.Light, AccessoryKind.None, EffectFamily.Damage)]
    [TestCase("Warlord", LookSide.Stone, HeadKind.Arch, CountBand.Many, StemBand.Slow, MassBand.Heavy, AccessoryKind.MiniHead, EffectFamily.Damage)]
    public void Create_ProposedUnit_DerivesTheSpecRow(string unit, LookSide side, HeadKind head, CountBand count, StemBand stem,
        MassBand mass, AccessoryKind accessory, EffectFamily accent)
    {
        EntityData data = LookSheetUnits.Create(unit, _created);

        UnitChannels channels = LookDerivation.Channels(data, LookSheetUnits.Side(unit));

        AssertRow(channels, side, head, count, stem, mass, accessory, accent);
    }

    [Test]
    public void Create_Warlord_DrawsItsArmourSkillAsAMiniHead()
    {
        EntityData data = LookSheetUnits.Create("Warlord", _created);

        UnitChannels channels = LookDerivation.Channels(data, LookSheetUnits.Side("Warlord"));

        Assert.AreEqual(HeadKind.GiftBoonDefence, channels.accessoryHead);
    }

    [Test]
    public void Create_RosterEntity_ReturnsHisAsset()
    {
        EntityData data = LookSheetUnits.Create("NormalEntity", _created);

        Assert.IsNotNull(data);
        Assert.AreEqual(0, _created.Count);
    }

    [Test]
    public void Create_Mortar_BakesItsBehavioursIntoAnInactiveCopy()
    {
        EntityData data = LookSheetUnits.Create("Mortar", _created);

        ShootProjectileSkillFactory shoot = (ShootProjectileSkillFactory)data.skillFactories[0];
        GameObject variant = shoot.data.projectiles[0].projectilePrefab;
        Assert.IsFalse(variant.activeInHierarchy);
        Assert.IsNotNull(variant.GetComponent<IncreaseDamageOnDistanceProjectileBehaviour>());
        Assert.IsNotNull(variant.GetComponent<AreaOfEffectProjectileBehaviour>());
    }

    [TestCase("Mortar", "MORTAR*")]
    [TestCase("Runner", "RUNNER*")]
    [TestCase("Mender", "MENDER")]
    [TestCase("HitArmorBufferEntity", "HITARMORBUFFER")]
    public void Label_Unit_StarsTheStandIns(string unit, string expected)
    {
        string label = LookSheetUnits.Label(unit);

        Assert.AreEqual(expected, label);
    }

    [Test]
    public void Draw_Label_LightsPixelsInsideItsBox()
    {
        Texture2D texture = new Texture2D(64, 32, TextureFormat.RGB24, false);
        _created.Add(texture);
        texture.SetPixels32(new Color32[64 * 32]);

        LookSheetFont.Draw(texture, "HEAL*", 2, 30, 2, Color.white);

        int lit = 0;
        foreach (Color32 pixel in texture.GetPixels32())
        {
            lit += pixel.r == 255 ? 1 : 0;
        }
        Assert.Greater(lit, 40);
        Assert.AreEqual(Color.black, texture.GetPixel(63, 2));
    }

    static void AssertRow(UnitChannels channels, LookSide side, HeadKind head, CountBand count, StemBand stem, MassBand mass,
        AccessoryKind accessory, EffectFamily accent)
    {
        Assert.AreEqual(side, channels.side);
        Assert.AreEqual(head, channels.head);
        Assert.AreEqual(count, channels.count);
        Assert.AreEqual(stem, channels.stem);
        Assert.AreEqual(mass, channels.mass);
        Assert.AreEqual(accessory, channels.accessory);
        Assert.AreEqual(accent, channels.accent);
    }
}

}
