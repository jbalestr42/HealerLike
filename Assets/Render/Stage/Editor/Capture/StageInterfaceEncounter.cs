using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using static HealerLike.Render.Stage.AStageRun;

namespace HealerLike.Render.Stage
{
    public class StageInterfaceEncounter
    {
        readonly StageCaptureSession _session;
        public StageInterfaceEncounter(StageCaptureSession session)
        {
            _session = session;
        }

        public IEnumerator SpellTap()
        {
            foreach (Button card in _session.actions.Cards("spell-list"))
            {
                if (!card.enabledInHierarchy)
                {
                    continue;
                }

                yield return _session.actions.SelectCard(card);
                yield return Wait(0.2f);
                if (_session.interaction.GetInteraction() != null)
                {
                    _session.output.Check(StageInterfaceOutput.IsVisible(_session.actions.root.Q("cancel-button")),
                        "Spell targeting has touch cancel");
                    yield return _session.actions.PointerTap("cancel-button");
                    yield return Wait(0.2f);
                    _session.output.Check(_session.interaction.GetInteraction() == null,
                        "Touch cancel ends spell targeting");
                    yield return _session.actions.SelectCard(card);
                    yield return Wait(0.2f);
                    AInteraction spell = _session.interaction.GetInteraction();
                    foreach (Entity entity in _session.manager.entityManager.GetComponentsInChildren<Entity>())
                    {
                        if (spell == null || !spell.IsValidTarget(entity.gameObject))
                        {
                            continue;
                        }

                        Vector2 point
                            = _session.manager.gameCamera.WorldToScreenPoint(RenderTargets.Point(entity.gameObject));
                        _session.actions.WorldTap(point);
                        if (_session.interaction.GetInteraction() == null)
                        {
                            _session.output.Check(true, "Toolkit spell casts on first valid world tap");
                            yield break;
                        }
                    }

                    _session.output.Check(false, "Toolkit spell reaches a valid target");
                }
            }

            _session.output.Check(false, "A usable targeting spell is available");
        }

        // Casts every usable spell card once: an instant spell casts on its press, a targeted one on the most
        // injured ally it accepts, or else the first creature it accepts; one with no target is cancelled
        IEnumerator Assist(System.Action<int> cast)
        {
            AscensionGameType ascension = Object.FindAnyObjectByType<AscensionGameType>();
            foreach (Button card in _session.actions.Cards("spell-list"))
            {
                if (ascension == null || LegacyUiReader.AscensionState(ascension) != AscensionGameType.State.OnGoingBattle)
                {
                    yield break;
                }

                if (!card.enabledInHierarchy || !StageInterfaceOutput.IsVisible(card))
                {
                    continue;
                }

                yield return _session.actions.SelectCard(card);
                yield return Wait(0.2f);
                AInteraction spell = _session.interaction.GetInteraction();
                if (spell == null)
                {
                    cast(1);
                    continue;
                }

                Entity target = Target(spell);
                if (target != null)
                {
                    _session.actions.WorldTap(
                        _session.manager.gameCamera.WorldToScreenPoint(RenderTargets.Point(target.gameObject)));
                    yield return null;
                }

                if (_session.interaction.GetInteraction() == null)
                {
                    cast(1);
                }
                else if (StageInterfaceOutput.IsVisible(_session.actions.root.Q("cancel-button")))
                {
                    _session.actions.Submit("cancel-button");
                    yield return null;
                }
            }
        }

        Entity Target(AInteraction spell)
        {
            Entity best = null;
            foreach (GameObject ally in _session.manager.entityManager.GetEntities(Entity.EntityType.Player))
            {
                Entity entity = ally.GetComponent<Entity>();
                if (entity != null && spell.IsValidTarget(ally) && (best == null
                    || entity.health.percent < best.health.percent))
                {
                    best = entity;
                }
            }

            if (best != null)
            {
                return best;
            }

            foreach (GameObject enemy in _session.manager.entityManager.GetEntities(Entity.EntityType.Computer))
            {
                if (spell.IsValidTarget(enemy))
                {
                    return enemy.GetComponent<Entity>();
                }
            }

            return null;
        }

        public IEnumerator Reward()
        {
            // The first wave runs normally. No direct reward or game-over event is injected.
            // Since Julien's 54c28bee the Cleric fields four units with weaker heals, and they lose this wave
            // unassisted: the healer heals, through the same Toolkit spell cards and world taps as a player
            float deadline = Time.realtimeSinceStartup + 100f;
            int casts = 0;
            while (!StageInterfaceOutput.IsVisible(_session.actions.root.Q("upgrade-panel")))
            {
                if (StageInterfaceOutput.IsVisible(_session.actions.root.Q("gameover-panel")))
                {
                    _session.output.Check(false, "Party survives first encounter, " + casts + " assisted casts");
                }

                if (Time.realtimeSinceStartup > deadline)
                {
                    _session.output.Check(false, "First encounter reaches reward in 100 seconds");
                }

                yield return Assist(count => casts += count);
                yield return Wait(0.3f);
            }

            _session.output.manifest.checks.Add("First encounter won with " + casts
                + " spells cast through Toolkit cards");

            yield return _session.Capture("10-reward");
            List<Button> cards = _session.actions.Cards("upgrade-list");
            _session.output.Check(cards.Count > 0, "Real wave reward offers choices");
            Button equipment = cards.Find(button => button.Q<Label>("card-status").text.StartsWith("Party equipment"));
            yield return _session.actions.SelectCard(equipment != null ? equipment : cards[0]);
            yield return Wait(1f);
            _session.output.Check(!StageInterfaceOutput.IsVisible(_session.actions.root.Q("upgrade-panel"))
                && StageInterfaceOutput.IsVisible(_session.actions.root.Q("map-panel")),
                "Toolkit reward returns to expedition map");
            yield return StageMapActions.EnterCombat(_session.actions);
            if (equipment != null)
            {
                _session.actions.Submit("inventory-button");
                yield return Wait(0.3f);
                yield return _session.actions.SelectCard(_session.actions.Cards("inventory-list")[0]);
                DropdownField target = _session.actions.root.Q<DropdownField>("inventory-target");
                target.value = target.choices[0];
                yield return Wait(0.3f);
                Entity ally
                    = _session.manager.entityManager.GetEntities(Entity.EntityType.Player)[0].GetComponent<Entity>();
                int previous = ally.inventoryHandler.items.Count;
                _session.actions.Submit("inventory-equip-button");
                yield return Wait(0.3f);
                _session.output.Check(ally.inventoryHandler.items.Count > previous,
                    "Toolkit equipment action transfers earned reward to ally");
                yield return _session.Capture("10b-equipped-reward");
                _session.actions.Submit("inventory-close-button");
                yield return Wait(0.2f);
            }
            else
            {
                _session.output.manifest.checks.Add("Equipment transfer not exercised: reward offered healer "
                    + "upgrades only");
            }
        }
    }
}
