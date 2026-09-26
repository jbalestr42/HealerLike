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
        _health.OnAllConsumerProcessed.AddListener((go, modifier, value, isCritical) =>
        {
            receivedGameObject = go;
            receivedValue = value;
            receivedIsCritical = isCritical;
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
    public void CriticalHit_NeverHappens_WhenChanceIsZeroOrBelowResist()
    {
        AttributeManager sourceAttributeManager = _sourceGo.GetComponent<AttributeManager>();
        sourceAttributeManager.Add(AttributeType.CriticalChance, new Attribute(0f));
        sourceAttributeManager.Add(AttributeType.CriticalMultiplier, new Attribute(2f));

        AddModifier(new FakeConsumer(-10f));
        Drain();

        Assert.AreEqual(90f, _health.Value); // no crit applied
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
}

}
