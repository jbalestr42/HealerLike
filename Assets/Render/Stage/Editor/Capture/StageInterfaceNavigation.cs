using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;
using static HealerLike.Render.Stage.AStageRun;
using UnityEngine.SceneManagement;

namespace HealerLike.Render.Stage
{
    public class StageInterfaceNavigation
    {
        readonly StageCaptureSession _session;
        public StageInterfaceNavigation(StageCaptureSession session)
        {
            _session = session;
        }

        public IEnumerator LandscapeControls()
        {
            yield return _session.Resize(844, 390);
            _session.actions.Submit("party-button");
            yield return Wait(0.3f);
            yield return _session.Capture("09b-landscape-party");
            _session.actions.Submit("party-close-button");
            yield return Wait(0.2f);
            _session.actions.Submit("detail-button");
            yield return Wait(0.3f);
            yield return _session.Capture("09c-landscape-details");
            _session.actions.Submit("detail-close-button");
            yield return Wait(0.2f);
            _session.actions.Submit("pause-button");
            yield return Wait(0.3f);
            yield return _session.Capture("09d-landscape-pause");
            _session.actions.Submit("resume-button");
            yield return _session.Resize(1080, 1920);
        }

        public IEnumerator Navigation()
        {
            RenderManager original = _session.manager;
            _session.actions.Submit("pause-button");
            yield return Wait(0.2f);
            _session.actions.Submit("menu-button");
            yield return Wait(1f);
            _session.mapFixture.Dispose();
            _session.mapFixture = null;
            _session.AttachInput();
            _session.output.Check(SceneManager.GetActiveScene().path == StageInterface.MenuPath,
                "Pause menu returns to Toolkit menu");
            yield return _session.Capture("11-menu");
            yield return _session.actions.PointerTap("start-button");
            yield return Wait(1.5f);
            _session.AttachInput();
            _session.output.Check(Object.FindAnyObjectByType<RenderManager>() == original
                && original.entityManager != null, "New expedition reuses attached RenderManager");
            _session.output.Check(Object.FindObjectsByType<ToolkitGameUI>().Length == 1,
                "New expedition has exactly one Toolkit host");
            _session.mapFixture = new StageMapFixture(Object.FindAnyObjectByType<AscensionGameType>(), false);
            yield return _session.actions.PointerTap("start-button");
            yield return Wait(0.8f);
            yield return StageMapActions.SelectFirst(_session.actions, true);
            _session.output.Check(_session.actions.legacyModuleReadTouches,
                "New expedition uses the selected legacy module for touch input");
            yield return _session.Capture("12-new-expedition");
        }
    }
}
