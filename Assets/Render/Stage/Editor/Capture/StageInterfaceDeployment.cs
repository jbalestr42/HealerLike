using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static HealerLike.Render.Stage.AStageRun;

namespace HealerLike.Render.Stage
{
    public class StageInterfaceDeployment
    {
        readonly StageCaptureSession _session;
        public StageInterfaceDeployment(StageCaptureSession session)
        {
            _session = session;
        }

        public IEnumerator Deploy(int index, Vector3 offset)
        {
            yield return _session.actions.PointerTap("party-button");
            yield return Wait(0.2f);
            List<Button> cards = _session.actions.Cards("party-list").FindAll(button => button.enabledInHierarchy
                && button.Q<Label>("card-status").text == "Deploy");
            _session.output.Check(cards.Count > 0, "Deployable party choice remains");
            yield return _session.actions.SelectCardByTouch(cards[Mathf.Min(index, cards.Count - 1)]);
            yield return Wait(0.2f);
            _session.output.Check(_session.interaction.GetInteraction() is EntityGridInteraction,
                "Multi-frame Toolkit touch selects deployment card " + index);
            int before = _session.manager.entityManager.GetEntities(Entity.EntityType.Player).Count;
            Vector3 point = _session.manager.player.grid.GetNearestWalkablePosition(offset);
            Vector2 screen = _session.manager.gameCamera.WorldToScreenPoint(point);
            yield return _session.actions.TouchGesture(screen);
            yield return Wait(0.4f);
            _session.output.Check(
                _session.manager.entityManager.GetEntities(Entity.EntityType.Player).Count > before,
                    "First world touch deploys ally " + index);
            _session.output.Check(_session.interaction.enabled,
                "Legacy input restored after multi-frame world touch " + index);
        }

        public IEnumerator GestureExclusion()
        {
            _session.actions.Submit("party-button");
            yield return Wait(0.2f);
            Button card = _session.actions.Cards("party-list")[0];
            yield return _session.actions.BringIntoView(card);
            Vector2 start = StageInterfaceActions.ScreenPoint(card);
            Vector2 end = _session.manager.gameCamera.WorldToScreenPoint(_session.manager.board.center);
            _session.actions.touch.ProcessTouch(3, TouchPhase.Began, start);
            _session.actions.Submit(card);
            _session.actions.touch.ProcessTouch(3, TouchPhase.Moved, end);
            _session.actions.touch.ProcessTouch(3, TouchPhase.Ended, end);
            _session.output.Check(_session.interaction.GetInteraction() != null,
                "Gesture beginning over UI cannot deploy on release over board");
            yield return Wait(0.2f);
            yield return _session.actions.PointerTap("cancel-button");
            yield return Wait(0.2f);
        }
    }
}
