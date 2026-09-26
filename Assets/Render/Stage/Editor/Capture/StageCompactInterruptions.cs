using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using static HealerLike.Render.Stage.AStageRun;

namespace HealerLike.Render.Stage
{
    public sealed class StageCompactInterruptions
    {
        readonly StageCaptureSession _s;
        public StageCompactInterruptions(StageCaptureSession session) { _s = session; }
        public IEnumerator Run(Button card, Vector2 destination)
        {
            foreach (string interruption in new[] { "pointer-loss", "pause", "ui-teardown" })
            {
                card = _s.actions.Cards("party-list")[0];
                yield return _s.actions.BringIntoView(card);
                int before = _s.manager.entityManager.GetEntities(Entity.EntityType.Player).Count;
                float mana = _s.manager.player.character.mana.Value;
                int gold = _s.manager.player.gold;
                Vector2 point = StageInterfaceActions.ScreenPoint(card);
                using (var touch = new StagePresentationTouch(_s.actions))
                {
                    yield return touch.Frame(TouchPhase.Began, point);
                    yield return touch.Frame(TouchPhase.Moved, point + Vector2.up * 48);
                    yield return touch.Frame(TouchPhase.Moved, destination);
                    yield return StageCompactGestures.Still(touch, destination, .15f);
                    _s.output.Check(_s.actions.touch.roster.active, "Actual placement owns press before " + interruption);
                    if (interruption == "pointer-loss")
                    {
                        _s.actions.captureInput.samples = Array.Empty<Touch>();
                        _s.actions.touch.captureTouches = Array.Empty<Touch>();
                    }
                    else if (interruption == "pause") _s.actions.Submit("pause-button");
                    else _s.actions.ui.enabled = false;
                    yield return Wait(.25f);
                    _s.output.ObserveGameplay(interruption, _s.manager, _s.actions.touch);
                    _s.output.Check(_s.manager.entityManager.GetEntities(Entity.EntityType.Player).Count == before
                        && _s.manager.player.gold == gold && _s.manager.player.character.mana.Value == mana
                        && _s.interaction.GetInteraction() == null && _s.manager.placement.preview == null,
                        "Interruption clears actual preview, creates no creature and spends no resources: " + interruption);
                    if (interruption == "pause") _s.actions.Submit("resume-button");
                    if (interruption == "ui-teardown") _s.actions.ui.enabled = true;
                    yield return Wait(.25f);
                    yield return touch.Frame(TouchPhase.Ended, destination);
                }
                yield return Wait(.15f);
                _s.output.Check(_s.manager.entityManager.GetEntities(Entity.EntityType.Player).Count == before,
                    "Late release after interruption still creates no creature: " + interruption);
            }
            yield return _s.Capture("04c-interruptions-cleared");
        }
    }
}
