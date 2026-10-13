using Entities;
using NUnit.Framework;
using UnityEngine;

namespace CharacterSkills
{

// Judgment: spends all the Devotion once, then heals every ally, +0.05 to the multiplier per Devotion spent
public class JudgmentCharacterSkillTests
{
    readonly TestUnits _units = new TestUnits();
    GameObject _characterGo;
    ResourceAttribute _devotion;
    ConsumerFactory _heal;
    JudgmentCharacterSkill _skill;

    [SetUp]
    public void SetUp()
    {
        _characterGo = new GameObject("Character");
        _devotion = TestHelpers.CreateResourceAttribute(_characterGo, ResourceType.Devotion, AttributeType.DevotionMax, 5f);

        // Heals 100, never critical
        _heal = ScriptableObject.CreateInstance<ConsumerFactory>();
        _heal.data = new ConsumerData { canBeCritical = false, value = new FlatValue { data = new FlatValueData { value = -100f } } };
        _skill = new JudgmentCharacterSkill { data = new JudgmentCharacterSkillData { consumer = _heal, multiplier = 0.2f, multiplierPerSpent = 0.05f, spentResource = ResourceType.Devotion } };
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        Object.DestroyImmediate(_characterGo);
        Object.DestroyImmediate(_heal);
    }

    [Test]
    public void Spend_EmptiesTheDevotion_AndRaisesTheMultiplierPerDevotion()
    {
        _devotion.SetValue(5f);

        Assert.AreEqual(5, _skill.Spend(_characterGo));
        TestHelpers.InvokePrivate(_devotion, "Update");

        // 0.2 + 5 x 0.05
        Assert.AreEqual(0.45f, _skill.GetMultiplier(), 0.0001f);
        Assert.AreEqual(0f, _devotion.Value, 0.001f);
    }

    [Test]
    public void EveryTarget_IsHealedWithTheDevotionReadOnce()
    {
        Entity first = _units.Create(10f, 100f, "First");
        Entity second = _units.Create(10f, 100f, "Second");
        _devotion.SetValue(5f);

        _skill.Spend(_characterGo);
        // Emptied before the targets are healed: they still get the bonus read when used
        TestHelpers.InvokePrivate(_devotion, "Update");
        _skill.ApplyOnTarget(_characterGo, first.gameObject);
        _skill.ApplyOnTarget(_characterGo, second.gameObject);
        TestUnits.Process(first.health);
        TestUnits.Process(second.health);

        Assert.AreEqual(55f, first.health.Value, 0.01f);
        Assert.AreEqual(55f, second.health.Value, 0.01f);
    }

    [Test]
    public void NoDevotion_KeepsTheBaseMultiplier()
    {
        _devotion.SetValue(0f);

        _skill.Spend(_characterGo);

        Assert.AreEqual(0.2f, _skill.GetMultiplier(), 0.0001f);
    }
}

}
