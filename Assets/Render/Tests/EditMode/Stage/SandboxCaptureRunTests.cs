using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;

namespace HealerLike.Render.Stage
{
    public class SandboxCaptureRunTests
    {
        static readonly string SandboxDataPath = "Assets/Data/SandboxData.asset";

        [TearDown]
        public void TearDown()
        {
            StageTarget.Reset();
        }

        // The run arms each unit through the sandbox button labelled with its title, so every unit must be one the
        // sandbox offers
        [Test]
        public void Units_AreAllOfferedByTheSandbox()
        {
            SandboxData sandbox = AssetDatabase.LoadAssetAtPath<SandboxData>(SandboxDataPath);
            Assert.That(sandbox, Is.Not.Null);
            foreach (string path in SandboxCaptureRun.Units)
            {
                EntityData data = AssetDatabase.LoadAssetAtPath<EntityData>(path);
                Assert.That(data, Is.Not.Null, path);
                CollectionAssert.Contains(sandbox.entities, data, path);
            }
        }

        [Test]
        public void Units_HaveDistinctButtonTitles()
        {
            HashSet<string> titles = new HashSet<string>();
            foreach (string path in SandboxCaptureRun.Units)
            {
                EntityData data = AssetDatabase.LoadAssetAtPath<EntityData>(path);
                Assert.That(string.IsNullOrEmpty(data.title), Is.False, path);
                Assert.That(titles.Add(data.title), Is.True, "Two units share the title " + data.title);
            }
        }

        [Test]
        public void Units_IncludeJuliensNewPlayerUnits()
        {
            CollectionAssert.IsSubsetOf(new[]
            {
                "Assets/Data/Entities/ZealotEntity/ZealotEntity.asset",
                "Assets/Data/Entities/TreantEntity/TreantEntity.asset",
                "Assets/Data/Entities/GroveKeeperEntity/GroveKeeperEntity.asset",
                "Assets/Data/Entities/BloodCultistEntity/BloodCultistEntity.asset"
            }, SandboxCaptureRun.Units);
        }

        [Test]
        public void Modes_AreDistinctAndNamed()
        {
            Assert.That(string.IsNullOrEmpty(SandboxCaptureRun.BootMode), Is.False);
            Assert.That(string.IsNullOrEmpty(SandboxCaptureRun.MenuMode), Is.False);
            Assert.That(SandboxCaptureRun.BootMode, Is.Not.EqualTo(SandboxCaptureRun.MenuMode));
            Assert.That(string.IsNullOrEmpty(SandboxCaptureRun.BackMode), Is.False);
            Assert.That(SandboxCaptureRun.BackMode, Is.Not.EqualTo(SandboxCaptureRun.BootMode));
            Assert.That(SandboxCaptureRun.BackMode, Is.Not.EqualTo(SandboxCaptureRun.MenuMode));
        }

        // The back run starts in the sandbox like the boot run; the menu run boots Main and reaches it by the menu
        [Test]
        public void BootsSandbox_BootAndBackModesOnly()
        {
            Assert.That(SandboxCaptureRun.BootsSandbox(SandboxCaptureRun.BootMode), Is.True);
            Assert.That(SandboxCaptureRun.BootsSandbox(SandboxCaptureRun.BackMode), Is.True);
            Assert.That(SandboxCaptureRun.BootsSandbox(SandboxCaptureRun.MenuMode), Is.False);
            Assert.That(SandboxCaptureRun.BootsSandbox(""), Is.False);
            Assert.That(SandboxCaptureRun.BootsSandbox(null), Is.False);
        }

        // Outside a batchmode session the boot hook must leave the stage on its plain boot, the Toolkit menu
        [Test]
        public void BootHook_OutsideACaptureSession_LeavesThePlainMenuBoot()
        {
            Assume.That(StagePlay.Mode, Is.EqualTo(""));
            StageTarget.Reset();

            typeof(StagePlay).GetMethod("SelectBootTarget", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, null);

            Assert.That(StageTarget.isSelected, Is.False);
            Assert.That(StageTarget.scenePath, Is.EqualTo(StageTarget.MenuPath));
        }

        [Test]
        public void BootTarget_SandboxModes_BootTheSandbox_AndTheMenuModeBootsMain()
        {
            Assert.That(StagePlay.BootTarget(SandboxCaptureRun.BootMode), Is.EqualTo(StageTarget.SandboxPath));
            Assert.That(StagePlay.BootTarget(SandboxCaptureRun.BackMode), Is.EqualTo(StageTarget.SandboxPath));
            // The menu run reaches the sandbox from Main's HUD, as it always has
            Assert.That(StagePlay.BootTarget(SandboxCaptureRun.MenuMode), Is.EqualTo(StageTarget.MainPath));
        }
    }
}
