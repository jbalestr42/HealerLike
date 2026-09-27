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

    [TestCase(EffectFamily.Damage, AttributeGroup.Offence, EffectKey.Burst)]
    [TestCase(EffectFamily.Heal, AttributeGroup.Offence, EffectKey.Rise)]
    [TestCase(EffectFamily.Rot, AttributeGroup.Offence, EffectKey.Drips)]
    [TestCase(EffectFamily.Renew, AttributeGroup.Offence, EffectKey.Stalks)]
    [TestCase(EffectFamily.Boon, AttributeGroup.Offence, EffectKey.Orbit)]
    [TestCase(EffectFamily.Boon, AttributeGroup.Defence, EffectKey.Plates)]
    [TestCase(EffectFamily.Boon, AttributeGroup.Prevention, EffectKey.Bud)]
    [TestCase(EffectFamily.Bane, AttributeGroup.Offence, EffectKey.Press)]
    [TestCase(EffectFamily.Bane, AttributeGroup.Defence, EffectKey.Crack)]
    public void Element_FamilyAndGroup_PicksTheTableElement(EffectFamily family, AttributeGroup group,
                                                            EffectKey expected)
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

    [TestCase(true, EffectKey.ManaUp)]
    [TestCase(false, EffectKey.ManaDown)]
    public void Mana_Sign_PicksUpOrDown(bool isGain, EffectKey expected)
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

        EffectRecipe recipe = EffectComposer.Compose(vocabulary, EffectKey.ManaUp, EffectFamily.Heal,
                                                     EffectTempo.Once, 0f, 1, 0f, 0f);

        Assert.AreEqual(vocabulary.palette.mana, recipe.colour);
    }

    [TestCase(1, 2)]
    [TestCase(2, 2)]
    [TestCase(9, 2)]
    public void Count_OrbitStacks_KeepsTwoReadableRingsWhileBeadsShowStacks(int stacks, int expected)
    {
        ElementEntry orbit = RenderTestAssets.LoadEffectVocabulary().GetEntry(EffectKey.Orbit);

        Assert.AreEqual(expected, EffectComposer.Count(orbit, stacks, 0f, 0f));
    }

    [TestCase(1f, 1)]
    [TestCase(2f, 2)]
    [TestCase(2.5f, 3)]
    public void Count_PlateCharges_OnePlatePerCharge(float charges, int expected)
    {
        ElementEntry plates = RenderTestAssets.LoadEffectVocabulary().GetEntry(EffectKey.Plates);

        Assert.AreEqual(expected, EffectComposer.Count(plates, 1, charges, 0f));
    }

    [Test]
    public void Compose_NoVocabulary_ReturnsNull()
    {
        Assert.IsNull(EffectComposer.Compose(null, Channels(EffectFamily.Heal, AttributeGroup.Offence), 1, 0f));
    }

    [TestCase(ResourceKind.Health, true, EffectKey.Rise, EffectFamily.Heal)]
    [TestCase(ResourceKind.Health, false, EffectKey.Burst, EffectFamily.Damage)]
    [TestCase(ResourceKind.Mana, true, EffectKey.ManaUp, EffectFamily.Heal)]
    [TestCase(ResourceKind.Mana, false, EffectKey.ManaDown, EffectFamily.Damage)]
    public void Impact_ResourceAndSign_PicksTheElementAndFamily(ResourceKind resource, bool isGain,
                                                               EffectKey element, EffectFamily family)
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

        Assert.AreEqual(EffectComposer.BurstScaleMin * vocabulary.GetEntry(EffectKey.Burst).presentation.scale, light.scale);
        Assert.AreEqual(EffectComposer.BurstScaleMax * vocabulary.GetEntry(EffectKey.Burst).presentation.scale, full.scale);
        Assert.AreEqual(vocabulary.GetEntry(EffectKey.Rise).presentation.scale,
            EffectComposer.Impact(vocabulary, ResourceKind.Health, true, 1f).scale);
    }

    [TestCase(ZoneKind.Heal, EffectKey.Ring, EffectFamily.Heal)]
    [TestCase(ZoneKind.Hostile, EffectKey.Litter, EffectFamily.Bane)]
    public void Area_Kind_ComposesTheFootprintForTheEntryCycle(ZoneKind kind, EffectKey element,
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

        Assert.AreEqual(EffectKey.Beam, recipe.element);
        Assert.AreEqual(vocabulary.palette.heal, recipe.colour);
    }

    [Test]
    public void Shield_Charges_ShowsOnePlatePerCharge()
    {
        EffectRecipe recipe = EffectComposer.Shield(RenderTestAssets.LoadEffectVocabulary(), 3f);

        Assert.AreEqual(EffectKey.Plates, recipe.element);
        Assert.AreEqual(3, recipe.count);
    }
}

}
