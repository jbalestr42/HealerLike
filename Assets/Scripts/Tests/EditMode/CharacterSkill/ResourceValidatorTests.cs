using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CharacterSkills
{

// A skill costs mana, the SkillCostMultiplier of the character lowering or raising the cost
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
        _mana = TestHelpers.CreateResourceAttribute(_characterGo, AttributeType.ManaMax, 100f);

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
}

}
