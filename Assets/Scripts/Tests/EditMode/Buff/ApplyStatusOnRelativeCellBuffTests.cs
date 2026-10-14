using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

// The allies and the board come from the EntityManager singleton in the game
public class TestApplyStatusOnRelativeCellBuff : ApplyStatusOnRelativeCellBuff
{
    public List<GameObject> allies = new List<GameObject>();
    public float cellSize = 2f;

    protected override List<GameObject> GetAllies(Entity owner) => allies;
    protected override float GetCellSize() => cellSize;
}

// Censer: the allies around the holder get its status (e.g. Blessed)
public class ApplyStatusOnRelativeCellBuffTests
{
    const float CellSize = 2f;

    readonly TestUnits _units = new TestUnits();
    readonly List<Object> _objects = new List<Object>();
    StatusData _status;
    BuffHandlerFactory _statusHandler;
    Entity _owner;
    Entity _adjacent;
    Entity _diagonal;
    Entity _far;

    [SetUp]
    public void SetUp()
    {
        _statusHandler = ScriptableObject.CreateInstance<BuffHandlerFactory>();
        _statusHandler.data = new BuffHandlerData { durationType = DurationType.Duration, duration = 4f, maxStacks = 1, buffFactoryList = new List<ABuffFactory>(), tags = new List<GameplayTag>() };
        _status = ScriptableObject.CreateInstance<StatusData>();
        _status.buffHandlerFactory = _statusHandler;
        _objects.Add(_statusHandler);
        _objects.Add(_status);

        _owner = CreateAt("Owner", 0, 0);
        _adjacent = CreateAt("Adjacent", 1, 0);
        _diagonal = CreateAt("Diagonal", -1, 1);
        _far = CreateAt("Far", 2, 0);
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        foreach (Object obj in _objects)
        {
            Object.DestroyImmediate(obj);
        }
        _objects.Clear();
    }

    // A unit on the cell (x, y) of the board
    Entity CreateAt(string name, int x, int y)
    {
        Entity entity = _units.Create(100f, 100f, name);
        entity.GetComponent<BuffManager>().isEnabled = true;
        entity.transform.position = new Vector3(x * CellSize, 0f, y * CellSize);
        return entity;
    }

    TestApplyStatusOnRelativeCellBuff CreateBuff(RelativeCellPatternType pattern = RelativeCellPatternType.Adjacent, int range = 1)
    {
        return new TestApplyStatusOnRelativeCellBuff
        {
            data = new ApplyStatusOnRelativeCellBuffData { status = _status, pattern = pattern, range = range },
            cellSize = CellSize,
            allies = new List<GameObject> { _owner.gameObject, _adjacent.gameObject, _diagonal.gameObject, _far.gameObject },
        };
    }

    BuffManager.BuffHandlerData GetStatus(Entity entity)
    {
        BuffManager buffManager = entity.GetComponent<BuffManager>();
        buffManager.ForceUpdate();
        return buffManager.GetActiveHandlers().Find(handler => handler.buffHandlerFactory == _statusHandler);
    }

    [Test]
    public void Instant_TheAdjacentAlliesGetTheStatus_FromTheHolder()
    {
        CreateBuff().Instant(_owner.gameObject, _owner.gameObject);

        Assert.AreSame(_owner.gameObject, GetStatus(_adjacent).source);
        Assert.IsNotNull(GetStatus(_diagonal), "adjacent includes the diagonals");
    }

    [Test]
    public void Instant_NeitherTheFarAlliesNorTheHolder()
    {
        CreateBuff().Instant(_owner.gameObject, _owner.gameObject);

        Assert.IsNull(GetStatus(_far));
        Assert.IsNull(GetStatus(_owner));
    }

    [Test]
    public void Instant_OtherPattern_OnlyItsCells()
    {
        CreateBuff(RelativeCellPatternType.Row, 2).Instant(_owner.gameObject, _owner.gameObject);

        Assert.IsNotNull(GetStatus(_adjacent));
        Assert.IsNotNull(GetStatus(_far));
        Assert.IsNull(GetStatus(_diagonal));
    }

    [Test]
    public void Instant_ADeadAlly_GetsNothing()
    {
        _adjacent.health.SetValue(0f);

        CreateBuff().Instant(_owner.gameObject, _owner.gameObject);

        Assert.IsNull(GetStatus(_adjacent));
    }

    [Test]
    public void Instant_WithoutStatus_NothingHappens()
    {
        TestApplyStatusOnRelativeCellBuff buff = CreateBuff();
        buff.data.status = null;

        Assert.DoesNotThrow(() => buff.Instant(_owner.gameObject, _owner.gameObject));
        Assert.IsNull(GetStatus(_adjacent));
    }
}

}
