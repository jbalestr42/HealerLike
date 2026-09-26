using NUnit.Framework;
using UnityEngine;

namespace UI.Toolkit.Integration
{
    public class UIManagerAccessorTests : UiAccessorFixture
    {
        [Test]
        public void State_AuthoritativeFieldChanges_ReadsCurrentValue()
        {
            UIManager owner = root.AddComponent<UIManager>();
            Assert.AreEqual(ViewType.None, owner.currentView);

            TestHelpers.SetPrivateField(owner, "_currentView", ViewType.Map);

            Assert.AreEqual(ViewType.Map, owner.currentView);
        }
    }
}
