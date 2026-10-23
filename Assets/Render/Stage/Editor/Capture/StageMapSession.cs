using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;
using static HealerLike.Render.Stage.AStageRun;
using UnityEngine.SceneManagement;

namespace HealerLike.Render.Stage
{
    public class StageMapSession : StageCaptureSession
    {
        public readonly StageMapRun.Manifest manifest;
        public AscensionGameType ascension { get; private set; }

        public StageMapSession(RenderManager manager, StageInterfaceOutput output,
            StageMapRun.Manifest manifest) : base(manager, output)
        {
            this.manifest = manifest;
        }

        public void Init()
        {
            AscensionGameType.OnRoomSelected.AddListener(OnRoomSelected);
            AscensionGameType.OnRoundStart.AddListener(OnRoundStart);
            AscensionGameType.OnBattleStart.AddListener(OnBattleStart);
        }

        public void Attach()
        {
            AttachInput();
            output.Check(actions.ui != null && Object.FindObjectsByType<ToolkitGameUI>().Length == 1,
                "Exactly one live Toolkit host");
            ascension = Object.FindAnyObjectByType<AscensionGameType>();
        }

        public IEnumerator NewExpedition()
        {
            AscensionGameType oldRun = ascension;
            RenderManager original = manager;
            yield return actions.PointerTap("pause-button");
            yield return Wait(0.2f);
            yield return actions.PointerTap("menu-button");
            yield return Wait(1f);
            mapFixture.Dispose();
            mapFixture = null;
            Attach();
            output.Check(oldRun == null && SceneManager.GetActiveScene().path == StageInterface.MenuPath,
                "Menu destroys the previous gameplay run and scene");
            yield return Capture("04-menu");
            yield return actions.StartExpedition();
            yield return Wait(1.2f);
            Attach();
            output.Check(manager == original && ascension != null && ascension.run != null,
                "New expedition reuses its RenderManager with a fresh gameplay map");
            output.Check(Object.FindObjectsByType<GameManager>().Length == 1,
                "Scene restart leaves one game manager");
            yield return Resize(1080, 1920);
        }

        void OnRoomSelected(MapNode node)
        {
            manifest.roomSelections++;
        }

        void OnRoundStart()
        {
            manifest.roundsStarted++;
        }

        void OnBattleStart()
        {
            manifest.battlesStarted++;
        }

        public override void Dispose()
        {
            AscensionGameType.OnRoomSelected.RemoveListener(OnRoomSelected);
            AscensionGameType.OnRoundStart.RemoveListener(OnRoundStart);
            AscensionGameType.OnBattleStart.RemoveListener(OnBattleStart);
            base.Dispose();
        }
    }
}
