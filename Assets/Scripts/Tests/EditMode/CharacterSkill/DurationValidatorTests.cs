using NUnit.Framework;
using UnityEngine;

namespace CharacterSkills
{

// The cooldown of a character skill, shortened by the SkillCooldownMultiplier of the character (Prayer Beads)
public class DurationValidatorTests
{
    GameObject _owner;
    DurationValidator _validator;

    [SetUp]
    public void SetUp()
    {
        _owner = new GameObject("Character");
        _validator = new DurationValidator { data = new DurationValidatorData { duration = 10f } };
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_owner);
    }

    [Test]
    public void GetDuration_WithoutSkillCooldown_IsTheDataDuration()
    {
        TestHelpers.CreateAttributeManager(_owner);

        Assert.AreEqual(10f, _validator.GetDuration(_owner), 0.0001f);
    }

    [Test]
    public void GetDuration_WithoutAttributes_IsTheDataDuration()
    {
        Assert.AreEqual(10f, _validator.GetDuration(_owner), 0.0001f);
        Assert.AreEqual(10f, _validator.GetDuration(null), 0.0001f);
    }

    [Test]
    public void GetDuration_IsMultipliedByTheSkillCooldown()
    {
        TestHelpers.CreateAttributeManager(_owner, AttributeType.SkillCooldownMultiplier, 0.85f);

        Assert.AreEqual(8.5f, _validator.GetDuration(_owner), 0.0001f);
    }

    [Test]
    public void OnSkillUsed_TheSkillIsOnCooldown()
    {
        TestHelpers.CreateAttributeManager(_owner, AttributeType.SkillCooldownMultiplier, 0.85f);

        _validator.OnSkillUsed(_owner);

        Assert.IsFalse(_validator.IsValid(_owner));
    }

    [Test]
    public void Init_TheSkillShowsACooldown_ReadyAtFirst()
    {
        CharacterSkillState state = new CharacterSkillState();

        _validator.Init(state, _owner);
        _validator.Update(state);

        Assert.IsTrue(state.hasCooldown);
        Assert.AreEqual(0f, state.cooldown, 0.0001f);
        Assert.AreEqual(0f, state.cooldownProgress, 0.0001f);
    }

    [Test]
    public void Update_AfterAUse_TheWholeCooldownIsLeft()
    {
        CharacterSkillState state = new CharacterSkillState();
        _validator.Init(state, _owner);

        _validator.OnSkillUsed(_owner);
        _validator.Update(state);

        // Same frame: no game time has passed since the use
        Assert.AreEqual(10f, state.cooldown, 0.0001f);
        Assert.AreEqual(10f, state.cooldownDuration, 0.0001f);
        Assert.AreEqual(1f, state.cooldownProgress, 0.0001f);
    }
}

}
