using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

// The summons are spawned by the EntityManager singleton in the game, their positions are kept here
public class TestSummonOnKillBuff : SummonOnKillBuff
{
    public List<Vector3> spawns = new List<Vector3>();

    protected override GameObject Spawn(Vector3 position)
    {
        spawns.Add(position);
        return null;
    }
}

// Bone Charm: each enemy the holder kills rises as a summon of its side where it died
public class SummonOnKillBuffTests
{
    readonly TestUnits _units = new TestUnits();
    Entity _owner;
    EntityData _skeleton;
    TestSummonOnKillBuff _buff;

    [SetUp]
    public void SetUp()
    {
        _owner = _units.Create(100f, 100f, "Owner");
        _owner.entityType = Entity.EntityType.Player;
        _skeleton = ScriptableObject.CreateInstance<EntityData>();
        _buff = new TestSummonOnKillBuff { data = new SummonOnKillBuffData { entity = _skeleton } };
        _buff.Add(_owner.gameObject, _owner.gameObject);
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        Object.DestroyImmediate(_skeleton);
    }

    Entity CreateAt(Entity.EntityType entityType, Vector3 position)
    {
        Entity entity = _units.Create(0f, 100f, entityType.ToString());
        entity.entityType = entityType;
        entity.transform.position = position;
        return entity;
    }

    [Test]
    public void Kill_OfAnEnemy_SummonsWhereItDied()
    {
        Entity enemy = CreateAt(Entity.EntityType.Computer, new Vector3(4f, 0f, 2f));

        _owner.OnKill.Invoke(enemy);

        CollectionAssert.AreEqual(new[] { new Vector3(4f, 0f, 2f) }, _buff.spawns);
    }

    [Test]
    public void Kill_OfAnAlly_SummonsNothing()
    {
        Entity ally = CreateAt(Entity.EntityType.Player, Vector3.one);

        _owner.OnKill.Invoke(ally);

        CollectionAssert.IsEmpty(_buff.spawns);
    }

    [Test]
    public void Kill_AfterRemove_SummonsNothing()
    {
        _buff.Remove(_owner.gameObject, _owner.gameObject);
        Entity enemy = CreateAt(Entity.EntityType.Computer, Vector3.one);

        _owner.OnKill.Invoke(enemy);

        CollectionAssert.IsEmpty(_buff.spawns);
    }
}

}
