using HealerLike.Render.Grammar;
using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Spells
{

public class EffectComposerTests
{
    static EffectChannels Channels(EffectFamily family, AttributeGroup group,
                                   EffectTempo tempo = EffectTempo.ForDuration)
    {
        return new EffectChannels { family = family, group = group, tempo = tempo };
    }

    [TestCase(EffectFamily.Damage, AttributeGroup.Offence, EffectElement.Burst)]
    [TestCase(EffectFamily.Heal, AttributeGroup.Offence, EffectElement.Rise)]
    [TestCase(EffectFamily.Rot, AttributeGroup.Offence, EffectElement.Drips)]
    [TestCase(EffectFamily.Renew, AttributeGroup.Offence, EffectElement.Stalks)]
    [TestCase(EffectFamily.Boon, AttributeGroup.Offence, EffectElement.Orbit)]
    [TestCase(EffectFamily.Boon, AttributeGroup.Defence, EffectElement.Plates)]
    [TestCase(EffectFamily.Boon, AttributeGroup.Prevention, EffectElement.Bud)]
    [TestCase(EffectFamily.Bane, AttributeGroup.Offence, EffectElement.Press)]
    [TestCase(EffectFamily.Bane, AttributeGroup.Defence, EffectElement.Crack)]
    public void Element_FamilyAndGroup_PicksTheTableElement(EffectFamily family, AttributeGroup group,
                                                            EffectElement expected)
    {
        Assert.AreEqual(expected, EffectComposer.Element(Channels(family, group)));
    }

    [Test]
    public void Element_EveryFamilyGroupAndTempo_ResolvesToAVocabularyEntry()
    {
        EffectVocabulary vocabulary = RenderTestAssets.LoadEffectVocabulary();

        foreach (EffectFamily family in System.Enum.GetValues(typeof(EffectFamily)))
        {
            foreach (AttributeGroup group in System.Enum.GetValues(typeof(AttributeGroup)))
            {
                foreach (EffectTempo tempo in System.Enum.GetValues(typeof(EffectTempo)))
                {
                    EffectRecipe recipe = EffectComposer.Compose(vocabulary, Channels(family, group, tempo), 1, 0f);

                    Assert.IsNotNull(recipe, $"{family} {group} {tempo}");
                    Assert.AreEqual(tempo, recipe.tempo);
                }
            }
        }
    }

    [TestCase(AttributeGroup.Offence)]
    [TestCase(AttributeGroup.Defence)]
    [TestCase(AttributeGroup.Prevention)]
    public void Element_BoonAgainstBane_DifferentElements(AttributeGroup group)
    {
        Assert.AreNotEqual(EffectComposer.Element(Channels(EffectFamily.Boon, group)),
                           EffectComposer.Element(Channels(EffectFamily.Bane, group)));
    }

    [TestCase(true, EffectElement.ManaUp)]
    [TestCase(false, EffectElement.ManaDown)]
    public void Mana_Sign_PicksUpOrDown(bool isGain, EffectElement expected)
    {
        Assert.AreEqual(expected, EffectComposer.Mana(isGain));
    }

    [Test]
    public void Compose_PerPeriod_ClockIsThePeriod()
    {
        EffectChannels channels = Channels(EffectFamily.Rot, AttributeGroup.Offence, EffectTempo.PerPeriod);
        channels.periodSeconds = 1.5f;

        EffectRecipe recipe = EffectComposer.Compose(RenderTestAssets.LoadEffectVocabulary(), channels, 1, 0f);

        Assert.AreEqual(1.5f, recipe.cycleSeconds);
    }

    [Test]
    public void Compose_Heal_TakesTheLimeAccent()
    {
        EffectVocabulary vocabulary = RenderTestAssets.LoadEffectVocabulary();

        EffectChannels channels = Channels(EffectFamily.Heal, AttributeGroup.Offence);

        EffectRecipe recipe = EffectComposer.Compose(vocabulary, channels, 1, 0f);

        Assert.AreEqual(vocabulary.palette.heal, recipe.colour);
    }

    [Test]
    public void Compose_Mana_TakesTheManaColour()
    {
        EffectVocabulary vocabulary = RenderTestAssets.LoadEffectVocabulary();

        EffectRecipe recipe = EffectComposer.Compose(vocabulary, EffectElement.ManaUp, EffectFamily.Heal,
                                                     EffectTempo.Once, 0f, 1, 0f, 0f);

        Assert.AreEqual(vocabulary.palette.mana, recipe.colour);
    }

    [TestCase(1, 2)]
    [TestCase(2, 3)]
    [TestCase(9, 3)]
    public void Count_OrbitStacks_TwoOrThreeTori(int stacks, int expected)
    {
        ElementEntry orbit = RenderTestAssets.LoadEffectVocabulary().GetEntry(EffectElement.Orbit);

        Assert.AreEqual(expected, EffectComposer.Count(orbit, stacks, 0f, 0f));
    }

    [TestCase(1f, 1)]
    [TestCase(2f, 2)]
    [TestCase(2.5f, 3)]
    public void Count_PlateCharges_OnePlatePerCharge(float charges, int expected)
    {
        ElementEntry plates = RenderTestAssets.LoadEffectVocabulary().GetEntry(EffectElement.Plates);

        Assert.AreEqual(expected, EffectComposer.Count(plates, 1, charges, 0f));
    }

    [Test]
    public void Compose_NoVocabulary_ReturnsNull()
    {
        Assert.IsNull(EffectComposer.Compose(null, Channels(EffectFamily.Heal, AttributeGroup.Offence), 1, 0f));
    }

    [TestCase(ResourceKind.Health, true, EffectElement.Rise, EffectFamily.Heal)]
    [TestCase(ResourceKind.Health, false, EffectElement.Burst, EffectFamily.Damage)]
    [TestCase(ResourceKind.Mana, true, EffectElement.ManaUp, EffectFamily.Heal)]
    [TestCase(ResourceKind.Mana, false, EffectElement.ManaDown, EffectFamily.Damage)]
    public void Impact_ResourceAndSign_PicksTheElementAndFamily(ResourceKind resource, bool isGain,
                                                               EffectElement element, EffectFamily family)
    {
        EffectRecipe recipe = EffectComposer.Impact(RenderTestAssets.LoadEffectVocabulary(), resource, isGain, 0.2f);

        Assert.AreEqual(element, recipe.element);
        Assert.AreEqual(family, recipe.family);
    }

    [Test]
    public void Impact_HarderHit_ScalesTheBurstUpToTheMaximum()
    {
        EffectVocabulary vocabulary = RenderTestAssets.LoadEffectVocabulary();

        EffectRecipe light = EffectComposer.Impact(vocabulary, ResourceKind.Health, false, 0f);
        EffectRecipe full = EffectComposer.Impact(vocabulary, ResourceKind.Health, false, 1f);

        Assert.AreEqual(EffectComposer.BurstScaleMin, light.scale);
        Assert.AreEqual(EffectComposer.BurstScaleMax, full.scale);
        Assert.AreEqual(1f, EffectComposer.Impact(vocabulary, ResourceKind.Health, true, 1f).scale);
    }

    [TestCase(ZoneKind.Heal, EffectElement.Ring, EffectFamily.Heal)]
    [TestCase(ZoneKind.Hostile, EffectElement.Litter, EffectFamily.Bane)]
    public void Area_Kind_ComposesTheFootprintForTheEntryCycle(ZoneKind kind, EffectElement element,
                                                              EffectFamily family)
    {
        EffectVocabulary vocabulary = RenderTestAssets.LoadEffectVocabulary();

        EffectRecipe recipe = EffectComposer.Area(vocabulary, kind);

        Assert.AreEqual(element, recipe.element);
        Assert.AreEqual(family, recipe.family);
        Assert.AreEqual(vocabulary.GetEntry(element).cycleSeconds, recipe.cycleSeconds);
    }

    [Test]
    public void Link_Heal_BeamsInTheHealAccent()
    {
        EffectVocabulary vocabulary = RenderTestAssets.LoadEffectVocabulary();

        EffectRecipe recipe = EffectComposer.Link(vocabulary, EffectFamily.Heal);

        Assert.AreEqual(EffectElement.Beam, recipe.element);
        Assert.AreEqual(vocabulary.palette.heal, recipe.colour);
    }

    [Test]
    public void Shield_Charges_ShowsOnePlatePerCharge()
    {
        EffectRecipe recipe = EffectComposer.Shield(RenderTestAssets.LoadEffectVocabulary(), 3f);

        Assert.AreEqual(EffectElement.Plates, recipe.element);
        Assert.AreEqual(3, recipe.count);
    }
}

}
