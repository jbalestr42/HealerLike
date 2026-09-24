using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Spells
{

public class ResourceOutcomeObserverTests
{
    class HealSinkSpy : IHealthVisualSink
    {
        public int count;

        public void OnHealthResolved(GameObject target, float value, bool critical)
        {
            count++;
        }
    }

    class SpellSinkSpy : ISpellVisualSink
    {
        public int impacts;
        public ResourceKind last;

        public void ShowImpact(GameObject source, GameObject target, ResourceKind resource, float amount,
            bool critical)
        {
            impacts++;
            last = resource;
        }

        public void SetStatus(GameObject source, GameObject target, ABuffHandlerFactory factory, int stacks,
            float elapsed, float duration)
        {
        }

        public void RemoveStatus(GameObject source, GameObject target, ABuffHandlerFactory factory)
        {
        }

        public void PulseArea(Vector3 center, float radius, ZoneKind kind, float strength)
        {
        }
    }

    GameObject _owner;
    GameObject _caster;
    GameObject _manaGo;
    ResourceAttribute _health;
    ResourceAttribute _mana;
    SpellSinkSpy _spy;
    HealSinkSpy _healed;
    RenderRegistry _registry;
    ResourceModifier _modifier;

    [SetUp]
    public void SetUp()
    {
        _owner = new GameObject("ResourceOwner");
        _caster = new GameObject("Caster");
        _health = TestHelpers.CreateResourceAttribute(_owner, AttributeType.HealthMax, 100);
        _manaGo = new GameObject("Mana");
        _manaGo.transform.SetParent(_owner.transform);
        _mana = TestHelpers.CreateResourceAttribute(_manaGo, AttributeType.ManaMax, 100);
        _spy = new SpellSinkSpy();
        _registry = new RenderRegistry();
        _healed = new HealSinkSpy();
        _registry.Register(_caster, _healed);
        _modifier = new ResourceModifier { source = _caster };
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_owner);
        Object.DestroyImmediate(_caster);
    }

    ResourceOutcomeObserver CreateObserver()
    {
        ResourceOutcomeObserver observer = _owner.AddComponent<ResourceOutcomeObserver>();
        observer.Init(_health, _mana, _spy, _registry);
        return observer;
    }

    [Test]
    public void Init_Repeated_SubscribesOnce()
    {
        ResourceOutcomeObserver observer = CreateObserver();
        for (int i = 0; i < 4; i++)
        {
            observer.Init(_health, _mana, _spy, _registry);
        }

        _health.OnAllConsumerProcessed.Invoke(_owner, _modifier, 7, false);

        Assert.AreEqual(1, _spy.impacts);
    }

    [Test]
    public void Init_ManaChanged_ShowsManaWithoutReachingTheHealSinks()
    {
        CreateObserver();

        _mana.OnAllConsumerProcessed.Invoke(_manaGo, _modifier, 9, false);

        Assert.AreEqual(1, _spy.impacts);
        Assert.AreEqual(ResourceKind.Mana, _spy.last);
        Assert.AreEqual(0, _healed.count);
    }

    [Test]
    public void Init_Damage_ReachesTheRegistryToo()
    {
        CreateObserver();

        _health.OnAllConsumerProcessed.Invoke(_owner, _modifier, -2, false);

        Assert.AreEqual(1, _spy.impacts);
        Assert.AreEqual(1, _healed.count, "each heal sink filters by sign, the registry passes damage on");
    }

    [Test]
    public void OnDisable_HealthChanged_ShowsNothing()
    {
        ResourceOutcomeObserver observer = CreateObserver();
        observer.enabled = false;

        TestHelpers.InvokePrivate(observer, "OnDisable");
        _health.OnAllConsumerProcessed.Invoke(_owner, _modifier, 7, false);

        Assert.AreEqual(0, _spy.impacts);
    }

    [Test]
    public void OnEnable_AfterDisable_ListensAgain()
    {
        ResourceOutcomeObserver observer = CreateObserver();
        observer.enabled = false;
        TestHelpers.InvokePrivate(observer, "OnDisable");
        observer.enabled = true;

        TestHelpers.InvokePrivate(observer, "OnEnable");
        _health.OnAllConsumerProcessed.Invoke(_owner, _modifier, 7, false);

        Assert.AreEqual(1, _spy.impacts);
    }
}

}
