using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Balance
{

public class TableSortTests
{
    class Row
    {
        public string name;
        public float? threat;
    }

    static List<string> Names(List<Row> rows)
    {
        return rows.ConvertAll(row => row.name);
    }

    static IComparable Threat(Row row)
    {
        return row.threat;
    }

    static readonly List<Row> Rows = new List<Row>
    {
        new Row { name = "B", threat = 300f },
        new Row { name = "Unmeasured", threat = null },
        new Row { name = "A", threat = 100f },
        new Row { name = "C", threat = 200f },
    };

    [Test]
    public void Ascending_SortsFromTheLowestValue_MissingValuesLast()
    {
        CollectionAssert.AreEqual(new[] { "A", "C", "B", "Unmeasured" }, Names(TableSort.Sort(Rows, Threat, true)));
    }

    [Test]
    public void Descending_SortsFromTheHighestValue_MissingValuesStillLast()
    {
        CollectionAssert.AreEqual(new[] { "B", "C", "A", "Unmeasured" }, Names(TableSort.Sort(Rows, Threat, false)));
    }

    [Test]
    public void Strings_AreSortedAlphabetically()
    {
        CollectionAssert.AreEqual(new[] { "A", "B", "C", "Unmeasured" }, Names(TableSort.Sort(Rows, row => row.name, true)));
    }

    [Test]
    public void EqualValues_KeepTheirOrder_InBothDirections()
    {
        List<Row> rows = new List<Row>
        {
            new Row { name = "First", threat = 100f },
            new Row { name = "Second", threat = 100f },
            new Row { name = "Third", threat = 100f },
        };

        CollectionAssert.AreEqual(new[] { "First", "Second", "Third" }, Names(TableSort.Sort(rows, Threat, true)));
        CollectionAssert.AreEqual(new[] { "First", "Second", "Third" }, Names(TableSort.Sort(rows, Threat, false)));
    }

    [Test]
    public void TheRowsGiven_AreNotChanged()
    {
        List<Row> rows = new List<Row>(Rows);

        TableSort.Sort(rows, Threat, true);

        CollectionAssert.AreEqual(Names(Rows), Names(rows));
    }
}

}
