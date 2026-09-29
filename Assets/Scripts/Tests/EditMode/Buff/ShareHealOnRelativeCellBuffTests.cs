using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

// The allies and the grid come from the EntityManager and PlayerBehaviour singletons in the game
public class TestShareHealOnRelativeCellBuff : ShareHealOnRelativeCellBuff
{
    public List<GameObject> allies = new List<GameObject>();
    public float cellSize = 2f;

    protected override List<GameObject> GetAllies() => allies;
    protected override float GetCellSize() => cellSize;
}

// Martyr's Heart: each heal received by the holder is partly shared with the allies around it
public class ShareHealOnRelativeCellBuffTests
{
    const float CellSize = 2f;

    readonly TestUnits _units = new TestUnits();
    Entity _owner;
    Entity _adjacent;
    Entity _diagonal;
    Entity _far;
    TestShareHealOnRelativeCellBuff _buff;

    [SetUp]
    public void SetUp()
    {
        _owner = CreateAt("Owner", 0, 0);
        _adjacent = CreateAt("Adjacent", 1, 0);
        _diagonal = CreateAt("Diagonal", -1, 1);
        _far = CreateAt("Far", 2, 0);
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
    }

    // A wounded unit on the cell (x, y) of the grid
    Entity CreateAt(string name, int x, int y)
    {
        Entity entity = _units.Create(50f, 100f, name);
        entity.transform.position = new Vector3(x * CellSize, 0f, y * CellSize);
        return entity;
    }

    void AddBuff(RelativeCellPatternType pattern, int range = 1)
    {
        _buff = new TestShareHealOnRelativeCellBuff
        {
            data = new ShareHealOnRelativeCellBuffData { ratio = 0.2f, pattern = pattern, range = range, minimumSharedHeal = 1f },
            cellSize = CellSize,
        };
        _buff.allies = new List<GameObject> { _owner.gameObject, _adjacent.gameObject, _diagonal.gameObject, _far.gameObject };
        _buff.Add(_owner.gameObject, _owner.gameObject);
    }

    void Heal(float amount)
    {
        Entity.NotifyHealed(null, _owner.gameObject, new ConsumerResult(amount, false));
    }

    static float Processed(Entity entity)
    {
        TestUnits.Process(entity.health);
        return entity.health.Value;
    }

    [Test]
    public void Heal_Received_HealsEveryAdjacentAllyForTheRatio()
    {
        AddBuff(RelativeCellPatternType.Adjacent);

        Heal(20f);

        Assert.AreEqual(54f, Processed(_adjacent), 0.0001f);
        Assert.AreEqual(54f, Processed(_diagonal), 0.0001f);
    }

    [Test]
    public void Heal_Received_SkipsTheAlliesOutOfThePattern()
    {
        AddBuff(RelativeCellPatternType.Row);

        Heal(20f);

        Assert.AreEqual(54f, Processed(_adjacent), 0.0001f);
        CollectionAssert.IsEmpty(TestUnits.GetPendingModifiers(_diagonal));
        CollectionAssert.IsEmpty(TestUnits.GetPendingModifiers(_far));
    }

    [Test]
    public void Heal_Received_TheRangeWidensThePattern()
    {
        AddBuff(RelativeCellPatternType.Row, 2);

        Heal(20f);

        Assert.AreEqual(54f, Processed(_far), 0.0001f);
    }

    [Test]
    public void Heal_Received_IsNotSharedWithTheHolderItself()
    {
        AddBuff(RelativeCellPatternType.Adjacent);

        Heal(20f);

        CollectionAssert.IsEmpty(TestUnits.GetPendingModifiers(_owner));
    }

    [Test]
    public void Heal_Received_SkipsADeadAlly()
    {
        AddBuff(RelativeCellPatternType.Adjacent);
        _adjacent.health.SetValue(0f);

        Heal(20f);

        CollectionAssert.IsEmpty(TestUnits.GetPendingModifiers(_adjacent));
    }

    [Test]
    public void Heal_Shared_ComesFromTheHolderAndIgnoresArmorAndInvincibility()
    {
        AddBuff(RelativeCellPatternType.Adjacent);

        Heal(20f);

        ResourceModifier shared = TestUnits.GetPendingModifiers(_adjacent)[0];
        Assert.AreSame(_owner.gameObject, shared.source);
        Assert.IsTrue(shared.consumers[0].ignoreDamageReduction);
        Assert.IsTrue(shared.consumers[0].ignoreConsumerPrevention);
    }

    [Test]
    public void Heal_TooSmallToShare_IsNotShared()
    {
        AddBuff(RelativeCellPatternType.Adjacent);

        // 20% of 4 is below the minimum shared heal of 1
        Heal(4f);

        CollectionAssert.IsEmpty(TestUnits.GetPendingModifiers(_adjacent));
    }

    [Test]
    public void Damage_Received_IsNotShared()
    {
        AddBuff(RelativeCellPatternType.Adjacent);

        Heal(-20f);

        CollectionAssert.IsEmpty(TestUnits.GetPendingModifiers(_adjacent));
    }

    [Test]
    public void Heal_AfterRemove_IsNotShared()
    {
        AddBuff(RelativeCellPatternType.Adjacent);
        _buff.Remove(_owner.gameObject, _owner.gameObject);

        Heal(20f);

        CollectionAssert.IsEmpty(TestUnits.GetPendingModifiers(_adjacent));
    }

    [Test]
    public void Add_WithACellPrefab_ShowsOneCellOnEachCellOfThePattern()
    {
        GameObject cellPrefab = new GameObject("Cell");
        try
        {
            _buff = new TestShareHealOnRelativeCellBuff
            {
                data = new ShareHealOnRelativeCellBuffData { pattern = RelativeCellPatternType.Row, range = 1, cellPrefab = cellPrefab },
                cellSize = CellSize,
            };
            _buff.Add(_owner.gameObject, _owner.gameObject);

            List<Vector3> cells = new List<Vector3>();
            foreach (Transform child in _owner.transform)
            {
                cells.Add(child.position);
            }
            CollectionAssert.AreEquivalent(new[] { new Vector3(CellSize, 0f, 0f), new Vector3(-CellSize, 0f, 0f) }, cells);
        }
        finally
        {
            Object.DestroyImmediate(cellPrefab);
        }
    }

    [Test]
    public void Add_WithoutACellPrefab_ShowsNothing()
    {
        AddBuff(RelativeCellPatternType.Adjacent);

        Assert.AreEqual(0, _owner.transform.childCount);
    }

    [Test]
    public void GetCellOffset_CountsTheCellsOnTheGroundPlane()
    {
        Assert.AreEqual(new Vector2Int(1, 0), ShareHealOnRelativeCellBuff.GetCellOffset(Vector3.zero, new Vector3(2f, 5f, 0f), 2f));
        Assert.AreEqual(new Vector2Int(-1, 2), ShareHealOnRelativeCellBuff.GetCellOffset(new Vector3(2f, 0f, 0f), new Vector3(0.1f, 0f, 3.9f), 2f));
    }
}

}
