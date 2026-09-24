using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Studio.Editor
{

public class CreatureGrammarSamplesTests
{
    readonly List<Object> _objects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (Object trackedObject in _objects)
        {
            Object.DestroyImmediate(trackedObject);
        }
        _objects.Clear();
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    public void Build_EachSample_ComposesAValidRecipe(int index)
    {
        CreatureGrammarPreset preset = CreatureGrammarSamples.Build(index);
        _objects.Add(preset);

        CreatureRecipe recipe = preset.Compose();
        _objects.Add(recipe);

        string error;
        Assert.IsEmpty(CreatureGrammarValidator.Validate(preset));
        Assert.IsTrue(CreatureValidator.TryValidate(recipe, out error), error);
        Assert.AreEqual(CreatureGrammarSamples.Names[index], preset.displayName);
    }

    [TestCase(0, LookSide.Plant, HeadKind.Bud)]
    [TestCase(3, LookSide.Stone, HeadKind.Ward)]
    [TestCase(5, LookSide.Stone, HeadKind.Pulse)]
    public void Build_Sample_SitsOnItsSideWithItsHead(int index, LookSide side, HeadKind head)
    {
        CreatureGrammarPreset preset = CreatureGrammarSamples.Build(index);
        _objects.Add(preset);

        Assert.AreEqual(side, preset.side);
        Assert.AreEqual(head, preset.head);
    }

    [Test]
    public void Build_OutsideTheNames_LogsAndReturnsNull()
    {
        LogAssert.Expect(LogType.Error, "[CreatureGrammarSamples] No sample at 6");

        Assert.IsNull(CreatureGrammarSamples.Build(6));
    }
}

}
