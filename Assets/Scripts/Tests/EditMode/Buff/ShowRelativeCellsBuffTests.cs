using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

// The board comes from the EntityManager singleton in the game
public class TestShowRelativeCellsBuff : ShowRelativeCellsBuff
{
    public float cellSize = 2f;

    protected override float GetCellSize() => cellSize;
}

// The zone of an effect of the holder shown on the ground (e.g. Martyr's Heart, Beacon, Phalanx)
public class ShowRelativeCellsBuffTests
{
    const float CellSize = 2f;

    GameObject _owner;
    GameObject _cellPrefab;

    [SetUp]
    public void SetUp()
    {
        _owner = new GameObject("Owner");
        _owner.transform.position = new Vector3(10f, 0f, 4f);
        _cellPrefab = new GameObject("Cell");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_owner);
        Object.DestroyImmediate(_cellPrefab);
    }

    TestShowRelativeCellsBuff CreateBuff(RelativeCellPatternType pattern, int range, GameObject cellPrefab)
    {
        return new TestShowRelativeCellsBuff
        {
            data = new ShowRelativeCellsBuffData { pattern = pattern, range = range, cellPrefab = cellPrefab },
            cellSize = CellSize,
        };
    }

    List<Vector3> GetCellOffsets()
    {
        List<Vector3> offsets = new List<Vector3>();
        foreach (Transform child in _owner.transform)
        {
            offsets.Add(child.position - _owner.transform.position);
        }
        return offsets;
    }

    [Test]
    public void Add_ShowsOneCellOnEachCellOfThePattern_AroundTheHolder()
    {
        CreateBuff(RelativeCellPatternType.Row, 1, _cellPrefab).Add(_owner, _owner);

        CollectionAssert.AreEquivalent(new[] { new Vector3(CellSize, 0f, 0f), new Vector3(-CellSize, 0f, 0f) }, GetCellOffsets());
    }

    [Test]
    public void Add_TheAdjacentCells_TheEightAroundTheHolder()
    {
        CreateBuff(RelativeCellPatternType.Adjacent, 1, _cellPrefab).Add(_owner, _owner);

        Assert.AreEqual(8, GetCellOffsets().Count);
        CollectionAssert.DoesNotContain(GetCellOffsets(), Vector3.zero, "not on the holder");
    }

    [Test]
    public void Cells_FollowTheHolder()
    {
        CreateBuff(RelativeCellPatternType.Row, 1, _cellPrefab).Add(_owner, _owner);

        _owner.transform.position += new Vector3(4f, 0f, 2f);

        CollectionAssert.AreEquivalent(new[] { new Vector3(CellSize, 0f, 0f), new Vector3(-CellSize, 0f, 0f) }, GetCellOffsets());
    }

    [Test]
    public void Remove_TheCellsAreGone()
    {
        TestShowRelativeCellsBuff buff = CreateBuff(RelativeCellPatternType.Adjacent, 1, _cellPrefab);
        buff.Add(_owner, _owner);

        buff.Remove(_owner, _owner);

        Assert.AreEqual(0, _owner.transform.childCount);
        Assert.AreEqual(0, buff.cells.Count);
    }

    [Test]
    public void Add_WithoutACellPrefab_ShowsNothing()
    {
        CreateBuff(RelativeCellPatternType.Adjacent, 1, null).Add(_owner, _owner);

        Assert.AreEqual(0, _owner.transform.childCount);
    }
}

}
