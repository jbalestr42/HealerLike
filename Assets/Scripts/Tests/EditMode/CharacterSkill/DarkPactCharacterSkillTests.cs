using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace CharacterSkills
{

// Dark Pact: every ally sacrifices part of its max health, turned into mana
public class DarkPactCharacterSkillTests
{
    readonly List<GameObject> _objects = new List<GameObject>();
    ResourceAttribute _mana;

    [SetUp]
    public void SetUp()
    {
        _mana = CreateResource(AttributeType.ManaMax, 10f, 100f);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in _objects)
        {
            Object.DestroyImmediate(go);
        }
        _objects.Clear();
    }

    ResourceAttribute CreateResource(AttributeType maxType, float value, float max)
    {
        GameObject go = new GameObject(maxType.ToString());
        _objects.Add(go);
        ResourceAttribute resource = TestHelpers.CreateResourceAttribute(go, maxType, max);
        resource.SetValue(value);
        return resource;
    }

    [Test]
    public void Pact_EveryAllyLosesThePercentOfItsMaxHealth_TurnedIntoMana()
    {
        ResourceAttribute full = CreateResource(AttributeType.HealthMax, 100f, 100f);
        ResourceAttribute tank = CreateResource(AttributeType.HealthMax, 150f, 200f);

        float sacrificed = DarkPactCharacterSkill.Pact(new List<ResourceAttribute> { full, tank }, _mana, 0.15f, 0.5f);

        Assert.AreEqual(85f, full.Value, 0.0001f);
        Assert.AreEqual(120f, tank.Value, 0.0001f);
        Assert.AreEqual(45f, sacrificed, 0.0001f);
        // 10 + 45 x 0.5
        Assert.AreEqual(32.5f, _mana.Value, 0.0001f);
    }

    [Test]
    public void Pact_NeverKills_AWoundedAllyKeeps1Health()
    {
        ResourceAttribute dying = CreateResource(AttributeType.HealthMax, 5f, 100f);

        float sacrificed = DarkPactCharacterSkill.Pact(new List<ResourceAttribute> { dying }, _mana, 0.15f, 0.5f);

        Assert.AreEqual(1f, dying.Value, 0.0001f);
        Assert.AreEqual(4f, sacrificed, 0.0001f);
    }

    [Test]
    public void Pact_IgnoresInvincibilityAndSkipsTheDead()
    {
        ResourceAttribute invincible = CreateResource(AttributeType.HealthMax, 100f, 100f);
        invincible.preventConsumers = true;
        ResourceAttribute dead = CreateResource(AttributeType.HealthMax, 0f, 100f);

        DarkPactCharacterSkill.Pact(new List<ResourceAttribute> { invincible, dead }, _mana, 0.15f, 0.5f);

        Assert.AreEqual(85f, invincible.Value, 0.0001f);
        Assert.AreEqual(0f, dead.Value, 0.0001f);
    }

    [Test]
    public void Pact_ManaIsCappedAtItsMax()
    {
        ResourceAttribute tank = CreateResource(AttributeType.HealthMax, 1000f, 1000f);

        DarkPactCharacterSkill.Pact(new List<ResourceAttribute> { tank }, _mana, 0.15f, 1f);

        Assert.AreEqual(100f, _mana.Value, 0.0001f);
    }
}

}
