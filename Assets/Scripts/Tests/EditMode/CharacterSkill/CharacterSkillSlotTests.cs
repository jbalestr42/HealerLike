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
        TestHelpers.SetPrivateField(_skillButton, "_nameText", _buttonGo.AddComponent<TMPro.TextMeshProUGUI>());

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

    [Test]
    public void UseSkill_Disabled_IsBlockedEvenWithoutValidators()
    {
        _slot.Init(_skill, _skillButton, useValidators: false);
        _slot.isEnabled = false;

        _slot.UseSkill();

        Assert.IsFalse(_slot.CanUseSkill());
        Assert.AreEqual(0, _skill.useCount);
    }

    [Test]
    public void UseSkill_EnabledAgain_CanBeUsed()
    {
        _slot.Init(_skill, _skillButton, useValidators: false);
        _slot.isEnabled = false;
        _slot.isEnabled = true;

        _slot.UseSkill();

        Assert.AreEqual(1, _skill.useCount);
    }

    // Counted by the items reacting to each spell (e.g. the Rosary)
    [Test]
    public void UseSkill_Used_TellsTheCharacter()
    {
        Character character = null;
        TestHelpers.WithLoggingDisabled(() => character = _owner.AddComponent<Character>());
        List<CharacterSkillData> used = new List<CharacterSkillData>();
        character.OnSkillUsed.AddListener(used.Add);
        _slot.Init(_skill, _skillButton, useValidators: false);

        _slot.UseSkill();

        CollectionAssert.AreEqual(new[] { _skill.data }, used);
    }

    [Test]
    public void UseSkill_Blocked_DoesNotTellTheCharacter()
    {
        Character character = null;
        TestHelpers.WithLoggingDisabled(() => character = _owner.AddComponent<Character>());
        int used = 0;
        character.OnSkillUsed.AddListener(skill => used++);
        _slot.Init(_skill, _skillButton);

        _slot.UseSkill();

        Assert.AreEqual(0, used);
    }

    // A validator of the resource of the character costing cost (a gain when negative), tracked for the tear down
    ResourceValidatorFactory CreateResourceValidator(ResourceType resourceType, float cost, List<Object> created)
    {
        ConsumerFactory consumer = ScriptableObject.CreateInstance<ConsumerFactory>();
        consumer.data = new ConsumerData
        {
            ignoreDamageReduction = true,
            ignoreConsumerPrevention = true,
            canBeCritical = false,
            value = new FlatValue { data = new FlatValueData { value = cost } },
        };
        ResourceValidatorFactory factory = ScriptableObject.CreateInstance<ResourceValidatorFactory>();
        factory.data = new ResourceValidatorData { resourceType = resourceType, consumer = consumer };
        created.Add(consumer);
        created.Add(factory);
        return factory;
    }

    // The Heal of the Cleric in the sandbox: free, but giving its Devotion as in a run
    [Test]
    public void UseSkill_WithoutValidators_StillGivesItsGains()
    {
        List<Object> created = new List<Object>();
        try
        {
            ResourceAttribute mana = TestHelpers.CreateResourceAttribute(_owner, ResourceType.Mana, AttributeType.ManaMax, 100f);
            mana.SetValue(0f);
            // On the same attributes as the mana, as on the character
            ResourceAttribute devotion = null;
            TestHelpers.WithLoggingDisabled(() =>
            {
                _owner.GetComponent<AttributeManager>().Add(AttributeType.DevotionMax, new Attribute(5f));
                devotion = _owner.AddComponent<ResourceAttribute>();
                devotion.Init(ResourceType.Devotion, AttributeType.DevotionMax, new RawResourceConsumerResolver());
            });
            devotion.SetValue(0f);
            _skill.data.validators = new List<ACharacterSkillValidatorFactory>
            {
                CreateResourceValidator(ResourceType.Mana, 20f, created),
                CreateResourceValidator(ResourceType.Devotion, -1f, created),
            };
            _slot.Init(_skill, _skillButton, useValidators: false);

            _slot.UseSkill();
            TestHelpers.InvokePrivate(devotion, "Update");
            TestHelpers.InvokePrivate(mana, "Update");

            Assert.AreEqual(1, _skill.useCount, "Used without the mana it costs");
            Assert.AreEqual(1f, devotion.Value, 0.001f);
            Assert.AreEqual(0f, mana.Value, 0.001f);
            Assert.IsFalse(_skillButton.hasCost);
        }
        finally
        {
            foreach (Object obj in created)
            {
                Object.DestroyImmediate(obj);
            }
        }
    }
}

}
