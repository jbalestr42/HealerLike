using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static HealerLike.Render.Stage.AStageRun;
using BattleEvidence = HealerLike.Render.Stage.StageMapRun.BattleEvidence;
using SpellEvidence = HealerLike.Render.Stage.StageMapRun.SpellEvidence;
using MapFrame = HealerLike.Render.Stage.StageMapRun.MapFrame;
using Room = HealerLike.Render.Stage.StageMapRun.Room;

namespace HealerLike.Render.Stage
{
    public class StageMapBattle
    {
        readonly StageMapSession _session;
        readonly StageMapSpellCast _spells;
        public StageMapBattle(StageMapSession session)
        {
            _session = session;
            _spells = new StageMapSpellCast(session);
        }

        public IEnumerator DeployParty(int desiredAllies)
        {
            Vector3[] offsets =
            {
                Vector3.left * 2f,
                Vector3.left * 3f + Vector3.back,
                Vector3.left * 2f + Vector3.forward * 2f,
                Vector3.left * 4f + Vector3.forward,
                Vector3.left * 4f + Vector3.back * 2f,
                Vector3.left * 3f + Vector3.forward * 3f,
                Vector3.left * 2f + Vector3.back * 3f,
                Vector3.left * 4f + Vector3.forward * 3f,
                Vector3.left * 4f + Vector3.back * 3f,
                Vector3.left * 3f + Vector3.forward * 4f
            };
            for (int i = 0; i < offsets.Length
                && _session.manager.entityManager.GetEntities(Entity.EntityType.Player).Count < desiredAllies; i++)
            {
                yield return _session.actions.PointerTap("party-button");
                yield return Wait(0.2f);
                List<Button> cards
                    = _session.actions.Cards("party-list").FindAll(button => button.enabledInHierarchy
                    && button.Q<Label>("card-status").text == "Deploy");
                if (cards.Count == 0)
                {
                    yield return _session.actions.PointerTap("party-close-button");
                    break;
                }

                yield return _session.actions.SelectCardByTouch(cards[Mathf.Min(i, cards.Count - 1)]);
                yield return Wait(0.15f);
                int before = _session.manager.entityManager.GetEntities(Entity.EntityType.Player).Count;
                Vector3 point = _session.manager.player.grid.GetNearestWalkablePosition(offsets[i]);
                Vector3 screen = _session.manager.gameCamera.WorldToScreenPoint(point);
                _session.output.Check(screen.z > 0f
                    && _session.actions.ui.normalizedWorldViewport.Contains(new Vector2(screen.x / Screen.width,
                    screen.y / Screen.height)) && !_session.actions.touch.IsOverInterface(screen),
                    "Deployment aims inside the visible battlefield clear of UI");
                yield return _session.actions.TouchGesture(screen);
                yield return Wait(0.4f);
                _session.output.Check(
                    _session.manager.entityManager.GetEntities(Entity.EntityType.Player).Count == before + 1,
                        "Ordinary Toolkit party and board touches deploy fixture ally " + i);
            }

            _session.output.Check(
                    _session.manager.entityManager.GetEntities(Entity.EntityType.Player).Count > 0,
                "The ordinary party has living allies before battle");
        }

        public IEnumerator Battle(string prefix, int rewards)
        {
            BattleEvidence evidence = new BattleEvidence
            {
                floor = _session.ascension.run.currentFloor,
                roomType = _session.ascension.run.currentNode.type.ToString(),
                authoredWave = UnityEditor.AssetDatabase.GetAssetPath(StageMapReadout.Wave(_session.ascension)),
                alliesBeforeDeployment = _session.manager.entityManager.GetEntities(Entity.EntityType.Player).Count
            };
            _session.manifest.battles.Add(evidence);
            int desired
                = _session.ascension.run.currentNode.type == MapNodeType.Elite ? 10 : evidence.floor == 0 ? 6 : 8;
            yield return DeployParty(desired);
            evidence.alliesAtBattleStart = _session.manager.entityManager.GetEntities(Entity.EntityType.Player).Count;
            evidence.enemiesAtBattleStart
                = _session.manager.entityManager.GetEntities(Entity.EntityType.Computer).Count;
            int starts = _session.manifest.battlesStarted;
            yield return _session.actions.PointerTap("wave-button");
            float battleStarted = Time.realtimeSinceStartup;
            yield return Wait(0.3f);
            _session.output.Check(_session.manifest.battlesStarted == starts + 1,
                "One battle button touch dispatches one battle start");
            yield return _session.Capture(prefix + "-battle");
            float deadline = Time.realtimeSinceStartup + 120f;
            float nextHeal = 0f;
            float nextDamage = battleStarted + 6f;
            while (!StageInterfaceOutput.IsVisible(_session.actions.root.Q("upgrade-panel")))
            {
                _session.output.Check(!StageInterfaceOutput.IsVisible(_session.actions.root.Q("gameover-panel")),
                    "Party survives " + prefix);
                _session.output.Check(Time.realtimeSinceStartup < deadline,
                    "Natural battle reaches reward within 120 seconds: " + prefix);
                if (Time.realtimeSinceStartup >= nextHeal)
                {
                    nextHeal = Time.realtimeSinceStartup + 0.4f;
                    yield return _spells.HealThroughInterface();
                }

                if (Time.realtimeSinceStartup >= nextDamage
                    && LegacyUiReader.AscensionState(_session.ascension) == AscensionGameType.State.OnGoingBattle)
                {
                    nextDamage = Time.realtimeSinceStartup + 3f;
                    Button damage = _spells.SpellButton("Damage all enemy");
                    if (damage != null)
                    {
                        yield return _spells.CastThroughInterface(damage, null, false);
                    }
                }

                yield return Wait(0.1f);
            }

            yield return Reward(prefix + "-reward", rewards);
        }

        public IEnumerator Reward(string name, int expectedChoices)
        {
            yield return Wait(0.3f);
            List<Button> rewards = _session.actions.Cards("upgrade-list");
            _session.output.Check(StageInterfaceOutput.IsVisible(_session.actions.root.Q("upgrade-panel"))
                && rewards.Count == expectedChoices, "Original room reward offers " + expectedChoices + " choices: "
                + name);
            yield return _session.Capture(name);
            yield return _session.actions.SelectCardByTouch(rewards[0]);
            yield return StageMapActions.WaitForSelection(_session.actions);
            _session.output.Check(!StageInterfaceOutput.IsVisible(_session.actions.root.Q("upgrade-panel")),
                "Reward touch returns to expedition map");
        }
    }
}
