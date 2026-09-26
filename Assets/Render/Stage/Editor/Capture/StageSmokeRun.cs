using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // Plays three rounds of the game loop: allies, Next Wave, first upgrade, again. AscensionGameType keeps its
    // round state private, so it is read from its own "[AscensionGameType] A -> B" log line.
    public class StageSmokeRun : AStageRun
    {
        public static readonly int Rounds = 3;
        // Errors that are not the render layer's: the package search index and its web fetch on the editor side, and
        // the game's own report that BasicHealer has six skills for the five inputs of Main
        static readonly string[] knownErrors =
            { "Insecure connection not allowed", "SearchDatabase", "Not Enough inputs" };

        AscensionGameType.State _state = AscensionGameType.State.None;
        int _roundsDone;
        int _errors;

        protected override IEnumerator Run()
        {
            Application.logMessageReceived += OnLog;
            AscensionGameType.OnRoundEnd.AddListener(OnRoundEnd);
            float previousTimeScale = Time.timeScale;
            try
            {
                Time.timeScale = 3f;
                List<EntityData> allies = StagePlayer.LoadAllies();
                float nextCast = 0f;
                // AStageRun has already selected the first map room through Toolkit navigation.
                _state = LegacyUiReader.AscensionState(UnityEngine.Object.FindAnyObjectByType<AscensionGameType>());
                while (_roundsDone < Rounds && _state != AscensionGameType.State.GameOver)
                {
                    if (_state == AscensionGameType.State.SelectRoom)
                    {
                        StageInterfaceActions actions = new StageInterfaceActions
                            { ui = UnityEngine.Object.FindAnyObjectByType<ToolkitGameUI>() };
                        yield return StageMapActions.SelectFirst(actions, false);
                    }
                    else if (_state == AscensionGameType.State.WaitForRoundToStart)
                    {
                        yield return Wait(1f);
                        _player.PlaceAllies(_manager, allies);
                        _state = AscensionGameType.State.None;
                        _hud.nextWaveButton.onClick.Invoke();
                    }
                    else if (_state == AscensionGameType.State.SelectUpgrade)
                    {
                        yield return Wait(1f);
                        _state = AscensionGameType.State.None;
                        PickUpgrade();
                    }
                    else if (_state == AscensionGameType.State.OnGoingBattle && Time.time >= nextCast)
                    {
                        nextCast = Time.time + 1f;
                        List<GameObject> friends = _manager.entityManager.GetEntities(Entity.EntityType.Player);
                        _player.CastOn(_manager,
                            friends.Count > 0 && friends[0] != null ? friends[0].GetComponent<Entity>() : null);
                    }

                    yield return NextFrame();
                }
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                Application.logMessageReceived -= OnLog;
                AscensionGameType.OnRoundEnd.RemoveListener(OnRoundEnd);
            }
            bool isPassed = _roundsDone >= Rounds && _errors == 0 && _attacks > 0 && _heals > 0 && _maxZones > 0;
            Debug.Log($"[StageSmokeRun] {(isPassed ? "PASS" : "FAIL")} rounds {_roundsDone} attacks {_attacks} "
                      + $"heals {_heals} zones {_maxZones} errors {_errors}");
            StagePlay.Finish(this, isPassed);
        }

        void OnRoundEnd()
        {
            _roundsDone++;
            Debug.Log($"[StageSmokeRun] Round {_roundsDone} ended, attacks {_attacks} heals {_heals} "
                + $"zones {_maxZones}");
        }

        // The upgrade view offers buttons, the first one is picked through its own call
        void PickUpgrade()
        {
            SelectItemUpgradeButton item = UnityEngine.Object.FindAnyObjectByType<SelectItemUpgradeButton>();
            if (item != null)
            {
                item.SelectUpgrade();
                return;
            }

            SelectPlayerItemUpgradeButton playerItem =
                UnityEngine.Object.FindAnyObjectByType<SelectPlayerItemUpgradeButton>();
            if (playerItem != null)
            {
                playerItem.SelectUpgrade();
            }
        }

        static bool IsKnown(string message, string stackTrace)
        {
            foreach (string known in knownErrors)
            {
                if (message.Contains(known) || (stackTrace != null && stackTrace.Contains(known)))
                {
                    return true;
                }
            }
            return false;
        }

        void OnLog(string message, string stackTrace, LogType type)
        {
            if ((type == LogType.Error || type == LogType.Exception) && !IsKnown(message, stackTrace))
            {
                _errors++;
                Debug.Log($"[StageSmokeRun] Counted error: {message}");
            }

            string prefix = "[AscensionGameType] ";
            int arrow = message.IndexOf(" -> ", StringComparison.Ordinal);
            if (message.StartsWith(prefix, StringComparison.Ordinal) && arrow > 0
                && Enum.TryParse(message.Substring(arrow + 4).Trim(), out AscensionGameType.State next))
            {
                _state = next;
            }
        }
    }
}
