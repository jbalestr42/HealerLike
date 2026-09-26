using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Stage
{
    public sealed class StageCompactRun : AStageRun
    {
        readonly StageInterfaceOutput _output = new StageInterfaceOutput(StagePlay.CaptureFolder);
        StageCaptureSession _session;
        StageCompactGestures _gestures;
        protected override bool shouldStartGame => false;
        protected override void OnFailed(Exception error) { _output.Fail(error.ToString()); base.OnFailed(error); }
        void Observe(string name) { _output.ObserveGameplay(name, _manager, _session.actions.touch); }
        int Count => _manager.entityManager.GetEntities(Entity.EntityType.Player).Count;
        Button Available => _session.actions.Cards("party-list").First(b => (b.userData as ToolkitCardModel)?.canDrag == true);
        protected override IEnumerator Run()
        {
            bool passed = false;
            try
            {
                _session = new StageCaptureSession(_manager, _output, StageCaptureTheme.TakeSelection());
                _session.AttachInput(); _gestures = new StageCompactGestures(_session);
                yield return _session.Resize(1080, 1920);
                _session.mapFixture = new StageMapFixture(Object.FindAnyObjectByType<AscensionGameType>(), false);
                UnityEngine.Random.InitState(271828);
                yield return _session.actions.PointerTap("start-button");
                yield return Wait(.8f);
                yield return StageMapActions.SelectFirst(_session.actions, true);
                yield return Wait(.5f);
                yield return _session.Capture("01-preparation");
                var root = _session.actions.root;
                _output.Check(_session.actions.legacyModuleReadTouches, "Actual input module consumed synthetic touch frames");
                _output.Check(root.Q("party-panel").worldBound.yMax <= root.Q("command-dock").worldBound.yMin + 1,
                    "Party is immediately above spells");
                _output.Check(root.Q("command-dock").worldBound.height + root.Q("party-panel").worldBound.height <= 180,
                    "Both compact rows occupy at most 180 logical pixels");
                int initial = Count;
                Observe("initial");
                float gold = _manager.player.gold;
                float mana = _manager.player.character.mana.Value;
                yield return _gestures.Scroll(Available);
                Observe("horizontal-scroll");
                _output.Check(Count == initial && _session.interaction.GetInteraction() == null,
                    "Horizontal scroll then upward movement produces zero entities and no placement");
                yield return _gestures.Hold(Available, "02-roster-details", true);
                Observe("roster-hold");
                _output.Check(Count == initial, "Creature hold produces zero entities");
                yield return _session.actions.PointerTap("detail-close-button");
                Vector3 cell = _manager.player.grid.GetNearestWalkablePosition(Vector3.left * 2);
                Vector2 drop = _gestures.DropPoint(cell);
                yield return _gestures.Drag(Available, new Vector2(4, Screen.height - 4), "03-invalid-placement");
                Observe("invalid-drop");
                _output.Check(Count == initial && _session.interaction.GetInteraction() == null,
                    "Invalid drop produces no entity and clears interaction");
                yield return _gestures.Drag(Available, drop, "04-cancelled-placement", TouchPhase.Canceled);
                Observe("cancelled-drop");
                _output.Check(Count == initial && _session.interaction.GetInteraction() == null,
                    "Cancelled valid placement produces no entity and clears preview");
                yield return _session.Capture("04b-after-cancel");
                _output.Check(gold == _manager.player.gold && mana == _manager.player.character.mana.Value,
                    "Scroll, hold, invalid drop and cancellation spend no resources");
                Button entry = Available;
                string key = ((ToolkitCardModel)entry.userData).key;
                yield return _gestures.Drag(entry, drop, "05-held-placement");
                Observe("valid-drop");
                _output.Check(Count == initial + 1, "Valid release plus duplicate Ended produces exactly one actual creature");
                _output.Check(_session.interaction.GetInteraction() == null && !_session.actions.touch.roster.active,
                    "Successful release clears placement ownership");
                _output.Check(((ToolkitCardModel)entry.userData).key == key && ((ToolkitCardModel)entry.userData).source is Entity,
                    "Deployed creature retains its actual roster entry and identity");
                yield return _session.Capture("06-successful-release");
                yield return _gestures.Hold(entry, "07-deployed-details", true);
                _output.Check(root.Q<Button>("detail-inventory-button").enabledSelf, "Deployed detail retains equipment access");
                yield return _session.actions.PointerTap("detail-inventory-button");
                yield return Wait(.2f);
                yield return _session.Capture("08-equipment");
                yield return _session.actions.PointerTap("inventory-close-button");
                yield return Wait(.2f);
                yield return _session.actions.PointerTap("map-button");
                yield return Wait(.2f);
                yield return _session.Capture("09-map");
                yield return _session.actions.PointerTap("map-close-button");
                yield return Wait(.2f);
                for (int i = 0; i < 2; i++)
                {
                    cell = _manager.player.grid.GetNearestWalkablePosition(new Vector3(-3, 0, i * 2));
                    yield return _gestures.Drag(Available, _gestures.DropPoint(cell), null);
                }
                yield return new StageCompactSpells(_session, _gestures).Run();
                yield return _session.Resize(1170, 2532);
                _session.actions.ui.safeAreaProvider = () => new Rect(0, 34f / 844, 1, 1 - 78f / 844);
                yield return Wait(.3f);
                yield return _session.Capture("16-tall-safe-area");
                _session.actions.ui.safeAreaProvider = null;
                yield return _session.Resize(844, 390);
                yield return _session.Capture("17-landscape");
                yield return _session.actions.PointerTap("pause-button");
                yield return Wait(.2f);
                yield return _session.Capture("18-landscape-pause");
                yield return _session.actions.PointerTap("focus-button");
                _output.Check(Time.timeScale == 0, "Overview control remains usable inside Pause without resuming");
                yield return _session.actions.PointerTap("resume-button");
                yield return _session.Resize(1080, 1920);
                yield return new StageInterfaceNavigation(_session).Navigation();
                passed = true;
            }
            finally { _session?.Dispose(); _output.Write(passed); StagePlay.Finish(this, passed); }
        }
    }
}
