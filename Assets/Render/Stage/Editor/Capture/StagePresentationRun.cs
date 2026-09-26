using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;
using static HealerLike.Render.Stage.AStageRun;
using System.Text.RegularExpressions;

namespace HealerLike.Render.Stage
{
    public class StagePresentationRun : AStageRun
    {
        readonly StagePresentationOutput _output = new StagePresentationOutput();
        StageCaptureSession _session;
        protected override bool shouldStartGame
        {
            get
            {
                return false;
            }
        }

        protected override void OnFailed(Exception error)
        {
            _output.Fail(error.ToString());
            base.OnFailed(error);
        }

        protected override IEnumerator Run()
        {
            bool passed = false;
            try
            {
                _output.Check(_output.manifest.revision != null
                    && Regex.IsMatch(_output.manifest.revision, "^[0-9a-fA-F]{40}$"),
                    "Exact 40-character capture revision supplied");
                _session = new StageCaptureSession(_manager, _output.ui);
                _session.AttachInput();
                StagePresentationPortraits portraits = new StagePresentationPortraits(_session, _output);
                StagePresentationGrowth growth = new StagePresentationGrowth(_session, _output);
                StagePresentationPlacement placement = new StagePresentationPlacement(_session, _output, growth);
                StagePresentationNavigation navigation = new StagePresentationNavigation(_session, _output,
                    placement, portraits);
                yield return _session.Resize(1080, 1920);
                UnityEngine.Random.InitState(271828);
                _session.mapFixture = new StageMapFixture(Object.FindAnyObjectByType<AscensionGameType>(), false);
                _output.manifest.interventions.Add("Authored map settings retained; capture-only map seed fixed to "
                    + "271828");
                yield return _session.actions.PointerTap("start-button");
                yield return Wait(0.8f);
                _output.Check(_session.actions.legacyModuleReadTouches,
                    "Begin journey consumed actual multi-frame legacy input samples");
                yield return StageMapActions.SelectFirst(_session.actions, true);
                _output.manifest.checks.Add("Entered first combat through actual multi-frame Toolkit map touch");
                yield return portraits.Portraits("01-portrait");
                yield return _session.Resize(844, 390);
                yield return portraits.Portraits("02-landscape");
                yield return _session.Resize(1080, 1920);
                yield return placement.Placement();
                yield return growth.StoneAppearance();
                yield return navigation.Navigation();
                yield return _output.VerifyImages();
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
