using NUnit.Framework;
using UnityEngine;

namespace CharacterSkills
{

// Raise Dead: the summon gets stronger with the Heal Power, and only a few can be alive at once
public class RaiseDeadCharacterSkillTests
{
    GameObject _summon;

    [SetUp]
    public void SetUp()
    {
        _summon = new GameObject("Skeleton");
    }

    [TearDown]
    public void TearDown()
    {
        if (_summon != null)
        {
            Object.DestroyImmediate(_summon);
        }
    }

    [Test]
    public void Empower_AddsHealthAndDamagePerHealPower()
    {
        ResourceAttribute health = TestHelpers.CreateResourceAttribute(_summon, AttributeType.HealthMax, 50f);
        AttributeManager attributes = _summon.GetComponent<AttributeManager>();
        attributes.Add(AttributeType.Damage, new Attribute(4f));
        RaiseDeadCharacterSkillData data = new RaiseDeadCharacterSkillData { healthPerHealPower = 3f, damagePerHealPower = 0.1f };

        RaiseDeadCharacterSkill.Empower(attributes, 15f, data);

        Assert.AreEqual(95f, attributes.Get(AttributeType.HealthMax).Value, 0.0001f);
        Assert.AreEqual(5.5f, attributes.Get(AttributeType.Damage).Value, 0.0001f);
        // The summon starts with its full, empowered health
        TestHelpers.InvokePrivate(health, "Update");
        Assert.AreEqual(95f, health.Value, 0.0001f);
    }

    [Test]
    public void Empower_WithoutHealPower_KeepsTheBaseStats()
    {
        AttributeManager attributes = TestHelpers.CreateAttributeManager(_summon, AttributeType.HealthMax, 50f);
        RaiseDeadCharacterSkillData data = new RaiseDeadCharacterSkillData();

        RaiseDeadCharacterSkill.Empower(attributes, 0f, data);

        Assert.AreEqual(50f, attributes.Get(AttributeType.HealthMax).Value, 0.0001f);
    }

    [Test]
    public void AliveCount_OnlyCountsTheLivingSummons()
    {
        RaiseDeadCharacterSkill skill = new RaiseDeadCharacterSkill { data = new RaiseDeadCharacterSkillData { maxAlive = 2 } };
        GameObject other = new GameObject("Other Skeleton");
        skill.AddSummon(_summon);
        skill.AddSummon(other);
        Assert.AreEqual(2, skill.aliveCount);

        Object.DestroyImmediate(other);

        Assert.AreEqual(1, skill.aliveCount);
    }
}

}
