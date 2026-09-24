using UnityEditor;

namespace HealerLike.Render.Stage
{
    // Batchmode entry points: -executeMethod HealerLike.Render.Stage.StageCaptureMenu.Portrait, and so on
    public static class StageCaptureMenu
    {
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

        [MenuItem("Tools/Render/Smoke Three Rounds")]
        public static void Smoke()
        {
            StagePlay.Enter("smoke", 420f);
        }
    }
}
