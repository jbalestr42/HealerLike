using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;

namespace CharacterSkills
{

public class FakeCharacterSkill : ACharacterSkill<CharacterSkillData>
{
    public int useCount = 0;

    public override void Use(GameObject source, UnityAction<bool> onSkillComplete)
    {
        useCount++;
        onSkillComplete(true);
    }
}

public class BlockingValidator : ACharacterSkillValidator<int>
{
    public override bool IsValid(GameObject owner) => false;
    public override void OnSkillUsed(GameObject owner) { }
}

public class BlockingValidatorFactory : CharacterSkillValidatorFactory<BlockingValidator, int> { }

public class CharacterSkillSlotTests
{
    GameObject _owner;
    GameObject _buttonGo;
    CharacterSkillSlot _slot;
    UseCharacterSkillButton _skillButton;
    FakeCharacterSkill _skill;
    BlockingValidatorFactory _validatorFactory;

    [SetUp]
    public void SetUp()
    {
        _owner = new GameObject("Character");
        _slot = _owner.AddComponent<CharacterSkillSlot>();

        _buttonGo = new GameObject("SkillButton");
        _skillButton = _buttonGo.AddComponent<UseCharacterSkillButton>();
        TestHelpers.SetPrivateField(_skillButton, "_button", _buttonGo.AddComponent<UnityEngine.UI.Button>());
        TestHelpers.SetPrivateField(_skillButton, "_nameText", _buttonGo.AddComponent<UnityEngine.UI.Text>());

        _validatorFactory = ScriptableObject.CreateInstance<BlockingValidatorFactory>();
        _skill = new FakeCharacterSkill { data = new CharacterSkillData { name = "Skill", validators = new List<ACharacterSkillValidatorFactory> { _validatorFactory } } };
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_owner);
        Object.DestroyImmediate(_buttonGo);
        Object.DestroyImmediate(_validatorFactory);
    }

    [Test]
    public void UseSkill_WithValidators_IsBlockedByAFailingValidator()
    {
        _slot.Init(_skill, _skillButton);

        _slot.UseSkill();

        Assert.AreEqual(0, _skill.useCount);
    }

    [Test]
    public void UseSkill_WithoutValidators_IgnoresThem()
    {
        _slot.Init(_skill, _skillButton, useValidators: false);

        _slot.UseSkill();
        _slot.UseSkill();

        Assert.AreEqual(2, _skill.useCount);
        Assert.IsFalse(_skillButton.hasCooldown);
        Assert.IsFalse(_skillButton.hasCost);
    }
}

}
