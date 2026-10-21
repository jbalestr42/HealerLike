using NUnit.Framework;
using UnityEngine;

namespace UI.Toolkit.Integration
{
    public class GameManagerAccessorTests : UiAccessorFixture
    {
        [Test]
        public void State_AuthoritativeFieldChanges_ReadsCurrentValue()
        {
            GameManager owner = root.AddComponent<GameManager>();
            Assert.AreEqual(GameManager.GameState.None, owner.state);

            TestHelpers.SetPrivateField(owner, "_state", GameManager.GameState.Running);

            Assert.AreEqual(GameManager.GameState.Running, owner.state);
        }
    }
}
