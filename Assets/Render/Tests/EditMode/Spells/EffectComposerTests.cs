using HealerLike.Render.Grammar;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Spells
{

public class EffectComposerTests
{
    static EffectVocabulary LoadVocabulary()
    {
        return AssetDatabase.LoadAssetAtPath<EffectVocabulary>("Assets/Render/Spells/Data/EffectVocabulary.asset");
    }

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
        EffectVocabulary vocabulary = LoadVocabulary();

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

        EffectRecipe recipe = EffectComposer.Compose(LoadVocabulary(), channels, 1, 0f);

        Assert.AreEqual(1.5f, recipe.cycleSeconds);
    }

    [Test]
    public void Compose_Heal_TakesTheLimeAccent()
    {
        EffectVocabulary vocabulary = LoadVocabulary();

        EffectChannels channels = Channels(EffectFamily.Heal, AttributeGroup.Offence);

        EffectRecipe recipe = EffectComposer.Compose(vocabulary, channels, 1, 0f);

        Assert.AreEqual(vocabulary.palette.heal, recipe.colour);
    }

    [Test]
    public void Compose_Mana_TakesTheManaColour()
    {
        EffectVocabulary vocabulary = LoadVocabulary();

        EffectRecipe recipe = EffectComposer.Compose(vocabulary, EffectElement.ManaUp, EffectFamily.Heal,
                                                     EffectTempo.Once, 0f, 1, 0f, 0f);

        Assert.AreEqual(vocabulary.palette.mana, recipe.colour);
    }

    [TestCase(1, 2)]
    [TestCase(2, 3)]
    [TestCase(9, 3)]
    public void Count_OrbitStacks_TwoOrThreeTori(int stacks, int expected)
    {
        ElementEntry orbit = LoadVocabulary().GetEntry(EffectElement.Orbit);

        Assert.AreEqual(expected, EffectComposer.Count(orbit, stacks, 0f, 0f));
    }

    [TestCase(1f, 1)]
    [TestCase(2f, 2)]
    [TestCase(2.5f, 3)]
    public void Count_PlateCharges_OnePlatePerCharge(float charges, int expected)
    {
        ElementEntry plates = LoadVocabulary().GetEntry(EffectElement.Plates);

        Assert.AreEqual(expected, EffectComposer.Count(plates, 1, charges, 0f));
    }

    [Test]
    public void Compose_NoVocabulary_ReturnsNull()
    {
        Assert.IsNull(EffectComposer.Compose(null, Channels(EffectFamily.Heal, AttributeGroup.Offence), 1, 0f));
    }
}

}
