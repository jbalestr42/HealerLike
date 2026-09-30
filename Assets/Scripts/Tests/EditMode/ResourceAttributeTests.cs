using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Attributes
{

public class ResourceAttributeTests
{
    class FakeConsumer : AConsumer
    {
        readonly float _value;
        readonly bool _ignoreDamageReduction;
        readonly bool _ignoreConsumerPrevention;

        public FakeConsumer(float value, bool ignoreDamageReduction = false, bool ignoreConsumerPrevention = false)
        {
            _value = value;
            _ignoreDamageReduction = ignoreDamageReduction;
            _ignoreConsumerPrevention = ignoreConsumerPrevention;
        }

        public override float GetValue() => _value;
        public override bool ignoreDamageReduction => _ignoreDamageReduction;
        public override bool ignoreConsumerPrevention => _ignoreConsumerPrevention;
    }

    GameObject _targetGo;
    GameObject _sourceGo;
    ResourceAttribute _health;
    AttributeManager _targetAttributeManager;

    [SetUp]
    public void SetUp()
    {
        _targetGo = new GameObject();
        _health = TestHelpers.CreateResourceAttribute(_targetGo, AttributeType.HealthMax, 100f);
        _targetAttributeManager = _targetGo.GetComponent<AttributeManager>();

        _sourceGo = new GameObject();
        TestHelpers.CreateAttributeManager(_sourceGo);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_targetGo);
        Object.DestroyImmediate(_sourceGo);
    }

    void Drain()
    {
        TestHelpers.InvokePrivate(_health, "Update");
    }

    ResourceModifier AddModifier(params AConsumer[] consumers)
    {
        ResourceModifier modifier = new ResourceModifier { source = _sourceGo };
        modifier.consumers.AddRange(consumers);
        _health.AddResourceModifier(modifier);
        return modifier;
    }

    [Test]
    public void Init_SetsValueToMax()
    {
        Assert.AreEqual(100f, _health.Value);
        Assert.AreEqual(100f, _health.Max);
    }

    [Test]
    public void Refill_AfterDamage_RestoresValueToMax()
    {
        AddModifier(new FakeConsumer(-60f));
        Drain();
        int changedCount = 0;
        _health.OnValueChanged.AddListener(_ => changedCount++);

        _health.Refill();
        Drain();

        Assert.AreEqual(100f, _health.Value);
        Assert.AreEqual(1, changedCount);
    }

    [Test]
    public void SetValue_SetsTheValueAndNotifiesOnTheNextUpdate()
    {
        int changedCount = 0;
        _health.OnValueChanged.AddListener(_ => changedCount++);

        _health.SetValue(40f);
        Drain();

        Assert.AreEqual(40f, _health.Value);
        Assert.AreEqual(1, changedCount);
    }

    [Test]
    public void SetValue_IsClampedBetweenZeroAndMax()
    {
        _health.SetValue(150f);
        Assert.AreEqual(100f, _health.Value);

        _health.SetValue(-10f);
        Assert.AreEqual(0f, _health.Value);
    }

    [Test]
    public void SetValue_IgnoresArmorAndInvincibility()
    {
        _targetAttributeManager.Get(AttributeType.PercentArmor).BaseValue = 0.5f;
        _health.preventConsumers = true;

        _health.SetValue(30f);
        Drain();

        Assert.AreEqual(30f, _health.Value);
    }

    [Test]
    public void Percent_ReturnsValueOverMax()
    {
        AddModifier(new FakeConsumer(-25f));
        Drain();

        Assert.AreEqual(0.75f, _health.percent);
    }

    [Test]
    public void AddResourceModifier_DamageConsumer_ReducesValue()
    {
        AddModifier(new FakeConsumer(-10f));
        Drain();

        Assert.AreEqual(90f, _health.Value);
    }

    [Test]
    public void AddResourceModifier_ClampsAtZero_NeverNegative()
    {
        AddModifier(new FakeConsumer(-1000f));
        Drain();

        Assert.AreEqual(0f, _health.Value);
    }

    [Test]
    public void AddResourceModifier_ClampsAtMax_HealingNeverOverheals()
    {
        // Healing bypasses armor (ignoreDamageReduction) since the armor formula assumes damage.
        AddModifier(new FakeConsumer(1000f, ignoreDamageReduction: true));
        Drain();

        Assert.AreEqual(100f, _health.Value);
    }

    [Test]
    public void AddResourceModifier_PositiveFlatArmor_ReducesEachHit()
    {
        _targetAttributeManager.GetOrAdd(AttributeType.FlatArmor).BaseValue = 3f;
        _targetAttributeManager.GetOrAdd(AttributeType.FlatArmor).Update();

        AddModifier(new FakeConsumer(-10f));
        Drain();

        Assert.AreEqual(93f, _health.Value); // took 7 damage (10 - 3) instead of 10
    }

    [Test]
    public void AddResourceModifier_FlatArmorAboveDamage_BlocksTheHitWithoutHealing()
    {
        AddModifier(new FakeConsumer(-50f));
        Drain();
        _targetAttributeManager.GetOrAdd(AttributeType.FlatArmor).BaseValue = 3f;
        _targetAttributeManager.GetOrAdd(AttributeType.FlatArmor).Update();

        AddModifier(new FakeConsumer(-2f));
        Drain();

        Assert.AreEqual(50f, _health.Value); // 2 damage fully blocked, not turned into +1 heal
    }

    [Test]
    public void AddResourceModifier_NegativeFlatArmor_ClampsToZero_HasNoEffect()
    {
        // Attribute.Update() clamps every attribute's .Value to Mathf.Max(_value, 0f), so a
        // negative FlatArmor can't be used to add damage.
        Attribute flatArmor = _targetAttributeManager.GetOrAdd(AttributeType.FlatArmor);
        flatArmor.BaseValue = -3f;
        flatArmor.Update();
        Assert.AreEqual(0f, flatArmor.Value); // clamped, despite BaseValue being -3

        AddModifier(new FakeConsumer(-10f));
        Drain();

        Assert.AreEqual(90f, _health.Value); // identical to having no armor at all
    }

    [Test]
    public void AddResourceModifier_PercentArmor_ReducesDamageByPercentage()
    {
        _targetAttributeManager.GetOrAdd(AttributeType.PercentArmor).BaseValue = 0.3f;
        _targetAttributeManager.GetOrAdd(AttributeType.PercentArmor).Update();

        AddModifier(new FakeConsumer(-10f));
        Drain();

        Assert.AreEqual(93f, _health.Value); // took 7 damage (10 * 0.7) instead of 10
    }

    [Test]
    public void AddResourceModifier_Vulnerability_IncreasesDamage()
    {
        _targetAttributeManager.GetOrAdd(AttributeType.Vulnerability).BaseValue = 0.5f;
        _targetAttributeManager.GetOrAdd(AttributeType.Vulnerability).Update();

        AddModifier(new FakeConsumer(-10f));
        Drain();

        Assert.AreEqual(85f, _health.Value); // took 15 damage (10 * 1.5) instead of 10
    }

    [Test]
    public void HealingReceived_ByDefault_IsOneAndKeepsHealsWhole()
    {
        AddModifier(new FakeConsumer(-50f));
        Drain();

        AddModifier(new FakeConsumer(20f, ignoreDamageReduction: true));
        Drain();

        Assert.AreEqual(1f, _targetAttributeManager.Get(AttributeType.HealingReceived).Value);
        Assert.AreEqual(70f, _health.Value);
    }

    [Test]
    public void HealingReceived_MultipliesHeals()
    {
        AddModifier(new FakeConsumer(-50f));
        Drain();
        SetAttribute(AttributeType.HealingReceived, 0.5f);

        AddModifier(new FakeConsumer(20f, ignoreDamageReduction: true));
        Drain();

        Assert.AreEqual(60f, _health.Value); // healed 10 (20 * 0.5) instead of 20
    }

    [Test]
    public void HealingReceived_AppliesAfterTheMultiplier()
    {
        AddModifier(new FakeConsumer(-50f));
        Drain();
        SetAttribute(AttributeType.HealingReceived, 0.5f);

        AddScaledModifier(new FakeConsumer(10f, ignoreDamageReduction: true), 2f);
        Drain();

        Assert.AreEqual(60f, _health.Value); // healed 10 (10 * 2 * 0.5) instead of 20
    }

    [Test]
    public void HealingReceived_DoesNotChangeDamage()
    {
        SetAttribute(AttributeType.HealingReceived, 0.5f);

        AddModifier(new FakeConsumer(-10f));
        Drain();

        Assert.AreEqual(90f, _health.Value);
    }

    // Character skills scale their consumer with the modifier's multiplier
    void AddScaledModifier(AConsumer consumer, float multiplier)
    {
        ResourceModifier modifier = new ResourceModifier { source = _sourceGo, multiplier = multiplier };
        modifier.consumers.Add(consumer);
        _health.AddResourceModifier(modifier);
    }

    void SetAttribute(AttributeType type, float value)
    {
        Attribute attribute = _targetAttributeManager.GetOrAdd(type);
        attribute.BaseValue = value;
        attribute.Update();
    }

    // Heals don't always set ignoreDamageReduction: being positive must be enough
    [Test]
    public void Heal_IsNotReducedByArmor()
    {
        AddModifier(new FakeConsumer(-50f));
        Drain();
        SetAttribute(AttributeType.FlatArmor, 3f);
        SetAttribute(AttributeType.PercentArmor, 0.5f);

        AddModifier(new FakeConsumer(10f));
        Drain();

        Assert.AreEqual(60f, _health.Value); // full 10 heal, not blocked to 0 by the damage formula
    }

    [Test]
    public void Heal_IsNotIncreasedByVulnerability()
    {
        AddModifier(new FakeConsumer(-50f));
        Drain();
        SetAttribute(AttributeType.Vulnerability, 0.5f);

        AddModifier(new FakeConsumer(10f));
        Drain();

        Assert.AreEqual(60f, _health.Value); // 10 heal, not 15
    }

    [Test]
    public void Heal_DoesNotConsumeHitArmor()
    {
        AddModifier(new FakeConsumer(-50f));
        Drain();
        SetAttribute(AttributeType.HitArmor, 2f);

        AddModifier(new FakeConsumer(10f));
        Drain();

        Assert.AreEqual(60f, _health.Value);
        Assert.AreEqual(2f, _targetAttributeManager.Get(AttributeType.HitArmor).BaseValue);
    }

    [Test]
    public void ScaledHeal_IsMultiplied()
    {
        AddModifier(new FakeConsumer(-50f));
        Drain();

        AddScaledModifier(new FakeConsumer(10f), 1.5f);
        Drain();

        Assert.AreEqual(65f, _health.Value);
    }

    [Test]
    public void ScaledDamage_ArmorAppliesBeforeTheMultiplier()
    {
        SetAttribute(AttributeType.FlatArmor, 3f);

        AddScaledModifier(new FakeConsumer(-10f), 2f);
        Drain();

        Assert.AreEqual(86f, _health.Value); // (10 - 3) * 2 = 14 damage
    }

    [Test]
    public void AddResourceModifier_HitArmor_BlocksDamageAndDecrementsBaseValue()
    {
        Attribute hitArmor = _targetAttributeManager.GetOrAdd(AttributeType.HitArmor);
        hitArmor.BaseValue = 5f;
        hitArmor.Update();

        AddModifier(new FakeConsumer(-10f));
        Drain();

        Assert.AreEqual(100f, _health.Value); // damage fully blocked
        Assert.AreEqual(4f, hitArmor.BaseValue); // hit armor consumed one charge
    }

    [Test]
    public void AddResourceModifier_IgnoreDamageReduction_BypassesArmor()
    {
        _targetAttributeManager.GetOrAdd(AttributeType.PercentArmor).BaseValue = 0.9f;
        _targetAttributeManager.GetOrAdd(AttributeType.PercentArmor).Update();

        AddModifier(new FakeConsumer(-10f, ignoreDamageReduction: true));
        Drain();

        Assert.AreEqual(90f, _health.Value); // full 10 damage, armor ignored
    }

    [Test]
    public void PreventConsumers_BlocksConsumersThatDoNotIgnoreIt()
    {
        _health.preventConsumers = true;

        AddModifier(new FakeConsumer(-10f, ignoreConsumerPrevention: false));
        Drain();

        Assert.AreEqual(100f, _health.Value);
    }

    [Test]
    public void PreventConsumers_DoesNotBlockConsumersThatIgnoreIt()
    {
        _health.preventConsumers = true;

        AddModifier(new FakeConsumer(-10f, ignoreConsumerPrevention: true));
        Drain();

        Assert.AreEqual(90f, _health.Value);
    }

    [Test]
    public void PreventConsumers_FalseAfterMatchingDisable()
    {
        _health.preventConsumers = true;
        _health.preventConsumers = false;

        AddModifier(new FakeConsumer(-10f));
        Drain();

        Assert.AreEqual(90f, _health.Value);
    }

    [Test]
    public void MultipleConsumersOnSameModifier_AreSummedInOneDrain()
    {
        AddModifier(new FakeConsumer(-10f), new FakeConsumer(-5f));
        Drain();

        Assert.AreEqual(85f, _health.Value);
    }

    [Test]
    public void OnValueChanged_InvokedWhenValueActuallyChanges()
    {
        int callCount = 0;
        ResourceAttribute received = null;
        _health.OnValueChanged.AddListener(ra => { callCount++; received = ra; });

        AddModifier(new FakeConsumer(-10f));
        Drain();

        Assert.AreEqual(1, callCount);
        Assert.AreSame(_health, received);
    }

    [Test]
    public void OnValueChanged_NotInvokedWhenNothingChanges()
    {
        int callCount = 0;
        _health.OnValueChanged.AddListener(_ => callCount++);

        Drain(); // no modifiers added, value stays at Max

        Assert.AreEqual(0, callCount);
    }

    [Test]
    public void OnAllConsumerProcessed_InvokedWithComputedValue()
    {
        GameObject receivedGameObject = null;
        float receivedValue = 0f;
        bool receivedIsCritical = true;
        _health.OnAllConsumerProcessed.AddListener((go, modifier, result) =>
        {
            receivedGameObject = go;
            receivedValue = result.value;
            receivedIsCritical = result.isCritical;
        });

        AddModifier(new FakeConsumer(-10f));
        Drain();

        Assert.AreSame(_targetGo, receivedGameObject);
        Assert.AreEqual(-10f, receivedValue);
        Assert.IsFalse(receivedIsCritical);
    }

    [Test]
    public void CriticalHit_MultipliesDamage_WhenChanceGuaranteesIt()
    {
        AttributeManager sourceAttributeManager = _sourceGo.GetComponent<AttributeManager>();
        sourceAttributeManager.Add(AttributeType.CriticalChance, new Attribute(100f));
        sourceAttributeManager.Add(AttributeType.CriticalMultiplier, new Attribute(2f));

        AddModifier(new FakeConsumer(-10f));
        Drain();

        Assert.AreEqual(80f, _health.Value); // 10 * 2 critical damage
    }

    [Test]
    public void CriticalHit_WithoutCriticalMultiplier_Deals1Point5TimesTheDamage()
    {
        // e.g. a Lucky Coin on a unit that never had a critical multiplier
        _sourceGo.GetComponent<AttributeManager>().Add(AttributeType.CriticalChance, new Attribute(100f));

        AddModifier(new FakeConsumer(-10f));
        Drain();

        Assert.AreEqual(85f, _health.Value);
    }

    [Test]
    public void CriticalHit_NeverHappens_WhenChanceIsZeroOrBelowResist()
    {
        AttributeManager sourceAttributeManager = _sourceGo.GetComponent<AttributeManager>();
        sourceAttributeManager.Add(AttributeType.CriticalChance, new Attribute(0f));
        sourceAttributeManager.Add(AttributeType.CriticalMultiplier, new Attribute(2f));

        AddModifier(new FakeConsumer(-10f));
        Drain();

        Assert.AreEqual(90f, _health.Value); // no crit applied
    }

    // Wounds the target by 50 so a heal can be seen
    void Wound()
    {
        AddModifier(new FakeConsumer(-50f));
        Drain();
    }

    [Test]
    public void HealCritical_Alone_MakesHealsCriticalWithTheDefaultMultiplier()
    {
        // e.g. the Chalice of Plenty on a character without any critical attribute
        Wound();
        _sourceGo.GetComponent<AttributeManager>().Add(AttributeType.HealCriticalChance, new Attribute(100f));
        bool isCritical = false;
        _health.OnAllConsumerProcessed.AddListener((go, modifier, result) => isCritical = result.isCritical);

        AddModifier(new FakeConsumer(10f, ignoreDamageReduction: true));
        Drain();

        Assert.AreEqual(65f, _health.Value); // healed 15 (10 * 1.5)
        Assert.IsTrue(isCritical);
    }

    [Test]
    public void HealCritical_UsesTheCriticalMultiplier()
    {
        Wound();
        AttributeManager sourceAttributeManager = _sourceGo.GetComponent<AttributeManager>();
        sourceAttributeManager.Add(AttributeType.HealCriticalChance, new Attribute(100f));
        sourceAttributeManager.Add(AttributeType.CriticalMultiplier, new Attribute(3f));

        AddModifier(new FakeConsumer(10f, ignoreDamageReduction: true));
        Drain();

        Assert.AreEqual(80f, _health.Value); // healed 30 (10 * 3)
    }

    [Test]
    public void HealCritical_IsAddedToTheCriticalChanceForHeals()
    {
        // 60 + 40: a single roll that always succeeds, where two rolls in a row could both fail
        Wound();
        AttributeManager sourceAttributeManager = _sourceGo.GetComponent<AttributeManager>();
        sourceAttributeManager.Add(AttributeType.CriticalChance, new Attribute(60f));
        sourceAttributeManager.Add(AttributeType.HealCriticalChance, new Attribute(40f));

        AddModifier(new FakeConsumer(10f, ignoreDamageReduction: true));
        Drain();

        Assert.AreEqual(65f, _health.Value);
    }

    [Test]
    public void HealCritical_BothChances_AreASingleCritical()
    {
        Wound();
        AttributeManager sourceAttributeManager = _sourceGo.GetComponent<AttributeManager>();
        sourceAttributeManager.Add(AttributeType.CriticalChance, new Attribute(100f));
        sourceAttributeManager.Add(AttributeType.HealCriticalChance, new Attribute(100f));

        AddModifier(new FakeConsumer(10f, ignoreDamageReduction: true));
        Drain();

        Assert.AreEqual(65f, _health.Value); // healed 15 (10 * 1.5), not 22.5 (10 * 1.5 * 1.5)
    }

    [Test]
    public void HealCritical_NeverHappens_WhenChanceIsZero()
    {
        Wound();
        _sourceGo.GetComponent<AttributeManager>().Add(AttributeType.HealCriticalChance, new Attribute(0f));

        AddModifier(new FakeConsumer(10f, ignoreDamageReduction: true));
        Drain();

        Assert.AreEqual(60f, _health.Value);
    }

    [Test]
    public void HealCritical_NeverAppliesToDamage()
    {
        _sourceGo.GetComponent<AttributeManager>().Add(AttributeType.HealCriticalChance, new Attribute(100f));

        AddModifier(new FakeConsumer(-10f));
        Drain();

        Assert.AreEqual(90f, _health.Value);
    }

    [Test]
    public void HealCritical_IsReducedByTheCriticalResistOfTheTarget()
    {
        Wound();
        _sourceGo.GetComponent<AttributeManager>().Add(AttributeType.HealCriticalChance, new Attribute(100f));
        SetAttribute(AttributeType.CriticalChanceResist, 100f);

        AddModifier(new FakeConsumer(10f, ignoreDamageReduction: true));
        Drain();

        Assert.AreEqual(60f, _health.Value);
    }

    // The overflows reported for the modifiers of the next drain
    List<float> Overflows()
    {
        List<float> overflows = new List<float>();
        _health.OnAllConsumerProcessed.AddListener((go, modifier, result) => overflows.Add(result.overflow));
        return overflows;
    }

    [Test]
    public void Overflow_HealAboveTheMax_ReportsThePartAboveIt()
    {
        AddModifier(new FakeConsumer(-10f));
        Drain();
        List<float> overflows = Overflows();

        AddModifier(new FakeConsumer(25f, ignoreDamageReduction: true));
        Drain();

        CollectionAssert.AreEqual(new[] { 15f }, overflows);
        Assert.AreEqual(100f, _health.Value);
    }

    [Test]
    public void Overflow_HealWithinTheMax_IsZero()
    {
        AddModifier(new FakeConsumer(-30f));
        Drain();
        List<float> overflows = Overflows();

        AddModifier(new FakeConsumer(20f, ignoreDamageReduction: true));
        Drain();

        CollectionAssert.AreEqual(new[] { 0f }, overflows);
    }

    [Test]
    public void Overflow_HealAtFullHealth_IsEntirelyReported()
    {
        List<float> overflows = Overflows();

        AddModifier(new FakeConsumer(20f, ignoreDamageReduction: true));
        Drain();

        CollectionAssert.AreEqual(new[] { 20f }, overflows);
    }

    [Test]
    public void Overflow_Damage_IsZero()
    {
        List<float> overflows = Overflows();

        AddModifier(new FakeConsumer(-20f));
        Drain();

        CollectionAssert.AreEqual(new[] { 0f }, overflows);
    }

    [Test]
    public void Overflow_SeveralHealsInAFrame_EachReportsItsOwnPart()
    {
        AddModifier(new FakeConsumer(-10f));
        Drain();
        List<float> overflows = Overflows();

        AddModifier(new FakeConsumer(15f, ignoreDamageReduction: true));
        AddModifier(new FakeConsumer(20f, ignoreDamageReduction: true));
        Drain();

        // 5 above the max for the first one, then the whole second one
        CollectionAssert.AreEqual(new[] { 5f, 20f }, overflows);
    }

    [Test]
    public void GetOverflow_ComputesThePartAboveTheMax()
    {
        Assert.AreEqual(0f, ResourceAttribute.GetOverflow(50f, 20f, 100f));
        Assert.AreEqual(10f, ResourceAttribute.GetOverflow(90f, 20f, 100f));
        Assert.AreEqual(20f, ResourceAttribute.GetOverflow(110f, 20f, 100f));
        Assert.AreEqual(0f, ResourceAttribute.GetOverflow(110f, -20f, 100f));
    }

    [Test]
    public void MaxAttributeChanged_UpdatesValueAndFiresOnValueChanged()
    {
        int callCount = 0;
        _health.OnValueChanged.AddListener(_ => callCount++);

        Attribute maxAttribute = _targetAttributeManager.Get(AttributeType.HealthMax);
        maxAttribute.BaseValue = 150f;
        maxAttribute.Update();

        Assert.AreEqual(150f, _health.Value);
        Assert.AreEqual(150f, _health.Max);
        Assert.AreEqual(1, callCount);
    }

    void SetMax(float value)
    {
        Attribute maxAttribute = _targetAttributeManager.Get(AttributeType.HealthMax);
        maxAttribute.BaseValue = value;
        maxAttribute.Update();
    }

    [Test]
    public void MaxIncreased_WhileDamaged_KeepsTheDamageTaken()
    {
        AddModifier(new FakeConsumer(-60f));
        Drain(); // 40 / 100

        SetMax(150f);

        Assert.AreEqual(90f, _health.Value); // still 60 damage taken, not a full heal
    }

    [Test]
    public void MaxDecreased_KeepsTheCurrentValue()
    {
        AddModifier(new FakeConsumer(-60f));
        Drain(); // 40 / 100
        SetMax(150f); // 90 / 150

        SetMax(100f); // the buff ends

        Assert.AreEqual(90f, _health.Value);
    }

    [Test]
    public void MaxDecreasedBelowTheValue_CapsItAtTheNewMax()
    {
        AddModifier(new FakeConsumer(-10f));
        Drain(); // 90 / 100

        SetMax(50f);

        Assert.AreEqual(50f, _health.Value);
    }

    [Test]
    public void MaxIncreased_WhenEmpty_AddsTheDifference()
    {
        // e.g. mana: 0 / 100 means 100 spent, still 100 spent at 50 / 150
        AddModifier(new FakeConsumer(-1000f));
        Drain(); // 0 / 100

        SetMax(150f);

        Assert.AreEqual(50f, _health.Value);
    }

    [Test]
    public void MaxChanged_ThenUpdate_FiresOnValueChangedOnce()
    {
        AddModifier(new FakeConsumer(-60f));
        Drain();
        int callCount = 0;
        _health.OnValueChanged.AddListener(_ => callCount++);

        SetMax(150f);
        Drain();

        Assert.AreEqual(1, callCount);
    }

    [Test]
    public void MaxDecreased_WithoutChangingTheValue_StillNotifiesTheNewMax()
    {
        AddModifier(new FakeConsumer(-60f));
        Drain(); // 40 / 100
        SetMax(150f); // 90 / 150
        float notifiedMax = 0f;
        _health.OnValueChanged.AddListener(health => notifiedMax = health.Max);

        SetMax(100f); // 90 / 100: same value, the views must show the new max

        Assert.AreEqual(100f, notifiedMax);
    }
}

}
