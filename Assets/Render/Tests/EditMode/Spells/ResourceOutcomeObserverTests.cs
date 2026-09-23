using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Spells
{

public class ResourceOutcomeObserverTests
{
    class HealSinkSpy : IHealVisualSink
    {
        public int count;

        public void OnHealResolved(GameObject target, float value, bool critical)
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
            float elapsed, float duration, ClockKind clock)
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

    [SetUp]
    public void SetUp()
    {
        _owner = new GameObject("ResourceOwner");
        _caster = new GameObject("Caster");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_owner);
        Object.DestroyImmediate(_caster);
    }

    [Test]
    public void Bind_RepeatedWithHealthAndMana_SubscribesOnceAndKeepsManaApartFromHealth()
    {
        ResourceAttribute health = TestHelpers.CreateResourceAttribute(_owner, AttributeType.HealthMax, 100);
        GameObject manaGo = new GameObject("Mana");
        manaGo.transform.SetParent(_owner.transform);
        ResourceAttribute mana = TestHelpers.CreateResourceAttribute(manaGo, AttributeType.ManaMax, 100);
        SpellSinkSpy spy = new SpellSinkSpy();
        RenderRegistry registry = new RenderRegistry { spellSink = spy };
        HealSinkSpy healed = new HealSinkSpy();
        registry.Register(_caster, healed);
        ResourceOutcomeObserver observer = _owner.AddComponent<ResourceOutcomeObserver>();
        for (int i = 0; i < 5; i++)
        {
            observer.Bind(health, mana, spy, registry);
        }
        ResourceModifier modifier = new ResourceModifier { source = _caster };

        health.OnAllConsumerProcessed.Invoke(_owner, modifier, 7, false);
        mana.OnAllConsumerProcessed.Invoke(manaGo, modifier, 9, false);

        Assert.AreEqual(2, spy.impacts);
        Assert.AreEqual(ResourceKind.Mana, spy.last);
        Assert.AreEqual(1, healed.count);

        observer.enabled = false;
        TestHelpers.InvokePrivate(observer, "OnDisable");
        health.OnAllConsumerProcessed.Invoke(_owner, modifier, 7, false);
        Assert.AreEqual(2, spy.impacts);

        observer.enabled = true;
        TestHelpers.InvokePrivate(observer, "OnEnable");
        health.OnAllConsumerProcessed.Invoke(_owner, modifier, -2, false);
        Assert.AreEqual(3, spy.impacts);
        Assert.AreEqual(2, healed.count, "damage reaches the registry too, each sink filters by sign");
    }
}

}
