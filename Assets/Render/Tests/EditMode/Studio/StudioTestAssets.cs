using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Studio
{

// The fixtures more than one studio test class builds on
public static class StudioTestAssets
{
    // A handler of the game data that derives a held debuff on the other side
    public static readonly string OpposingHandlerPath =
        "Assets/Data/CharacterSkills/MultiTargetReduceDamage/BuffHandlerFactory.asset";

    // Four shapes counted by amount, one bead, one critical ring, one rim, over 0.75 seconds
    public static ElementEntry CreateRise()
    {
        ElementEntry rise = new ElementEntry();
        rise.parts = new LookPart[] { Part("First"), Part("Second"), Part("Third"), Part("Fourth") };
        rise.stackBeads = new LookPart[] { Part("Bead") };
        rise.criticalRings = new LookPart[] { Part("Critical") };
        rise.sideRim = new LookPart[] { Part("Side") };
        rise.motion = EffectMotionKind.Rise;
        rise.socket = EffectSocket.Feet;
        rise.count = EffectCount.Amount;
        rise.minCount = 1;
        rise.cycleSeconds = 0.75f;
        return rise;
    }

    public static LookPart Part(string id)
    {
        return new LookPart { id = id, size = Vector3.one, colour = ColourRole.Accent };
    }

    public static IEnumerable<object[]> ChannelCases()
    {
        foreach (EffectFamily family in Enum.GetValues(typeof(EffectFamily)))
        {
            foreach (AttributeGroup group in Enum.GetValues(typeof(AttributeGroup)))
            {
                foreach (EffectTempo tempo in Enum.GetValues(typeof(EffectTempo)))
                {
                    yield return new object[] { family, group, tempo };
                }
            }
        }
    }

    public static void AssertRecipe(EffectRecipe expected, EffectRecipe actual)
    {
        Assert.NotNull(actual);
        Assert.AreEqual(expected.element, actual.element);
        Assert.AreEqual(expected.family, actual.family);
        Assert.AreEqual(expected.tempo, actual.tempo);
        Assert.AreEqual(expected.cycleSeconds, actual.cycleSeconds);
        Assert.AreEqual(expected.colour, actual.colour);
        Assert.AreEqual(expected.count, actual.count);
        Assert.AreEqual(expected.motion, actual.motion);
        Assert.AreEqual(expected.socket, actual.socket);
    }
}

}
