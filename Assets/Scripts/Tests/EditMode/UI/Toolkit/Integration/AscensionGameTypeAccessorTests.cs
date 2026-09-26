using NUnit.Framework;
using UnityEngine;

namespace UI.Toolkit.Integration
{
    public class AscensionGameTypeAccessorTests : UiAccessorFixture
    {
        [Test]
        public void State_AuthoritativeFieldChanges_ReadsCurrentValue()
        {
            AscensionGameType owner = root.AddComponent<AscensionGameType>();
            Assert.AreEqual(AscensionGameType.State.None, owner.state);

            TestHelpers.SetPrivateField(owner, "_state", AscensionGameType.State.SelectRoom);

            Assert.AreEqual(AscensionGameType.State.SelectRoom, owner.state);
        }
    }
}
