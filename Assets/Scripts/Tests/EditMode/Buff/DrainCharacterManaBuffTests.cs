using NUnit.Framework;
using UnityEngine;

namespace Buff
{

// The real character is reached through the PlayerBehaviour singleton
public class TestDrainCharacterManaBuff : DrainCharacterManaBuff
{
    public ResourceAttribute testMana;

    protected override ResourceAttribute characterMana => testMana;
}

public class DrainCharacterManaBuffTests
{
    GameObject _character;
    GameObject _source;
    GameObject _target;
    ResourceAttribute _mana;
    ResourceAttribute _targetHealth;
    Attributes.RecordingConsumerFactory _drain;
    TestDrainCharacterManaBuff _buff;

    [SetUp]
    public void SetUp()
    {
        _character = new GameObject("Character");
        _mana = TestHelpers.CreateResourceAttribute(_character, AttributeType.ManaMax, 100f);
        _source = new GameObject("Siphoner");
        TestHelpers.CreateAttributeManager(_source);
        _target = new GameObject("Target");
        _targetHealth = TestHelpers.CreateResourceAttribute(_target, AttributeType.HealthMax, 100f);

        _drain = ScriptableObject.CreateInstance<Attributes.RecordingConsumerFactory>(); // -5 per consumer
        _buff = new TestDrainCharacterManaBuff
        {
            data = new DrainCharacterManaBuffData { consumerFactory = _drain },
            testMana = _mana,
        };
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_character);
        if (_source != null)
        {
            Object.DestroyImmediate(_source);
        }
        Object.DestroyImmediate(_target);
        Object.DestroyImmediate(_drain);
    }

    static void Drain(ResourceAttribute resource)
    {
        TestHelpers.InvokePrivate(resource, "Update");
    }

    [Test]
    public void Instant_DrainsTheCharacterMana()
    {
        _buff.Instant(_source, _target);
        Drain(_mana);

        Assert.AreEqual(95f, _mana.Value);
    }

    [Test]
    public void Instant_LeavesTheHitTargetUntouched()
    {
        _buff.Instant(_source, _target);
        Drain(_targetHealth);

        Assert.AreEqual(100f, _targetHealth.Value);
    }

    [Test]
    public void Instant_TheAttackerIsTheSource()
    {
        ResourceModifier processed = null;
        _mana.OnAllConsumerProcessed.AddListener((target, modifier, value, isCritical) => processed = modifier);

        _buff.Instant(_source, _target);
        Drain(_mana);

        Assert.AreSame(_source, processed.source);
    }

    [Test]
    public void Instant_DestroyedSource_StillDrains()
    {
        Object.DestroyImmediate(_source);

        _buff.Instant(_source, _target);
        Drain(_mana);

        Assert.AreEqual(95f, _mana.Value);
    }

    [Test]
    public void Instant_NoCharacter_DoesNothing()
    {
        _buff.testMana = null;

        Assert.DoesNotThrow(() => _buff.Instant(_source, _target));
    }

    [Test]
    public void Instant_EachHitDrainsAgain()
    {
        _buff.Instant(_source, _target);
        _buff.Instant(_source, _target);
        Drain(_mana);

        Assert.AreEqual(90f, _mana.Value);
    }
}

}
