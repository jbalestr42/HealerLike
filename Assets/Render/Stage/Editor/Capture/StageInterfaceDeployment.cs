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
        readonly StageCompactGestures _gestures;
        public StageInterfaceDeployment(StageCaptureSession session)
        {
            _session = session;
            _gestures = new StageCompactGestures(session);
        }

        // A tap on a roster card opens its details. Placement starts only on the drag owner, so a deployment is a
        // held drag from the card to the cell, released over the board.
        public IEnumerator Deploy(int index, Vector3 offset)
        {
            List<Button> cards = StageRosterCards.Deployable(_session.actions);
            _session.output.Check(cards.Count > 0, "Deployable party choice remains");
            Button card = cards[Mathf.Min(index, cards.Count - 1)];
            int before = _session.manager.entityManager.GetEntities(Entity.EntityType.Player).Count;
            Vector3 point = _session.manager.player.grid.GetNearestWalkablePosition(offset);
            Vector2 drop = _gestures.DropPoint(point);
            yield return _gestures.Drag(card, drop, null, TouchPhase.Ended, () => _session.output.Check(
                _session.interaction.GetInteraction() is EntityGridInteraction,
                "Held roster drag begins deployment of card " + index));
            yield return Wait(0.4f);
            _session.output.Check(
                _session.manager.entityManager.GetEntities(Entity.EntityType.Player).Count > before,
                    "Released roster drag deploys ally " + index);
            _session.output.Check(_session.interaction.enabled,
                "Legacy input restored after multi-frame roster drag " + index);
        }

        // The drag is held over the board and screenshotted, then cancelled by the input system
        public IEnumerator Targeting(Vector3 offset)
        {
            List<Button> cards = StageRosterCards.Deployable(_session.actions);
            _session.output.Check(cards.Count > 0, "Deployable party choice remains for the targeting capture");
            int before = _session.manager.entityManager.GetEntities(Entity.EntityType.Player).Count;
            Vector2 drop = _gestures.DropPoint(_session.manager.player.grid.GetNearestWalkablePosition(offset));
            yield return _gestures.Drag(cards[0], drop, "04-targeting", TouchPhase.Canceled, () =>
                _session.output.Check(_session.interaction.GetInteraction() != null,
                    "Held roster drag begins deployment"));
            yield return Wait(0.2f);
            _session.output.Check(_session.interaction.GetInteraction() == null
                && _session.manager.entityManager.GetEntities(Entity.EntityType.Player).Count == before,
                "Cancelling the held drag ends deployment and places nobody");
        }

        // A press that begins over a roster card which cannot drag (a unit already on the board) owns no placement,
        // so releasing it over the board deploys nothing.
        public IEnumerator GestureExclusion()
        {
            Button card = _session.actions.Cards("party-list").Find(button => button.userData is ToolkitCardModel model
                && !model.canDrag && model.source is Entity);
            _session.output.Check(card != null, "A deployed roster card exists to press from");
            yield return _session.actions.BringIntoView(card);
            Vector2 start = StageInterfaceActions.ScreenPoint(card);
            Vector2 end = _session.manager.gameCamera.WorldToScreenPoint(_session.manager.board.center);
            int before = _session.manager.entityManager.GetEntities(Entity.EntityType.Player).Count;
            using (StagePresentationTouch touch = new StagePresentationTouch(_session.actions))
            {
                yield return touch.Frame(TouchPhase.Began, start);
                yield return touch.Frame(TouchPhase.Moved, start + Vector2.up * 48);
                yield return touch.Frame(TouchPhase.Moved, end);
                yield return StageCompactGestures.Still(touch, end, 0.15f);
                _session.output.Check(_session.interaction.GetInteraction() == null,
                    "A press on a card that cannot drag starts no placement while held over the board");
                yield return touch.Frame(TouchPhase.Ended, end);
            }

            yield return Wait(0.3f);
            _session.output.Check(_session.interaction.GetInteraction() == null
                && _session.manager.entityManager.GetEntities(Entity.EntityType.Player).Count == before,
                "Gesture beginning over UI cannot deploy on release over board");
        }
    }
}
