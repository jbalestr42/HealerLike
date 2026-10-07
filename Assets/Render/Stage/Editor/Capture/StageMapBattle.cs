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
            StageCompactGestures gestures = new StageCompactGestures(_session);
            for (int i = 0; i < offsets.Length
                && _session.manager.entityManager.GetEntities(Entity.EntityType.Player).Count < desiredAllies; i++)
            {
                // The roster row is always visible. A card deploys only through a held drag onto the board.
                List<Button> cards = StageRosterCards.Deployable(_session.actions);
                if (cards.Count == 0)
                {
                    break;
                }

                int before = _session.manager.entityManager.GetEntities(Entity.EntityType.Player).Count;
                Vector3 point = _session.manager.player.grid.GetNearestWalkablePosition(offsets[i]);
                Vector3 screen = _session.manager.gameCamera.WorldToScreenPoint(point);
                Vector2 drop = gestures.DropPoint(point);
                _session.output.Check(screen.z > 0f
                    && _session.actions.ui.normalizedWorldViewport.Contains(new Vector2(screen.x / Screen.width,
                    screen.y / Screen.height)) && !_session.actions.touch.IsOverInterface(drop),
                    "Deployment drops inside the visible battlefield clear of UI: cell " + screen + ", finger " + drop);
                yield return gestures.Drag(cards[Mathf.Min(i, cards.Count - 1)], drop, null);
                yield return Wait(0.4f);
                _session.output.Check(
                    _session.manager.entityManager.GetEntities(Entity.EntityType.Player).Count == before + 1,
                        "Held roster drag deploys fixture ally " + i);
            }

            _session.output.Check(
                    _session.manager.entityManager.GetEntities(Entity.EntityType.Player).Count > 0,
                "The ordinary party has living allies before battle");
        }

        public IEnumerator Battle(string prefix, int rewards)
        {
            yield return Fight(prefix, false);
            yield return Reward(prefix + "-reward", rewards);
        }

        // The boss room is a fight since 2026-09-30: beating it wins the run through his game-over view, no reward
        public IEnumerator Boss(string prefix)
        {
            yield return Fight(prefix, true);
        }

        IEnumerator Fight(string prefix, bool boss)
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
            float seconds = boss ? 180f : 120f;
            float deadline = Time.realtimeSinceStartup + seconds;
            float nextHeal = 0f;
            float nextDamage = battleStarted + 6f;
            while (boss ? !_session.ascension.IsOver()
                : !StageInterfaceOutput.IsVisible(_session.actions.root.Q("upgrade-panel")))
            {
                // A won boss fight shows the game-over panel too, so the boss survives on the run state instead
                _session.output.Check(boss ? LegacyUiReader.AscensionState(_session.ascension)
                    != AscensionGameType.State.GameOver
                    : !StageInterfaceOutput.IsVisible(_session.actions.root.Q("gameover-panel")),
                    "Party survives " + prefix);
                _session.output.Check(Time.realtimeSinceStartup < deadline,
                    "Natural battle " + (boss ? "ends the run" : "reaches reward") + " within " + seconds
                    + " seconds: " + prefix);
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
