using NUnit.Framework;
using UnityEngine;

namespace Oisif.Editor.Tests
{
    // The simple rows of a long list are drawn only when at least partly on screen
    public class ListCullingTests
    {
        static readonly Rect Visible = new Rect(0f, 900f, 300f, 300f);

        [Test]
        public void IsRowVisible_InsideTheVisiblePart_True()
        {
            Assert.IsTrue(AttributeEditor.IsRowVisible(new Rect(0f, 1000f, 300f, 23f), Visible));
        }

        [Test]
        public void IsRowVisible_PartlyAboveOrUnder_True()
        {
            Assert.IsTrue(AttributeEditor.IsRowVisible(new Rect(0f, 890f, 300f, 23f), Visible));
            Assert.IsTrue(AttributeEditor.IsRowVisible(new Rect(0f, 1190f, 300f, 23f), Visible));
        }

        [Test]
        public void IsRowVisible_AboveOrUnder_False()
        {
            Assert.IsFalse(AttributeEditor.IsRowVisible(new Rect(0f, 800f, 300f, 23f), Visible));
            Assert.IsFalse(AttributeEditor.IsRowVisible(new Rect(0f, 1250f, 300f, 23f), Visible));
        }
    }
}
