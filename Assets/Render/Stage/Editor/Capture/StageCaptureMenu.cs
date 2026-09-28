using UnityEditor;

namespace HealerLike.Render.Stage
{
    // Batchmode entry points: -executeMethod HealerLike.Render.Stage.StageCaptureMenu.Portrait, and so on
    public static class StageCaptureMenu
    {
        [MenuItem("Tools/Render/Capture Expedition Map")]
        public static void ExpeditionMap()
        {
            StagePlay.Enter("expedition-map", 420f);
        }

        [MenuItem("Tools/Render/Capture Creature Presentation")]
        public static void CreaturePresentation()
        {
            StagePlay.Enter("creature-presentation", 360f);
        }

        [MenuItem("Tools/Render/Capture Mobile Interface")]
        public static void MobileInterface()
        {
            StageCaptureTheme.Prepare();
            StagePlay.Enter("mobile-interface", 360f);
        }

        [MenuItem("Tools/Render/Capture Ambient Filmstrip")]
        public static void Motion()
        {
            StagePlay.Enter("motion", 300f);
        }

        [MenuItem("Tools/Render/Capture Offscreen Player")]
        public static void OffscreenPlayer()
        {
            StagePlay.Enter("offscreen-player", 180f);
        }

        [MenuItem("Tools/Render/Capture Portrait")]
        public static void Portrait()
        {
            StagePlay.Enter("portrait", 180f);
        }

        [MenuItem("Tools/Render/Capture Landscape")]
        public static void Landscape()
        {
            StagePlay.Enter("landscape", 180f);
        }

        // The grass carpet alone under a fixed camera, across two repaints and a revoked zone snapshot
        [MenuItem("Tools/Render/Capture Ground Fixture")]
        public static void Ground()
        {
            StagePlay.Enter("ground", 180f);
        }

        // Whole-frame time of the running stage with its grass, logged by GrassBenchRun
        [MenuItem("Tools/Render/Grass Bench")]
        public static void GrassBench()
        {
            StagePlay.Enter("grassbench", 300f);
        }

        // A scripted walk, heal, launch and gust through the grass at a fixed 60 Hz, as a filmstrip and probes
        [MenuItem("Tools/Render/Grass Lab")]
        public static void GrassLab()
        {
            StagePlay.Enter("grasslab", 300f);
        }

        // A real wave through the game camera with every skill cast in turn, beside the ground from above
        [MenuItem("Tools/Render/Grass Battle")]
        public static void GrassBattle()
        {
            StagePlay.Enter("grassbattle", 240f);
        }

        // Frame to frame flicker of the grass on the board and in the environment, with the suspects switched off
        [MenuItem("Tools/Render/Grass Jitter")]
        public static void GrassJitter()
        {
            StagePlay.Enter("grassjitter", 240f);
        }

        // Julien's sandbox booted by the stage, placed and played through its own uGUI
        [MenuItem("Tools/Render/Capture Sandbox")]
        public static void Sandbox()
        {
            StagePlay.Enter(SandboxCaptureRun.BootMode, 240f);
        }

        // Main, then the Toolkit menu, then its Sandbox entry
        [MenuItem("Tools/Render/Capture Sandbox Through The Menu")]
        public static void SandboxThroughMenu()
        {
            StagePlay.Enter(SandboxCaptureRun.MenuMode, 240f);
        }

        // The sandbox, Pause, then the stage's Menu button back to the Toolkit menu and its Start
        [MenuItem("Tools/Render/Capture Sandbox Back To The Menu")]
        public static void SandboxBackToMenu()
        {
            StagePlay.Enter(SandboxCaptureRun.BackMode, 240f);
        }

        // Main, then the Toolkit menu, Start, the class screen, the Druid, and its first room
        [MenuItem("Tools/Render/Capture Class Select")]
        public static void ClassSelect()
        {
            StagePlay.Enter(ClassSelectCaptureRun.Mode, 240f);
        }

        // The plain boot: the Toolkit menu first, then Start, the class screen, the pick (RENDER_CLASS_PICK), its room
        [MenuItem("Tools/Render/Capture Menu Boot")]
        public static void MenuBoot()
        {
            StagePlay.Enter(ClassSelectCaptureRun.BootMode, 240f);
        }

        [MenuItem("Tools/Render/Smoke Three Rounds")]
        public static void Smoke()
        {
            StagePlay.Enter("smoke", 420f);
        }
    }
}
