using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Oisif.Editor.Tests
{
    public class ListDragTests
    {
        // Three elements of 20 pixels, one under the other from y = 0
        static readonly List<Rect> Rects = new List<Rect>
        {
            new Rect(0f, 0f, 100f, 20f),
            new Rect(0f, 20f, 100f, 20f),
            new Rect(0f, 40f, 100f, 20f),
        };

        [Test]
        public void GetDropSlot_AboveTheMiddleOfTheFirst_BeforeIt()
        {
            Assert.AreEqual(0, ListDrag.GetDropSlot(Rects, 5f));
            Assert.AreEqual(0, ListDrag.GetDropSlot(Rects, -30f));
        }

        [Test]
        public void GetDropSlot_BetweenTwoMiddles_BetweenTheElements()
        {
            Assert.AreEqual(1, ListDrag.GetDropSlot(Rects, 15f));
            Assert.AreEqual(1, ListDrag.GetDropSlot(Rects, 25f));
            Assert.AreEqual(2, ListDrag.GetDropSlot(Rects, 45f));
        }

        [Test]
        public void GetDropSlot_BelowTheMiddleOfTheLast_AfterIt()
        {
            Assert.AreEqual(3, ListDrag.GetDropSlot(Rects, 55f));
            Assert.AreEqual(3, ListDrag.GetDropSlot(Rects, 500f));
        }

        [Test]
        public void GetDropSlot_NoElement_First()
        {
            Assert.AreEqual(0, ListDrag.GetDropSlot(new List<Rect>(), 10f));
        }

        [Test]
        public void GetDropIndex_SlotBefore_TakesTheSlot()
        {
            Assert.AreEqual(0, ListDrag.GetDropIndex(2, 0));
            Assert.AreEqual(1, ListDrag.GetDropIndex(2, 1));
        }

        [Test]
        public void GetDropIndex_SlotAfter_OneLessAsTheElementLeavesItsPlace()
        {
            // The first of three dropped after the last one ends last
            Assert.AreEqual(2, ListDrag.GetDropIndex(0, 3));
            Assert.AreEqual(1, ListDrag.GetDropIndex(0, 2));
        }

        [Test]
        public void GetDropIndex_SlotsAroundItself_DoesNotMove()
        {
            Assert.AreEqual(1, ListDrag.GetDropIndex(1, 1));
            Assert.AreEqual(1, ListDrag.GetDropIndex(1, 2));
        }
    }
}
