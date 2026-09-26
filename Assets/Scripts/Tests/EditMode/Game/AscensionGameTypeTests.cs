using NUnit.Framework;
using UnityEngine;

namespace Game
{

// Only the pieces independent from the singletons (GameManager, UIManager, ...) the run loop relies on
public class AscensionGameTypeTests
{
    GameObject _entityGo;
    GameObject _healthGo;
    Entity _entity;
    ResourceAttribute _health;
    ConsumerFactory _restHeal;

    [SetUp]
    public void SetUp()
    {
        _entityGo = new GameObject();
        // Adding Entity triggers Entity.Reset() (an editor-only message), which NREs without a
        // full Entity.Init() - not needed here, we only use it as a holder for .health.
        TestHelpers.WithLoggingDisabled(() => _entity = _entityGo.AddComponent<Entity>());
        // The entity is the source of the heal, the resolver reads its critical chance
        TestHelpers.InvokePrivate(_entityGo.GetComponent<AttributeManager>(), "Awake");

        _healthGo = new GameObject();
        _health = TestHelpers.CreateResourceAttribute(_healthGo, AttributeType.HealthMax, 100f);
        TestHelpers.SetPrivateField(_entity, "_health", _health);

        // Same setup as the rest room asset: heal 30% of the max health, never reduced nor prevented
        _restHeal = ScriptableObject.CreateInstance<ConsumerFactory>();
        _restHeal.data = new ConsumerData
        {
            ignoreDamageReduction = true,
            ignoreConsumerPrevention = true,
            value = new MaxHealthValue { data = new MaxHealthValueData { multiplier = -0.3f } },
        };
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_entityGo);
        Object.DestroyImmediate(_healthGo);
        Object.DestroyImmediate(_restHeal);
    }

    void SetHealth(float value)
    {
        TestHelpers.SetPrivateField(_health, "_value", value);
    }

    void Drain()
    {
        TestHelpers.InvokePrivate(_health, "Update");
    }

    [Test]
    public void ApplyConsumer_RestHeal_GivesBackAShareOfTheMaxHealth()
    {
        SetHealth(40f);

        AscensionGameType.ApplyConsumer(_entity, _restHeal);
        Drain();

        Assert.AreEqual(70f, _health.Value, 0.001f);
    }

    [Test]
    public void ApplyConsumer_RestHeal_NeverHealsAboveMax()
    {
        SetHealth(90f);

        AscensionGameType.ApplyConsumer(_entity, _restHeal);
        Drain();

        Assert.AreEqual(100f, _health.Value);
    }

    [Test]
    public void ApplyConsumer_GoesThroughTheResourceFlowWithTheEntityAsSource()
    {
        SetHealth(40f);
        GameObject processedTarget = null;
        ResourceModifier processedModifier = null;
        float processedValue = 0f;
        _health.OnAllConsumerProcessed.AddListener((target, modifier, value, isCritical) =>
        {
            processedTarget = target;
            processedModifier = modifier;
            processedValue = value;
        });
        int changedCount = 0;
        _health.OnValueChanged.AddListener(_ => changedCount++);

        AscensionGameType.ApplyConsumer(_entity, _restHeal);
        Drain();

        Assert.AreSame(_healthGo, processedTarget);
        Assert.AreSame(_entityGo, processedModifier.source);
        Assert.AreEqual(30f, processedValue, 0.001f);
        Assert.AreEqual(1, changedCount);
    }

    [Test]
    public void ApplyConsumer_RestHeal_IsNotBlockedByConsumerPrevention()
    {
        SetHealth(40f);
        _health.preventConsumers = true;

        AscensionGameType.ApplyConsumer(_entity, _restHeal);
        Drain();

        Assert.AreEqual(70f, _health.Value, 0.001f);
    }
}

}
