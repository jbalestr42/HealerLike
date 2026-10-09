using System.Collections.Generic;
using Oisif.Editor;
using NUnit.Framework;

namespace Oisif.Editor.Tests
{
    public class GridGUITests
    {
        [Test]
        public void GetIndex_ColumnAfterColumn()
        {
            Assert.AreEqual(0, GridGUI.GetIndex(0, 0, 3));
            Assert.AreEqual(2, GridGUI.GetIndex(0, 2, 3));
            Assert.AreEqual(4, GridGUI.GetIndex(1, 1, 3));
        }

        // A 2x3 grid holding its own coordinates
        static List<string> CreateGrid()
        {
            List<string> cells = new List<string>();
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    cells.Add($"{x},{y}");
                }
            }
            return cells;
        }

        [Test]
        public void Resize_Bigger_KeepsEachCellAtItsPlace_TheNewOnesEmpty()
        {
            List<string> resized = GridGUI.Resize(CreateGrid(), 2, 3, 3, 4);

            Assert.AreEqual(12, resized.Count);
            Assert.AreEqual("1,2", resized[GridGUI.GetIndex(1, 2, 4)]);
            Assert.AreEqual("0,0", resized[GridGUI.GetIndex(0, 0, 4)]);
            Assert.IsNull(resized[GridGUI.GetIndex(0, 3, 4)]);
            Assert.IsNull(resized[GridGUI.GetIndex(2, 0, 4)]);
        }

        [Test]
        public void Resize_Smaller_DropsTheCellsOutOfTheGrid()
        {
            List<string> resized = GridGUI.Resize(CreateGrid(), 2, 3, 1, 2);

            CollectionAssert.AreEqual(new[] { "0,0", "0,1" }, resized);
        }

        [Test]
        public void Resize_ListShorterThanTheGrid_MissingCellsEmpty()
        {
            List<string> resized = GridGUI.Resize(new List<string> { "0,0" }, 2, 3, 2, 3);

            Assert.AreEqual("0,0", resized[0]);
            Assert.IsNull(resized[5]);
        }

        [Test]
        public void Resize_NegativeSize_EmptyGrid()
        {
            Assert.IsEmpty(GridGUI.Resize(CreateGrid(), 2, 3, -1, 3));
        }
    }
}
