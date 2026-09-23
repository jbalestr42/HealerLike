using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Stage
{

public class LookSheetSpellsTests
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

    // The spec's Part 3.2 rows that leave a status, as the healer casts them
    [TestCase("Quicken", true, EffectFamily.Boon, AttributeGroup.Offence, EffectTempo.ForDuration)]
    [TestCase("Blight", false, EffectFamily.Rot, AttributeGroup.Offence, EffectTempo.PerPeriod)]
    [TestCase("Weaken", false, EffectFamily.Bane, AttributeGroup.Offence, EffectTempo.ForDuration)]
    [TestCase("Focus", true, EffectFamily.Boon, AttributeGroup.Offence, EffectTempo.ForDuration)]
    [TestCase("Renew", true, EffectFamily.Renew, AttributeGroup.Offence, EffectTempo.PerPeriod)]
    [TestCase("Barkskin", true, EffectFamily.Boon, AttributeGroup.Defence, EffectTempo.Once)]
    [TestCase("Sanctuary", true, EffectFamily.Boon, AttributeGroup.Prevention, EffectTempo.ForDuration)]
    [TestCase("Mark of ruin", false, EffectFamily.Bane, AttributeGroup.Defence, EffectTempo.ForDuration)]
    [TestCase("Overgrowth", true, EffectFamily.Boon, AttributeGroup.Offence, EffectTempo.ForDuration)]
    public void Handler_StatusSpell_DerivesTheSpecRow(string spell, bool isSameSide, EffectFamily family, AttributeGroup group,
        EffectTempo tempo)
    {
        ABuffHandlerFactory handler = LookSheetSpells.Handler(spell, _created);

        EffectChannels channels = EffectDerivation.Channels(handler, isSameSide);

        Assert.AreEqual(family, channels.family);
        Assert.AreEqual(group, channels.group);
        Assert.AreEqual(tempo, channels.tempo);
    }

    [TestCase("Heal", 30f)]
    [TestCase("Heal group", 16f)]
    [TestCase("Stonefall", -60f)]
    [TestCase("Lifebloom", 60f)]
    [TestCase("Transfusion", 20f)]
    public void Handler_OutcomeSpell_ShowsAnImpactAndNoStatus(string spell, float impact)
    {
        ABuffHandlerFactory handler = LookSheetSpells.Handler(spell, _created);

        Assert.IsNull(handler);
        Assert.AreEqual(impact, LookSheetSpells.Impact(spell));
    }

    [Test]
    public void Spells_Roster_CoversTheFifteenRows()
    {
        int count = LookSheetSpells.Spells.Length;

        Assert.AreEqual(15, count);
    }

    [TestCase("Weaken", true, "WEAKEN ON STONE")]
    [TestCase("Sprout", false, "SPROUT*")]
    [TestCase("Heal", false, "HEAL")]
    public void Label_Spell_StarsTheStandInsAndNamesTheStone(string spell, bool isOnStone, string expected)
    {
        string label = LookSheetSpells.Label(spell, isOnStone);

        Assert.AreEqual(expected, label);
    }

    [Test]
    public void Sapling_Channels_DrawALightBud()
    {
        EntityData sapling = LookSheetSpells.Sapling(_created);

        UnitChannels channels = LookDerivation.Channels(sapling, Entity.EntityType.Player);

        Assert.AreEqual(HeadKind.Bud, channels.head);
        Assert.AreEqual(MassBand.Light, channels.mass);
    }
}

}
