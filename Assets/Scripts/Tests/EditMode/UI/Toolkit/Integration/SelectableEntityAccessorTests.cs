using NUnit.Framework;
using UnityEngine;

namespace UI.Toolkit.Integration
{
    public class SelectableEntityAccessorTests : UiAccessorFixture
    {
        [Test]
        public void Highlight_AbsentOutline_HasNoPresentation()
        {
            SelectableEntity selectable = root.AddComponent<SelectableEntity>();

            Assert.IsFalse(selectable.isHighlighted);
            Assert.AreEqual(Color.clear, selectable.highlightColor);
            Assert.AreEqual(0f, selectable.highlightWidth);
        }

        [Test]
        public void Highlight_OutlineChanges_AreReadWithoutAdditionalState()
        {
            SelectableEntity selectable = root.AddComponent<SelectableEntity>();
            TestHelpers.WithLoggingDisabled(() => root.AddComponent<Entity>());
            TestHelpers.InvokePrivate(selectable, "Start");
            System.Reflection.FieldInfo field = typeof(SelectableEntity).GetField(
                "_outline", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Behaviour outline = (Behaviour)field.GetValue(selectable);
            outline.enabled = true;
            outline.GetType().GetProperty("OutlineColor").SetValue(outline, Color.cyan);
            outline.GetType().GetProperty("OutlineWidth").SetValue(outline, 3f);
            Assert.IsFalse(selectable.isHighlighted);

            root.SetActive(true);

            Assert.IsTrue(selectable.isHighlighted);
            Assert.AreEqual(Color.cyan, selectable.highlightColor);
            Assert.AreEqual(3f, selectable.highlightWidth);
            outline.GetType().GetProperty("OutlineColor").SetValue(outline, Color.magenta);
            outline.GetType().GetProperty("OutlineWidth").SetValue(outline, 5f);
            outline.enabled = false;
            Assert.IsFalse(selectable.isHighlighted);
            Assert.AreEqual(Color.magenta, selectable.highlightColor);
            Assert.AreEqual(5f, selectable.highlightWidth);
        }
    }
}
