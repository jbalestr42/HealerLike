using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;
using static HealerLike.Render.Stage.AStageRun;
using HealerLike.Render.Creatures;
using UnityEngine.SceneManagement;

namespace HealerLike.Render.Stage
{
    public class StagePresentationNavigation
    {
        readonly StageCaptureSession _session;
        readonly StagePresentationOutput _output;
        readonly StagePresentationPlacement _placement;
        readonly StagePresentationPortraits _portraits;
        public StagePresentationNavigation(StageCaptureSession session, StagePresentationOutput output,
            StagePresentationPlacement placement, StagePresentationPortraits portraits)
        {
            _session = session;
            _output = output;
            _placement = placement;
            _portraits = portraits;
        }

        public IEnumerator Navigation()
        {
            yield return Wait(0.3f);
            yield return _placement.Select(0);
            Transform preview = _session.manager.placement.preview.rig.root;
            StageInterface attachment = _session.manager.GetComponent<StageInterface>();
            CreaturePortraits portraits = attachment.portraits;
            Texture2D texture = portraits.GetCreatureIcon(_placement.selectedData, Entity.EntityType.Player);
            _session.actions.Submit("pause-button");
            yield return Wait(0.2f);
            _session.actions.Submit("menu-button");
            yield return Wait(1f);
            _output.Check(preview == null && portraits.isDisposed && texture == null,
                "Scene exit disposes placement preview and portrait textures");
            _output.Check(_portraits.PortraitCameras().Count == 0,
                "Scene exit destroys every owned portrait capture camera");
            _session.mapFixture.Dispose();
            _session.mapFixture = null;
            _session.AttachInput();
            _output.Check(SceneManager.GetActiveScene().path == StageInterface.MenuPath,
                "Pause menu reaches Toolkit menu");
            yield return _session.Capture("04-menu");
            yield return _session.actions.PointerTap("start-button");
            yield return Wait(1.2f);
            _session.AttachInput();
            _output.Check(Object.FindAnyObjectByType<RenderManager>() == _session.manager,
                "New expedition retains the single original RenderManager");
            _session.mapFixture = new StageMapFixture(Object.FindAnyObjectByType<AscensionGameType>(), false);
            yield return _session.actions.PointerTap("start-button");
            yield return Wait(0.8f);
            yield return StageMapActions.SelectFirst(_session.actions, true);
            yield return _portraits.Portraits("05-new-expedition");
        }
    }
}
