using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CharacterSkills
{

// A skill costs mana, the SkillCostMultiplier of the character lowering or raising the cost, or another resource
// of the character (e.g. a Heal giving Devotion with a negative cost)
public class ResourceValidatorTests
{
    GameObject _characterGo;
    ResourceAttribute _mana;
    ConsumerFactory _cost;
    ResourceValidator _validator;

    [SetUp]
    public void SetUp()
    {
        _characterGo = new GameObject();
        _mana = TestHelpers.CreateResourceAttribute(_characterGo, ResourceType.Mana, AttributeType.ManaMax, 100f);

        // 20 mana, never reduced nor critical
        _cost = ScriptableObject.CreateInstance<ConsumerFactory>();
        _cost.data = new ConsumerData
        {
            ignoreDamageReduction = true,
            ignoreConsumerPrevention = true,
            canBeCritical = false,
            value = new FlatValue { data = new FlatValueData { value = 20f } },
        };

        _validator = new ResourceValidator { data = new ResourceValidatorData { consumer = _cost } };
        TestHelpers.SetPrivateField(_validator, "resource", _mana);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_characterGo);
        Object.DestroyImmediate(_cost);
    }

    void SetCostMultiplier(float multiplier)
    {
        _characterGo.GetComponent<AttributeManager>().Add(AttributeType.SkillCostMultiplier, new Attribute(multiplier));
    }

    void SetMana(float mana)
    {
        TestHelpers.SetPrivateField(_mana, "_value", mana);
    }

    void UseSkill()
    {
        _validator.OnSkillUsed(_characterGo);
        TestHelpers.InvokePrivate(_mana, "Update");
    }

    [Test]
    public void DefaultValue_LeavesTheCostUntouched()
    {
        Assert.AreEqual(1f, AttributeManager.GetDefaultValue(AttributeType.SkillCostMultiplier));
    }

    [Test]
    public void GetCost_WithoutMultiplier_IsTheConsumerValue()
    {
        Assert.AreEqual(20f, _validator.GetCost(_characterGo), 0.001f);
        Assert.AreEqual(1f, ResourceValidator.GetCostMultiplier(null));
    }

    [Test]
    public void GetCost_MultiplierLowersTheCost()
    {
        // e.g. -20% spell cost
        SetCostMultiplier(0.8f);

        Assert.AreEqual(16f, _validator.GetCost(_characterGo), 0.001f);
    }

    [Test]
    public void GetCost_MultiplierRaisesTheCost()
    {
        // e.g. the +20% spell cost curse
        SetCostMultiplier(1.2f);

        Assert.AreEqual(24f, _validator.GetCost(_characterGo), 0.001f);
    }

    [Test]
    public void GetCost_NegativeMultiplier_IsFreeNotAGain()
    {
        SetCostMultiplier(-0.4f);

        Assert.AreEqual(0f, _validator.GetCost(_characterGo), 0.001f);
    }

    [Test]
    public void IsValid_ChecksTheReducedCost()
    {
        SetCostMultiplier(0.8f);
        SetMana(16f);

        Assert.IsTrue(_validator.IsValid(_characterGo));

        SetMana(15.9f);

        Assert.IsFalse(_validator.IsValid(_characterGo));
    }

    [Test]
    public void IsValid_ChecksTheRaisedCost()
    {
        SetCostMultiplier(1.2f);
        SetMana(20f);

        Assert.IsFalse(_validator.IsValid(_characterGo));
    }

    [Test]
    public void OnSkillUsed_WithoutMultiplier_ConsumesTheWholeCost()
    {
        SetMana(100f);

        UseSkill();

        Assert.AreEqual(80f, _mana.Value, 0.001f);
    }

    [Test]
    public void OnSkillUsed_ConsumesTheReducedCost()
    {
        SetCostMultiplier(0.8f);
        SetMana(100f);

        UseSkill();

        Assert.AreEqual(84f, _mana.Value, 0.001f);
    }

    [Test]
    public void OnSkillUsed_ConsumesTheRaisedCost()
    {
        SetCostMultiplier(1.2f);
        SetMana(100f);

        UseSkill();

        Assert.AreEqual(76f, _mana.Value, 0.001f);
    }

    [Test]
    public void EverySkillCost_IsNeverCritical()
    {
        // A critical would make the character pay twice the cost
        string[] guids = AssetDatabase.FindAssets("t:ResourceValidatorFactory", new[] { "Assets/Data" });
        Assert.IsNotEmpty(guids);
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ResourceValidatorFactory validator = AssetDatabase.LoadAssetAtPath<ResourceValidatorFactory>(path);
            Assert.IsNotNull(validator.data.consumer, path);
            Assert.IsFalse(validator.data.consumer.data.canBeCritical, path);
        }
    }

    [Test]
    public void OnSkillUsed_CriticalCharacter_PaysTheCostOnce()
    {
        AttributeManager attributes = _characterGo.GetComponent<AttributeManager>();
        attributes.Add(AttributeType.CriticalChance, new Attribute(100f));
        attributes.Add(AttributeType.CriticalMultiplier, new Attribute(2f));
        SetMana(100f);

        UseSkill();

        Assert.AreEqual(80f, _mana.Value, 0.001f);
    }

    // The Devotion of the character: up to 5, empty, and a validator of the skill on it costing cost
    (ResourceAttribute devotion, ResourceValidator validator) CreateDevotionValidator(float cost)
    {
        ResourceAttribute devotion = null;
        TestHelpers.WithLoggingDisabled(() =>
        {
            _characterGo.GetComponent<AttributeManager>().Add(AttributeType.DevotionMax, new Attribute(5f));
            devotion = _characterGo.AddComponent<ResourceAttribute>();
            devotion.Init(ResourceType.Devotion, AttributeType.DevotionMax);
        });
        devotion.SetValue(0f);

        _cost.data.value = new FlatValue { data = new FlatValueData { value = cost } };
        ResourceValidator validator = new ResourceValidator { data = new ResourceValidatorData { resourceType = ResourceType.Devotion, consumer = _cost } };
        TestHelpers.SetPrivateField(validator, "resource", devotion);
        return (devotion, validator);
    }

    [Test]
    public void NegativeDevotionCost_GivesDevotion_AndNeverBlocksTheSkill()
    {
        (ResourceAttribute devotion, ResourceValidator validator) = CreateDevotionValidator(-1f);

        Assert.IsTrue(validator.IsValid(_characterGo));
        validator.OnSkillUsed(_characterGo);
        TestHelpers.InvokePrivate(devotion, "Update");

        Assert.AreEqual(1f, devotion.Value, 0.001f);
        Assert.AreEqual(100f, _mana.Value, 0.001f, "The mana is untouched");
    }

    [Test]
    public void DevotionCost_NeedsThatMuchDevotion()
    {
        (ResourceAttribute devotion, ResourceValidator validator) = CreateDevotionValidator(3f);
        devotion.SetValue(2f);
        Assert.IsFalse(validator.IsValid(_characterGo));

        devotion.SetValue(3f);
        Assert.IsTrue(validator.IsValid(_characterGo));
    }

    [Test]
    public void WithoutCostMultiplier_TheCostIsUntouched()
    {
        (ResourceAttribute devotion, ResourceValidator validator) = CreateDevotionValidator(-1f);
        validator.data.useCostMultiplier = false;
        SetCostMultiplier(1.1f);

        Assert.AreEqual(-1f, validator.GetCost(_characterGo), 0.001f);
    }

    [Test]
    public void UseCostMultiplier_IsOnByDefault()
    {
        Assert.IsTrue(new ResourceValidatorData().useCostMultiplier);
    }

    // A skill button showing the cost given to it
    UseCharacterSkillButton CreateButton(GameObject go)
    {
        UseCharacterSkillButton button = go.AddComponent<UseCharacterSkillButton>();
        TestHelpers.SetPrivateField(button, "_costText", go.AddComponent<TMPro.TextMeshProUGUI>());
        return button;
    }

    [Test]
    public void Init_TheFirstPositiveCost_IsShownOnTheButton()
    {
        GameObject buttonGo = new GameObject("Button");
        try
        {
            UseCharacterSkillButton button = CreateButton(buttonGo);
            (ResourceAttribute devotion, ResourceValidator gain) = CreateDevotionValidator(-1f);
            ConsumerFactory manaCost = ScriptableObject.CreateInstance<ConsumerFactory>();
            manaCost.data = new ConsumerData { value = new FlatValue { data = new FlatValueData { value = 8f } } };
            ResourceValidator mana = new ResourceValidator { data = new ResourceValidatorData { consumer = manaCost } };
            try
            {
                // The gain first: not a cost, the mana cost after it is shown
                gain.Init(button, _characterGo);
                mana.Init(button, _characterGo);

                Assert.IsTrue(button.hasCost);
                Assert.AreEqual("8", buttonGo.GetComponent<TMPro.TextMeshProUGUI>().text);
            }
            finally
            {
                Object.DestroyImmediate(manaCost);
            }
        }
        finally
        {
            Object.DestroyImmediate(buttonGo);
        }
    }

    [Test]
    public void Init_OnlyAGain_ShowsNoCost()
    {
        GameObject buttonGo = new GameObject("Button");
        try
        {
            UseCharacterSkillButton button = CreateButton(buttonGo);
            (ResourceAttribute devotion, ResourceValidator gain) = CreateDevotionValidator(-1f);

            gain.Init(button, _characterGo);

            Assert.IsFalse(button.hasCost);
        }
        finally
        {
            Object.DestroyImmediate(buttonGo);
        }
    }

    [Test]
    public void ACharacterWithoutTheResource_CantPayForIt()
    {
        ResourceValidator validator = new ResourceValidator { data = new ResourceValidatorData { resourceType = ResourceType.Devotion, consumer = _cost } };

        Assert.IsFalse(validator.IsValid(_characterGo));
        Assert.DoesNotThrow(() => validator.OnSkillUsed(_characterGo));
    }
}

}
