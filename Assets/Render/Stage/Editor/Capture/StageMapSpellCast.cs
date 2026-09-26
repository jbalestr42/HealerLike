using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using static HealerLike.Render.Stage.AStageRun;
using BattleEvidence = HealerLike.Render.Stage.StageMapRun.BattleEvidence;
using SpellEvidence = HealerLike.Render.Stage.StageMapRun.SpellEvidence;
using MapFrame = HealerLike.Render.Stage.StageMapRun.MapFrame;
using Room = HealerLike.Render.Stage.StageMapRun.Room;

namespace HealerLike.Render.Stage
{
    public class StageMapSpellCast
    {
        readonly StageMapSession _session;
        public StageMapSpellCast(StageMapSession session)
        {
            _session = session;
        }

        public IEnumerator HealThroughInterface()
        {
            if (LegacyUiReader.AscensionState(_session.ascension) != AscensionGameType.State.OnGoingBattle)
            {
                yield break;
            }

            Entity target = null;
            int injured = 0;
            foreach (GameObject ally in _session.manager.entityManager.GetEntities(Entity.EntityType.Player))
            {
                Entity entity = ally.GetComponent<Entity>();
                if (entity.health.percent < 0.9f)
                {
                    injured++;
                    if (target == null || entity.health.percent < target.health.percent)
                    {
                        target = entity;
                    }
                }
            }

            if (target == null)
            {
                yield break;
            }

            Button group = SpellButton("Heal group");
            if (injured >= 2 && group != null)
            {
                yield return CastThroughInterface(group, null, true);
                yield break;
            }

            Button single = SpellButton("Heal");
            if (single != null && TargetPoint(target, out _))
            {
                yield return CastThroughInterface(single, target, true);
            }
            else if (group != null)
            {
                yield return CastThroughInterface(group, null, true);
            }
        }

        public Button SpellButton(string title)
        {
            return _session.actions.Cards("spell-list").Find(card => card.enabledInHierarchy
                && card.Q<Label>("card-title") != null && card.Q<Label>("card-title").text == title);
        }

        public bool TargetPoint(Entity target, out Vector2 point)
        {
            point = default;
            if (target == null)
            {
                return false;
            }

            Collider collider = target.GetComponent<Collider>();
            if (collider == null)
            {
                return false;
            }

            Vector3 screen = _session.manager.gameCamera.WorldToScreenPoint(collider.bounds.center);
            point = screen;
            if (screen.z <= 0f
                || !_session.actions.ui.normalizedWorldViewport.Contains(new Vector2(screen.x / Screen.width,
                screen.y / Screen.height)) || _session.actions.touch.IsOverInterface(point))
            {
                return false;
            }

            return Physics.Raycast(_session.manager.gameCamera.ScreenPointToRay(point), out RaycastHit hit,
                Mathf.Infinity, 1 << Layers.Entity) && hit.collider.gameObject == target.gameObject;
        }

        public IEnumerator CastThroughInterface(Button button, Entity target, bool healing)
        {
            yield return _session.actions.BringIntoView(button);
            if (LegacyUiReader.AscensionState(_session.ascension) != AscensionGameType.State.OnGoingBattle
                || !button.enabledInHierarchy || !StageInterfaceOutput.IsVisible(button))
            {
                yield break;
            }

            Character character = _session.manager.player.character;
            string title = button.Q<Label>("card-title").text;
            CharacterSkillSlot slot = character.skillSlots.Find(value => value.data != null
                && value.data.name == title);
            ApplyConsumerCharacterSkillData data = slot != null ? slot.data as ApplyConsumerCharacterSkillData : null;
            _session.output.Check(data != null, "Assisted spell uses the authored resource-consumer skill: " + title);
            SpellEvidence evidence = new SpellEvidence
            {
                floor = _session.ascension.run.currentFloor,
                spell = title,
                target = target != null ? target.name + " " + target.GetEntityId() : "all " + data.entityType,
                isHealing = healing,
                cameraBeforeTargeting = _session.manager.gameCamera.transform.position,
                viewportBeforeTargeting = _session.actions.ui.normalizedWorldViewport
            };
            _session.manifest.spells.Add(evidence);
            using (new StageMapSpellObservation(_session, character, target, data, evidence))
            {
                yield return _session.actions.PointerTap(button);
                // The next HUD refresh reveals Cancel and changes the free-world viewport. StageInterface
                // reframes the camera for that layout; aim only after it has settled, like MobileInterface.
                yield return Wait(0.2f);
                InteractionManager interaction = Object.FindAnyObjectByType<InteractionManager>();
                if (data.isSingle && target != null
                    && LegacyUiReader.AscensionState(_session.ascension) == AscensionGameType.State.OnGoingBattle
                    && LegacyUiReader.CurrentView(Object.FindAnyObjectByType<UIManager>()) == ViewType.Game
                    && interaction.GetInteraction()is SingleTargetInteraction single
                    && single.IsValidTarget(target.gameObject) && TargetPoint(target, out Vector2 point))
                {
                    _session.output.Check(Camera.main == _session.manager.gameCamera,
                        "Targeted touch and render observation use the same main camera");
                    evidence.targetVerifiedByRaycast = true;
                    evidence.targetScreenPoint = point;
                    evidence.cameraAtPress = _session.manager.gameCamera.transform.position;
                    evidence.viewportAtPress = _session.actions.ui.normalizedWorldViewport;
                    using (StagePresentationTouch finger = new StagePresentationTouch(_session.actions))
                    {
                        yield return finger.Frame(TouchPhase.Began, point);
                        bool stillInBattle
                            = LegacyUiReader.AscensionState(_session.ascension) == AscensionGameType.State.OnGoingBattle
                            && LegacyUiReader.CurrentView(Object.FindAnyObjectByType<UIManager>()) == ViewType.Game;
                        Vector2 release = point;
                        bool hit = stillInBattle && TargetPoint(target, out release);
                        evidence.releaseTargetVerifiedByRaycast = hit;
                        evidence.releaseScreenPoint = release;
                        evidence.cameraAtRelease = _session.manager.gameCamera.transform.position;
                        evidence.viewportAtRelease = _session.actions.ui.normalizedWorldViewport;
                        float threshold = 18f * Mathf.Min(Screen.width, Screen.height) / 390f;
                        if (hit && Vector2.Distance(point, release) <= threshold)
                        {
                            yield return finger.Frame(TouchPhase.Ended, release);
                            evidence.worldTapEndedInteraction = interaction.GetInteraction() == null;
                        }
                        else
                        {
                            evidence.worldTapCanceled = true;
                            yield return finger.Frame(TouchPhase.Canceled, point);
                        }
                    }
                }

                yield return Wait(0.25f);
                // A reward overlay can open while the held gesture is being consumed.
                if (interaction.GetInteraction() != null)
                {
                    if (StageInterfaceOutput.IsVisible(_session.actions.root.Q("cancel-button"))
                        && LegacyUiReader.CurrentView(Object.FindAnyObjectByType<UIManager>()) == ViewType.Game)
                    {
                        yield return _session.actions.PointerTap("cancel-button");
                    }
                    else
                    {
                        interaction.CancelInteraction();
                        _session.manifest.interventions.Add("Capture cleanup cancelled a pending targeting spell "
                            + "through the original CancelInteraction command after the reward overlay hid Cancel; "
                            + "no target was activated");
                    }
                }
            }
        }
    }
}
