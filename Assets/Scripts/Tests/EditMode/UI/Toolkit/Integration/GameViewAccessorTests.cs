using NUnit.Framework;
using UnityEngine;

namespace UI.Toolkit.Integration
{
    public class GameViewAccessorTests : UiAccessorFixture
    {
        [Test]
        public void Selection_GettersPreserveStoredObjectWhenPanelCloses()
        {
            GameView view = root.AddComponent<GameView>();
            TestHelpers.SetPrivateField(view, "_selectedObject", root);
            TestHelpers.SetPrivateField(view, "_selectedPanel", PanelType.Entity);
            Assert.AreEqual(PanelType.Entity, view.selectedPanel);
            Assert.AreSame(root, view.selectedObject);

            TestHelpers.SetPrivateField(view, "_selectedPanel", PanelType.None);

            Assert.AreEqual(PanelType.None, view.selectedPanel);
            Assert.AreSame(root, view.selectedObject);
        }
    }
}
