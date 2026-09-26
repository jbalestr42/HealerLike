using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using static HealerLike.Render.Stage.AStageRun;

namespace HealerLike.Render.Stage
{
    public class StageInterfaceRun : AStageRun
    {
        readonly StageInterfaceOutput _output = new StageInterfaceOutput();
        StageCaptureSession _session;
        protected override bool shouldStartGame
        {
            get
            {
                return false;
            }
        }

        protected override void OnFailed(System.Exception error)
        {
            _output.Fail(error.Message);
            base.OnFailed(error);
        }

        protected override IEnumerator Run()
        {
            bool passed = false;
            try
            {
                _output.Check(!string.IsNullOrWhiteSpace(_output.manifest.revision)
                    && _output.manifest.revision != "unspecified", "Capture source revision recorded");
                _session = new StageCaptureSession(_manager, _output);
                _session.AttachInput();
                StageInterfaceDeployment deployment = new StageInterfaceDeployment(_session);
                StageInterfaceEncounter encounter = new StageInterfaceEncounter(_session);
                StageInterfaceNavigation navigation = new StageInterfaceNavigation(_session);
                yield return _session.Resize(1080, 1920);
                _output.Check(_session.actions.ui != null, "RenderStage attached Toolkit interface");
                _output.Check(Object.FindObjectsByType<ToolkitGameUI>().Length == 1, "Exactly one Toolkit host");
                yield return _session.Capture("01-journey");
                UnityEngine.Random.InitState(271828);
                _session.mapFixture = new StageMapFixture(Object.FindAnyObjectByType<AscensionGameType>(), false);
                _output.manifest.checks.Add("Authored map settings retained; capture-only map seed fixed to 271828");
                yield return _session.actions.PointerTap("start-button");
                yield return Wait(1f);
                _output.Check(_session.actions.legacyModuleReadTouches,
                    "Selected StandaloneInputModule consumed held touch samples for Begin journey");
                _output.Check(
                    LegacyUiReader.GameState(Object.FindAnyObjectByType<GameManager>())
                    == GameManager.GameState.Running, "Toolkit begin journey started real gameplay");
                yield return StageMapActions.SelectFirst(_session.actions, true);
                _output.manifest.checks.Add("First map room selected through actual multi-frame Toolkit touch input");
                yield return _session.actions.PointerTap("party-button");
                yield return Wait(0.3f);
                yield return _session.Capture("02-party");
                List<Button> cards = _session.actions.Cards("party-list");
                _output.Check(cards.Count > 0, "Party has deploy choices");
                yield return _session.actions.SelectCard(cards[0].parent.Q<Button>("card-info"));
                yield return Wait(0.3f);
                _output.Check(_session.interaction.GetInteraction() == null, "Info opens details without deploying");
                yield return _session.Capture("03-details");
                _session.actions.Submit("detail-close-button");
                yield return Wait(0.2f);
                _session.actions.Submit("party-button");
                yield return Wait(0.2f);
                yield return _session.actions.SelectCardByTouch(_session.actions.Cards("party-list")[0]);
                yield return Wait(0.3f);
                _output.Check(_session.interaction.GetInteraction() != null, "Toolkit party card begins deployment");
                yield return _session.Capture("04-targeting");
                yield return _session.actions.PointerTap("cancel-button");
                yield return Wait(0.2f);
                _output.Check(_session.interaction.GetInteraction() == null, "Touch cancel ends deployment");
                yield return deployment.Deploy(0, Vector3.left * 2f);
                yield return deployment.Deploy(1, Vector3.left * 3f + Vector3.back);
                _output.manifest.checks.Add("Deployment uses multi-frame StandaloneInputModule and "
                    + "StageTouchInput.Update; Android OS input is not covered by this capture");
                _session.actions.Submit("inventory-button");
                yield return Wait(0.3f);
                _output.Check(StageInterfaceOutput.IsVisible(_session.actions.root.Q("inventory-panel")),
                    "Inventory opens");
                yield return _session.Capture("05-inventory");
                yield return _session.Resize(1170, 2532);
                _session.actions.ui.safeAreaProvider = () => new Rect(0f, 34f / 844f, 1f, 1f - 78f / 844f);
                yield return Wait(0.4f);
                yield return _session.Capture("05b-simulated-notch-inventory");
                _session.actions.ui.safeAreaProvider = null;
                yield return _session.Resize(1080, 1920);
                _session.actions.Submit("inventory-close-button");
                yield return Wait(0.3f);
                _output.Check(!StageInterfaceOutput.IsVisible(_session.actions.root.Q("inventory-panel")),
                    "Inventory closes");
                yield return deployment.GestureExclusion();
                yield return deployment.Deploy(1, Vector3.left * 2f + Vector3.forward * 2f);
                yield return navigation.LandscapeControls();
                _session.actions.Submit("wave-button");
                yield return Wait(2f);
                _output.Check(!_session.actions.root.Q<Button>("wave-button").enabledSelf, "Toolkit begins encounter");
                yield return _session.Capture("06-battle");
                _session.actions.Submit("pause-button");
                yield return Wait(0.3f);
                _output.Check(Time.timeScale == 0f, "Pause freezes gameplay");
                yield return _session.Capture("07-pause");
                _session.actions.Submit("resume-button");
                yield return Wait(0.2f);
                _output.Check(Time.timeScale > 0f, "Resume restores gameplay");
                yield return encounter.SpellTap();
                yield return _session.Resize(1170, 2532);
                yield return _session.Capture("08-tall-phone");
                _session.actions.ui.safeAreaProvider = () => new Rect(0f, 34f / 844f, 1f, 1f - 78f / 844f);
                _session.actions.Submit("pause-button");
                yield return Wait(0.4f);
                yield return _session.Capture("08b-simulated-notch-pause");
                _session.actions.Submit("resume-button");
                _session.actions.ui.safeAreaProvider = null;
                yield return Wait(0.2f);
                yield return _session.Resize(844, 390);
                yield return _session.Capture("09-phone-landscape");
                yield return _session.Resize(1080, 1920);
                yield return encounter.Reward();
                yield return navigation.Navigation();
                _output.Check(_session.actions.scrollActions > 0,
                    "Scrolled cards below the fold before activating reachable controls");
                passed = true;
            }
            finally
            {
                if (_session != null)
                {
                    _session.Dispose();
                }

                _output.Write(passed);
                StagePlay.Finish(this, passed);
            }
        }
    }
}
