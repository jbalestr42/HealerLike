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
        public IEnumerator Run()
        {
            Character character = _s.manager.player.character;
            Button spell = _s.actions.Cards("spell-list").First(b =>
                (b.userData as ToolkitCardModel)?.source is CharacterSkillSlot slot && slot.data.name == "Heal");
            CharacterSkillSlot slot = (CharacterSkillSlot)((ToolkitCardModel)spell.userData).source;
            float before = character.mana.Value;
            yield return _gestures.Hold(spell, "10-spell-hold", false);
            _s.output.Check(character.mana.Value == before && _s.interaction.GetInteraction() == null,
                "Held usable spell and duplicate release never cast or spend mana");
            yield return _s.actions.PointerTap(spell);
            yield return Wait(.2f);
            AInteraction targeting = _s.interaction.GetInteraction();
            _s.output.Check(targeting != null, "Short spell touch activates existing targeting on release");
            Entity entity = _s.manager.entityManager.GetEntities(Entity.EntityType.Player)[0].GetComponent<Entity>();
            Vector2 board = _s.manager.gameCamera.WorldToScreenPoint(RenderTargets.Point(entity.gameObject));
            using (var touch = new StagePresentationTouch(_s.actions))
            {
                yield return touch.Frame(TouchPhase.Began, board);
                yield return StageCompactGestures.Still(touch, board, .55f);
                _s.output.Check(StageInterfaceOutput.IsVisible(_s.actions.root.Q("detail-panel"))
                    && _s.actions.root.Q<Label>("detail-title").text == entity.data.title,
                    "Holding actual board creature selects its details while spell targeting is active");
                yield return _s.Capture("11-world-creature-hold");
                yield return touch.Frame(TouchPhase.Ended, board);
            }
            yield return Wait(.2f);
            _s.output.Check(ReferenceEquals(targeting, _s.interaction.GetInteraction()) && character.mana.Value == before,
                "Held world-creature release consumes press without casting targeted spell");
            yield return _s.actions.PointerTap("detail-close-button");
            Vector2 unrelated = StageInterfaceActions.ScreenPoint(_s.actions.root.Q("currency-label"));
            using (var touch = new StagePresentationTouch(_s.actions))
            {
                yield return touch.Frame(TouchPhase.Began, unrelated);
                yield return touch.Frame(TouchPhase.Moved, board);
                yield return touch.Frame(TouchPhase.Ended, board);
            }
            yield return Wait(.1f);
            _s.output.Check(ReferenceEquals(targeting, _s.interaction.GetInteraction()) && character.mana.Value == before,
                "Unrelated UI-origin release over a valid target cannot activate the board");
            yield return _s.actions.TouchGesture(board);
            yield return Wait(.15f);
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
            yield return Wait(.2f);
            Object.Destroy(consumer);
            _s.output.Check(character.mana.Value < ToolkitSpellState.Read(slot, character).cost,
                "Fixture public mana consumer creates actual insufficient-mana state");
            yield return _s.Capture("14-mana-shortage");
            float shortage = character.mana.Value;
            yield return _gestures.Hold(spell, "14b-shortage-inspection", false);
            _s.output.Check(character.mana.Value == shortage && _s.interaction.GetInteraction() == null,
                "Unavailable spell stays inspectable without activation");
            character.mana.Refill();
            yield return _s.actions.PointerTap("wave-button");
            yield return Wait(.6f);
            _s.output.Check(!StageInterfaceOutput.IsVisible(_s.actions.root.Q("wave-button")),
                "Start battle disappears during real combat");
            yield return _s.Capture("15-combat");
        }
    }
}
