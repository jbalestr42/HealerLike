using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static HealerLike.Render.Stage.AStageRun;

namespace HealerLike.Render.Stage
{
    public sealed class StageCompactSpells
    {
        readonly StageCaptureSession _s;
        readonly StageCompactGestures _gestures;
        public StageCompactSpells(StageCaptureSession session, StageCompactGestures gestures)
        { _s = session; _gestures = gestures; }
        void Observe(string name) { _s.output.ObserveGameplay(name, _s.manager, _s.actions.touch); }
        public IEnumerator Run()
        {
            Character character = _s.manager.player.character;
            Button spell = _s.actions.Cards("spell-list").First(b =>
                (b.userData as ToolkitCardModel)?.source is CharacterSkillSlot slot && slot.data.name == "Heal");
            CharacterSkillSlot slot = (CharacterSkillSlot)((ToolkitCardModel)spell.userData).source;
            float before = character.mana.Value;
            yield return new StageCompactReview(_s).KeyboardEscapeOwnership(spell);
            yield return _gestures.Hold(spell, "10-spell-hold", false);
            Observe("spell-hold");
            _s.output.Check(character.mana.Value == before && _s.interaction.GetInteraction() == null,
                "Held usable spell and duplicate release never cast or spend mana");
            yield return _s.actions.PointerTap(spell);
            yield return Wait(0.2f);
            Observe("short-spell-activation");
            AInteraction targeting = _s.interaction.GetInteraction();
            _s.output.Check(targeting != null, "Short spell touch activates existing targeting on release");
            Entity entity = _s.manager.entityManager.GetEntities(Entity.EntityType.Player)[0].GetComponent<Entity>();
            Vector2 board = _s.manager.gameCamera.WorldToScreenPoint(entity.GetComponent<Collider>().bounds.center);
            _s.output.Check(Physics.Raycast(_s.manager.gameCamera.ScreenPointToRay(board), out RaycastHit holdHit,
                Mathf.Infinity, targeting.GetLayerMask()) && holdHit.collider.gameObject == entity.gameObject,
                "World hold aims at the actual gameplay collider of the inspected creature");
            using (var touch = new StagePresentationTouch(_s.actions))
            {
                yield return touch.Frame(TouchPhase.Began, board);
                yield return StageCompactGestures.Still(touch, board, 0.55f);
                _s.output.Check(StageInterfaceOutput.IsVisible(_s.actions.root.Q("detail-panel"))
                    && _s.actions.root.Q<Label>("detail-title").text == entity.data.title,
                    "Holding actual board creature selects its details while spell targeting is active");
                _s.output.Check(entity.GetComponent<SelectableEntity>().isHighlighted,
                    "World hold selects the actual creature through existing selection controls");
                yield return _s.Capture("11-world-creature-hold");
                yield return touch.Frame(TouchPhase.Ended, board);
            }
            yield return Wait(0.2f);
            Observe("world-or-ui-release");
            _s.output.Check(ReferenceEquals(targeting, _s.interaction.GetInteraction()) && character.mana.Value == before,
                "Held world-creature release consumes press without casting targeted spell");
            yield return new StageCompactReview(_s).OutsideDismiss("11b-world-outside-dismissal");
            Vector2 unrelated = StageInterfaceActions.ScreenPoint(_s.actions.root.Q("wave-label"));
            using (var touch = new StagePresentationTouch(_s.actions))
            {
                yield return touch.Frame(TouchPhase.Began, unrelated);
                yield return touch.Frame(TouchPhase.Moved, board);
                yield return touch.Frame(TouchPhase.Ended, board);
            }
            yield return Wait(0.1f);
            Observe("world-or-ui-release");
            _s.output.Check(ReferenceEquals(targeting, _s.interaction.GetInteraction()) && character.mana.Value == before,
                "Unrelated UI-origin release over a valid target cannot activate the board");
            board = _s.manager.gameCamera.WorldToScreenPoint(entity.GetComponent<Collider>().bounds.center);
            _s.output.Check(!_s.actions.touch.IsOverInterface(board), "Reprojected spell target is outside UI");
            _s.output.Check(Physics.Raycast(_s.manager.gameCamera.ScreenPointToRay(board), out RaycastHit castHit,
                Mathf.Infinity, targeting.GetLayerMask()) && targeting.IsValidTarget(castHit.collider.gameObject),
                "Reprojected spell target hits an actual legal entity collider");
            yield return _s.actions.TouchGesture(board);
            yield return Wait(0.15f);
            Observe("actual-spell-cast");
            _s.output.Check(_s.interaction.GetInteraction() == null && character.mana.Value < before,
                "Short world release completes real spell action and spends actual mana");
            _s.output.Check(ToolkitSpellState.Read(slot, character).remaining > 0,
                "Cooldown overlay reads positive remaining fraction from existing fill");
            yield return _s.Capture("12-cooldown");
            yield return _gestures.Hold(spell, "13-disabled-spell-hold", false);
            // Explicit capture fixture: drain through a public resource consumer, then restore using Refill.
            var consumer = ScriptableObject.CreateInstance<ConsumerFactory>();
            consumer.data = new ConsumerData { value = new FlatValue { data = new FlatValueData { value = 10000 } } };
            character.mana.AddResourceModifier(ResourceModifier.Create(consumer, character.gameObject, character.gameObject));
            yield return Wait(0.2f);
            Object.Destroy(consumer);
            _s.output.Check(character.mana.Value < ToolkitSpellState.Read(slot, character).cost,
                "Fixture public mana consumer creates actual insufficient-mana state");
            Observe("mana-shortage-fixture");
            yield return _s.Capture("14-mana-shortage");
            float shortage = character.mana.Value;
            yield return _gestures.Hold(spell, "14b-shortage-inspection", false);
            _s.output.Check(character.mana.Value == shortage && _s.interaction.GetInteraction() == null,
                "Unavailable spell stays inspectable without activation");
            yield return new StageCompactReview(_s).ControllerInspect(spell);
            yield return Wait(ToolkitSpellState.Read(slot, character).duration + 0.1f);
            _s.output.Check(ToolkitSpellState.Read(slot, character).remaining == 0,
                "Existing cooldown recovers and clears the dark remaining sector");
            yield return _s.Capture("14c-cooldown-recovered-shortage");
            character.mana.Refill();
            yield return _s.actions.PointerTap("wave-button");
            yield return Wait(0.6f);
            _s.output.Check(!StageInterfaceOutput.IsVisible(_s.actions.root.Q("wave-button")),
                "Start battle disappears during real combat");
            yield return _s.Capture("15-combat");
            Rect beforeTargeting = _s.actions.ui.normalizedWorldViewport;
            yield return _s.actions.PointerTap(spell);
            yield return Wait(0.2f);
            _s.output.Check(beforeTargeting != _s.actions.ui.normalizedWorldViewport,
                "Combat targeting contextual control changes the reserved viewport");
            yield return _s.Capture("15b-combat-targeting");
            targeting = _s.interaction.GetInteraction();
            entity = _s.manager.entityManager.GetEntities(Entity.EntityType.Player)[0].GetComponent<Entity>();
            board = _s.manager.gameCamera.WorldToScreenPoint(entity.GetComponent<Collider>().bounds.center);
            _s.output.Check(targeting != null && !_s.actions.touch.IsOverInterface(board)
                && Physics.Raycast(_s.manager.gameCamera.ScreenPointToRay(board), out RaycastHit reprojected,
                    Mathf.Infinity, targeting.GetLayerMask()) && targeting.IsValidTarget(reprojected.collider.gameObject),
                "Combat target is reprojected against the current camera after contextual viewport change");
            before = character.mana.Value;
            using (var touch = new StagePresentationTouch(_s.actions))
            {
                yield return touch.Frame(TouchPhase.Began, board);
                Vector2 release = _s.manager.gameCamera.WorldToScreenPoint(entity.GetComponent<Collider>().bounds.center);
                _s.output.Check(Vector2.Distance(board, release) <= 18 * ToolkitScreenLayout.GetScale(Screen.width, Screen.height, false)
                    && Physics.Raycast(_s.manager.gameCamera.ScreenPointToRay(release), out RaycastHit released,
                        Mathf.Infinity, targeting.GetLayerMask()) && targeting.IsValidTarget(released.collider.gameObject),
                    "Moving combat creature remains a legal target at the tracked short release");
                yield return touch.Frame(TouchPhase.Ended, release);
            }
            yield return Wait(0.1f);
            Observe("combat-reprojected-cast");
            _s.output.Check(_s.interaction.GetInteraction() == null && character.mana.Value < before,
                "Reprojected combat target casts successfully after camera refit");
            yield return _s.Capture("15c-combat-cast");
        }
    }
}
