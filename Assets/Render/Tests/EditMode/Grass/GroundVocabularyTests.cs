using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Grass
{

public class GroundVocabularyTests
{
    GroundVocabulary _vocabulary;

    [SetUp]
    public void SetUp()
    {
        _vocabulary = GroundVocabulary.CreateDefault();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_vocabulary);
    }

    [Test]
    public void ForZone_TheTuftsZones_MoveTheGrassThroughTheirEffects()
    {
        Assert.AreSame(_vocabulary.heal, _vocabulary.ForZone(ZoneKind.Heal));
        Assert.AreSame(_vocabulary.range, _vocabulary.ForZone(ZoneKind.Range));
        Assert.IsNull(_vocabulary.ForZone(ZoneKind.Hostile));
        Assert.IsNull(_vocabulary.ForZone(ZoneKind.Bruise));
    }

    [Test]
    public void Defaults_KeepTheTunedLook()
    {
        Assert.AreEqual(GroundShape.Line, _vocabulary.launch.shape);
        Assert.AreEqual(110f, _vocabulary.launch.kick);
        Assert.AreEqual(GroundShape.Ring, _vocabulary.hit.shape);
        Assert.AreEqual(170f, _vocabulary.hit.kick);
        Assert.AreEqual(0.35f, _vocabulary.hit.grow);
        Assert.AreEqual(1f, _vocabulary.obstacle.flatten);
        Assert.AreEqual(Mathf.PI * 0.5f, _vocabulary.heal.kickTurn, 1e-6f, "A heal spins the grass.");
        Assert.AreEqual(1f, _vocabulary.ash.ash);
        Assert.AreEqual(-1f, _vocabulary.wilt.vitality);
        Assert.AreEqual(1f, _vocabulary.blight.blight);
        Assert.AreEqual(-1f, _vocabulary.frost.light);
        Assert.AreEqual(GroundShape.Line, _vocabulary.scorch.shape);
        Assert.AreEqual(5f, _vocabulary.warning.shiver);
    }

    [Test]
    public void Defaults_EveryEffectIsSet()
    {
        GroundEffect[] effects =
        {
            _vocabulary.heal, _vocabulary.range, _vocabulary.obstacle, _vocabulary.launch, _vocabulary.hit,
            _vocabulary.footRing, _vocabulary.bodyRing, _vocabulary.ash, _vocabulary.wilt, _vocabulary.boost,
            _vocabulary.blight, _vocabulary.frost, _vocabulary.scorch, _vocabulary.warning
        };

        foreach (GroundEffect effect in effects)
        {
            Assert.IsNotNull(effect);
        }

        Assert.AreNotSame(_vocabulary.hit, _vocabulary.footRing, "Each is tuned on its own.");
    }

    [Test]
    public void Ground_WithoutVocabulary_UsesTheDefaults()
    {
        Ground ground = new Ground();

        Assert.IsNotNull(ground.vocabulary);
        Assert.AreEqual(170f, ground.vocabulary.hit.kick);
    }
}

}
