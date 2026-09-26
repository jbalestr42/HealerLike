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
        public MapView mapView { get; private set; }

        public StageMapSession(RenderManager manager, StageInterfaceOutput output,
            StageMapRun.Manifest manifest) : base(manager, output)
        {
            this.manifest = manifest;
        }

        public void Init()
        {
            AscensionGameType.OnRoundStart.AddListener(OnRoundStart);
            AscensionGameType.OnBattleStart.AddListener(OnBattleStart);
        }

        public void Attach()
        {
            if (mapView != null)
            {
                mapView.OnNodeSelected.RemoveListener(OnRoomSelected);
            }

            AttachInput();
            output.Check(actions.ui != null && Object.FindObjectsByType<ToolkitGameUI>().Length == 1,
                "Exactly one live Toolkit host");
            ascension = Object.FindAnyObjectByType<AscensionGameType>();
            UIManager owner = Object.FindAnyObjectByType<UIManager>();
            mapView = owner != null ? owner.GetView<MapView>(ViewType.Map) : null;
            if (mapView != null)
            {
                mapView.OnNodeSelected.AddListener(OnRoomSelected);
            }
        }

        public IEnumerator NewExpedition()
        {
            MapView oldMap = mapView;
            oldMap.OnNodeSelected.RemoveListener(OnRoomSelected);
            RenderManager original = manager;
            yield return actions.PointerTap("pause-button");
            yield return Wait(0.2f);
            yield return actions.PointerTap("menu-button");
            yield return Wait(1f);
            mapFixture.Dispose();
            mapFixture = null;
            Attach();
            output.Check(oldMap == null && SceneManager.GetActiveScene().path == StageInterface.MenuPath,
                "Menu destroys the previous gameplay map and scene");
            yield return Capture("04-menu");
            yield return actions.PointerTap("start-button");
            yield return Wait(1.2f);
            Attach();
            output.Check(manager == original && ascension != null && mapView != null,
                "New expedition reuses its RenderManager with a fresh gameplay map");
            output.Check(Object.FindObjectsByType<UIManager>().Length == 1
                && Object.FindObjectsByType<GameManager>().Length == 1,
                "Scene restart leaves one UI manager and one game manager");
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
            AscensionGameType.OnRoundStart.RemoveListener(OnRoundStart);
            AscensionGameType.OnBattleStart.RemoveListener(OnBattleStart);
            if (mapView != null)
            {
                mapView.OnNodeSelected.RemoveListener(OnRoomSelected);
            }

            base.Dispose();
        }
    }
}
