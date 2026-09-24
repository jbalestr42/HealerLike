using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Studio.Editor
{

public class RenderGrammarSummaryTests
{
    SpellLooks _looks;

    [SetUp]
    public void SetUp()
    {
        _looks = ScriptableObject.CreateInstance<SpellLooks>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_looks);
    }

    [Test]
    public void Describe_MissingTables_CountsThemEmpty()
    {
        _looks.buffs = null;
        _looks.projectiles = null;

        string summary = RenderGrammarSummary.Describe(_looks);

        StringAssert.StartsWith("0 buff overrides · 0 projectile overrides.", summary);
    }

    [Test]
    public void Count_NoTable_ReturnsZero()
    {
        Assert.AreEqual(0, RenderGrammarSummary.Count(null));
    }
}

}
