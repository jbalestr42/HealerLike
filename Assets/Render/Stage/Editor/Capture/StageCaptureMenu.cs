using UnityEditor;

namespace HealerLike.Render.Stage
{
    // Batchmode entry points: -executeMethod HealerLike.Render.Stage.StageCaptureMenu.Portrait, and so on
    public static class StageCaptureMenu
    {
        [MenuItem("Tools/Render/Capture Creature Presentation")]
        public static void CreaturePresentation()
        {
            StagePlay.Enter("creature-presentation", 360f);
        }

        [MenuItem("Tools/Render/Capture Mobile Interface")]
        public static void MobileInterface()
        {
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

        [MenuItem("Tools/Render/Smoke Three Rounds")]
        public static void Smoke()
        {
            StagePlay.Enter("smoke", 420f);
        }
    }
}
