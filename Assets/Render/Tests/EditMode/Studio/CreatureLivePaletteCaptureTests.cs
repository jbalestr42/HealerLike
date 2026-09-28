using System.Reflection;
using HealerLike.Render.Stage;
using NUnit.Framework;
using UnityEditor;

namespace HealerLike.Render.Studio
{
    // A plain boot opens the Toolkit menu; the live palette run plays Main, so its session selects it
    public class CreatureLivePaletteCaptureTests
    {
        static readonly string Key = "CreatureLivePaletteCapture.Running";

        [TearDown]
        public void TearDown()
        {
            StageTarget.Reset();
        }

        static void SelectBootTarget()
        {
            typeof(Editor.CreatureLivePaletteCapture)
                .GetMethod("SelectBootTarget", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        }

        [Test]
        public void BootHook_DuringItsSession_SelectsMain()
        {
            Assume.That(SessionState.GetBool(Key, false), Is.False);
            StageTarget.Reset();
            SessionState.SetBool(Key, true);
            try
            {
                SelectBootTarget();
            }
            finally
            {
                SessionState.EraseBool(Key);
            }

            Assert.That(StageTarget.scenePath, Is.EqualTo(StageTarget.MainPath));
        }

        [Test]
        public void BootHook_OutsideItsSession_LeavesThePlainMenuBoot()
        {
            Assume.That(SessionState.GetBool(Key, false), Is.False);
            StageTarget.Reset();

            SelectBootTarget();

            Assert.That(StageTarget.isSelected, Is.False);
            Assert.That(StageTarget.scenePath, Is.EqualTo(StageTarget.MenuPath));
        }
    }
}
