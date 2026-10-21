using UnityEditor;

namespace HealerLike.Render.Stage
{
    // Batchmode play sessions that write the look grammar's sheets and the silhouette overlap list:
    // -executeMethod HealerLike.Render.Stage.LookSheetCapture.All runs units, effects and the list in one session,
    // .Units and .Effects run one of them. The editor exits with the run's code.
    public static class LookSheetCapture
    {
        [MenuItem("Tools/Render/Capture Look Sheet")]
        public static void Units()
        {
            StagePlay.Enter("units", 600f);
        }

        [MenuItem("Tools/Render/Capture Effect Sheet")]
        public static void Effects()
        {
            StagePlay.Enter("effects", 600f);
        }

        [MenuItem("Tools/Render/Capture Every Look Sheet")]
        public static void All()
        {
            StagePlay.Enter("all", 1200f);
        }
    }
}
