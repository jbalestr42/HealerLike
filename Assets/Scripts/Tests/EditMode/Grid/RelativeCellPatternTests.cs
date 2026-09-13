using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Grids
{

public class RelativeCellPatternTests
{
    [Test]
    public void GetOffsets_Adjacent_Range1_ReturnsEightNeighbours()
    {
        List<Vector2Int> offsets = RelativeCellPattern.GetOffsets(RelativeCellPatternType.Adjacent, 1);

        Assert.AreEqual(8, offsets.Count);
        CollectionAssert.DoesNotContain(offsets, Vector2Int.zero);
        Assert.Contains(new Vector2Int(1, 0), offsets);
        Assert.Contains(new Vector2Int(-1, -1), offsets);
    }

    [Test]
    public void GetOffsets_Adjacent_Range2_ReturnsTwentyFourCells()
    {
        List<Vector2Int> offsets = RelativeCellPattern.GetOffsets(RelativeCellPatternType.Adjacent, 2);

        Assert.AreEqual(24, offsets.Count);
        CollectionAssert.DoesNotContain(offsets, Vector2Int.zero);
    }

    [Test]
    public void GetOffsets_Diagonal_Range1_ReturnsFourDiagonalNeighbours()
    {
        List<Vector2Int> offsets = RelativeCellPattern.GetOffsets(RelativeCellPatternType.Diagonal, 1);

        Assert.AreEqual(4, offsets.Count);
        Assert.Contains(new Vector2Int(1, 1), offsets);
        Assert.Contains(new Vector2Int(1, -1), offsets);
        Assert.Contains(new Vector2Int(-1, 1), offsets);
        Assert.Contains(new Vector2Int(-1, -1), offsets);
    }

    [Test]
    public void GetOffsets_Diagonal_Range2_ExtendsAlongEachDiagonal()
    {
        List<Vector2Int> offsets = RelativeCellPattern.GetOffsets(RelativeCellPatternType.Diagonal, 2);

        Assert.AreEqual(8, offsets.Count);
        Assert.Contains(new Vector2Int(2, 2), offsets);
        Assert.Contains(new Vector2Int(-2, 2), offsets);
    }

    [Test]
    public void GetOffsets_Row_Range1_ReturnsLeftAndRightNeighbour()
    {
        List<Vector2Int> offsets = RelativeCellPattern.GetOffsets(RelativeCellPatternType.Row, 1);

        Assert.AreEqual(2, offsets.Count);
        Assert.Contains(new Vector2Int(1, 0), offsets);
        Assert.Contains(new Vector2Int(-1, 0), offsets);
    }

    [Test]
    public void GetOffsets_Row_Range3_ExtendsBothWays()
    {
        List<Vector2Int> offsets = RelativeCellPattern.GetOffsets(RelativeCellPatternType.Row, 3);

        Assert.AreEqual(6, offsets.Count);
        Assert.Contains(new Vector2Int(3, 0), offsets);
        Assert.Contains(new Vector2Int(-3, 0), offsets);
        CollectionAssert.DoesNotContain(offsets, new Vector2Int(0, 1));
    }

    [Test]
    public void GetOffsets_Column_Range1_ReturnsUpAndDownNeighbour()
    {
        List<Vector2Int> offsets = RelativeCellPattern.GetOffsets(RelativeCellPatternType.Column, 1);

        Assert.AreEqual(2, offsets.Count);
        Assert.Contains(new Vector2Int(0, 1), offsets);
        Assert.Contains(new Vector2Int(0, -1), offsets);
    }

    [Test]
    public void GetOffsets_Line_Range1_CombinesRowColumnAndDiagonal()
    {
        List<Vector2Int> offsets = RelativeCellPattern.GetOffsets(RelativeCellPatternType.Line, 1);

        Assert.AreEqual(8, offsets.Count);
        Assert.Contains(new Vector2Int(1, 0), offsets);
        Assert.Contains(new Vector2Int(0, 1), offsets);
        Assert.Contains(new Vector2Int(1, 1), offsets);
    }

    [Test]
    public void GetOffsets_Line_Range2_HasNoDuplicates()
    {
        List<Vector2Int> offsets = RelativeCellPattern.GetOffsets(RelativeCellPatternType.Line, 2);

        Assert.AreEqual(16, offsets.Count);
        CollectionAssert.AllItemsAreUnique(offsets);
    }
}

}
