using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

// The allies come from the EntityManager singleton in the game
public class TestLifeStealBuff : LifeStealBuff
{
    public List<GameObject> allies = new List<GameObject>();

    protected override List<GameObject> GetAllies() => allies;
}

public class LifeStealBuffTests
{
    readonly TestUnits _units = new TestUnits();
    Entity _owner;
    Entity _wounded;
    TestLifeStealBuff _buff;

    [SetUp]
    public void SetUp()
    {
        _owner = _units.Create(100f, 100f, "Blood Cultist");
        _wounded = _units.Create(20f, 100f, "Wounded");
        _buff = new TestLifeStealBuff { data = new LifeStealBuffData { ratio = 0.5f } };
        _buff.allies = new List<GameObject> { _owner.gameObject, _wounded.gameObject };
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
    }

    [Test]
    public void DamageDealt_WhileApplied_HealsTheMostWoundedAllyForTheRatio()
    {
        _buff.Add(_owner.gameObject, _owner.gameObject);

        _owner.OnDamageDealt.Invoke(null, 10f);
        TestUnits.Process(_wounded.health);

        Assert.AreEqual(25f, _wounded.health.Value, 0.0001f);
    }

    [Test]
    public void DamageDealt_AfterRemove_HealsNobody()
    {
        _buff.Add(_owner.gameObject, _owner.gameObject);
        _buff.Remove(_owner.gameObject, _owner.gameObject);

        _owner.OnDamageDealt.Invoke(null, 10f);

        CollectionAssert.IsEmpty(TestUnits.GetPendingModifiers(_wounded));
    }
}

}
