using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

// Blood Price: each attack of the owner applies a consumer to itself
public class ConsumerOnAttackBuffTests
{
    readonly TestUnits _units = new TestUnits();
    Entity _owner;
    ConsumerFactory _cost;
    ConsumerOnAttackBuff _buff;

    [SetUp]
    public void SetUp()
    {
        _owner = _units.Create(100f, 100f, "Owner");
        _cost = ScriptableObject.CreateInstance<ConsumerFactory>();
        _cost.data = new ConsumerData { ignoreDamageReduction = true, value = new FlatValue { data = new FlatValueData { value = 3f } } };
        _buff = new ConsumerOnAttackBuff { data = new ConsumerOnAttackBuffData { consumerFactory = _cost } };
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        Object.DestroyImmediate(_cost);
    }

    void Attack()
    {
        _owner.OnAttack.Invoke(null);
        TestUnits.Process(_owner.health);
    }

    [Test]
    public void Attack_WhileApplied_CostsTheOwnerTheConsumerValue()
    {
        _buff.Add(_owner.gameObject, _owner.gameObject);

        Attack();

        Assert.AreEqual(97f, _owner.health.Value, 0.0001f);
    }

    [Test]
    public void Attack_EachAttack_CostsAgain()
    {
        _buff.Add(_owner.gameObject, _owner.gameObject);

        Attack();
        Attack();

        Assert.AreEqual(94f, _owner.health.Value, 0.0001f);
    }

    [Test]
    public void Stack_MultipliesTheCostByTheStacks()
    {
        _buff.Add(_owner.gameObject, _owner.gameObject);
        _buff.Stack(_owner.gameObject, _owner.gameObject);

        Attack();

        Assert.AreEqual(94f, _owner.health.Value, 0.0001f);
    }

    [Test]
    public void Unstack_ReducesTheCostBackToOneStack()
    {
        _buff.Add(_owner.gameObject, _owner.gameObject);
        _buff.Stack(_owner.gameObject, _owner.gameObject);
        _buff.Unstack(_owner.gameObject, _owner.gameObject);

        Attack();

        Assert.AreEqual(97f, _owner.health.Value, 0.0001f);
    }

    [Test]
    public void Attack_AfterRemove_CostsNothing()
    {
        _buff.Add(_owner.gameObject, _owner.gameObject);
        _buff.Remove(_owner.gameObject, _owner.gameObject);

        _owner.OnAttack.Invoke(null);

        CollectionAssert.IsEmpty(TestUnits.GetPendingModifiers(_owner));
    }
}

}
