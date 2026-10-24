using NUnit.Framework;

namespace UI.Toolkit
{

    public class ToolkitGameContextTests
    {
        [Test]
        public void IsPreparing_NoAscension_ReturnsTrue()
        {
            ToolkitGameContext context = new ToolkitGameContext();
            Assert.IsTrue(context.IsPreparing());
        }

        [Test]
        public void IsCurrentView_Menu_ReturnsFalse()
        {
            ToolkitGameContext context = new ToolkitGameContext();
            context.isMenu = true;
            Assert.IsFalse(context.IsCurrentView(GameScreen.Map));
        }

        [Test]
        public void HasInteraction_NoInteractionManager_ReturnsFalse()
        {
            ToolkitGameContext context = new ToolkitGameContext();
            Assert.IsFalse(context.hasInteraction);
        }

        [TestCase(false, true)]
        [TestCase(true, false)]
        public void IsPreparing_Sandbox_WhileItsBattleIsStopped(bool isBattleRunning, bool isPreparing)
        {
            UnityEngine.GameObject host = new UnityEngine.GameObject("Sandbox");
            try
            {
                SandboxGameType sandbox = host.AddComponent<SandboxGameType>();
                TestHelpers.SetPrivateField(sandbox, "_isBattleRunning", isBattleRunning);
                ToolkitGameContext context = new ToolkitGameContext { sandbox = sandbox };

                Assert.AreEqual(isPreparing, context.IsPreparing());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }
    }
}
