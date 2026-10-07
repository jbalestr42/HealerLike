using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using static HealerLike.Render.Stage.AStageRun;
using System.Text.RegularExpressions;

namespace HealerLike.Render.Stage
{
    public class StageMapScenario
    {
        readonly StageMapSession _session;
        public StageMapScenario(StageMapSession session)
        {
            _session = session;
        }

        public IEnumerator Run()
        {
            StageMapGraphProof graph = new StageMapGraphProof(_session);
            StageMapBattle battle = new StageMapBattle(_session);
            _session.output.Check(Regex.IsMatch(_session.manifest.revision, "^[0-9a-fA-F]{40}$"),
                "Exact capture revision recorded");
            _session.Attach();
            _session.mapFixture = new StageMapFixture(_session.ascension, false);
            _session.manifest.interventions.Add("Default authored map settings cloned without layout changes; seed "
                + "fixed to 271828 in this capture only");
            UnityEngine.Random.InitState(StageMapFixture.Seed);
            yield return _session.Resize(1080, 1920);
            yield return _session.actions.PointerTap("start-button");
            yield return StageMapActions.WaitForSelection(_session.actions);
            yield return graph.Map("01-default-portrait", "authored-settings", true);
            _session.actions.ui.safeAreaProvider = () => new Rect(0f, 34f / 844f, 1f, 1f - 78f / 844f);
            yield return Wait(0.3f);
            yield return graph.Map("01b-default-simulated-notch", "authored-settings-simulated-insets", true);
            _session.actions.ui.safeAreaProvider = null;
            yield return graph.LockedRoom();
            yield return _session.Resize(1440, 900);
            yield return graph.Map("01c-default-desktop", "authored-settings", true);
            yield return _session.Resize(844, 390);
            yield return graph.Map("02-default-landscape", "authored-settings", true);
            yield return graph.SelectRoom(MapNodeType.Combat);
            yield return graph.PlacementBlocksMap();
            yield return graph.PlanningMap();
            yield return _session.Capture("03-combat-planning");
            yield return _session.NewExpedition();
            _session.mapFixture = new StageMapFixture(_session.ascension, true);
            _session.manifest.interventions.Add("Temporary cloned map generation settings: five floors, Combat / "
                + "Treasure / Combat / Elite / Rest / Boss; no RunState mutation, forced victory, reward injection "
                + "or authored asset write");
            _session.manifest.interventions.Add("Scripted ordinary party input: up to six living allies initially, "
                + "eight before the next combat and ten before Elite, using only remaining authored Deploy choices; "
                + "positions are chosen from visible free grid cells");
            _session.manifest.interventions.Add("Scripted battle assistance through usable Toolkit cards: check "
                + "heals every 0.4 seconds, prefer Heal group for multiple injured allies; after six seconds use "
                + "the shipped free Damage all enemy card at most every three seconds; resource effects and mana "
                + "consumers are observed, never granted");
            _session.manifest.interventions.Add("Targeted spells wait 0.2 seconds for the targeting HUD and camera "
                + "to settle, then use a two-frame touch with a refreshed collider aim inside the normal tap "
                + "movement threshold; targets and physics are not paused");
            UnityEngine.Random.InitState(StageMapFixture.Seed);
            yield return _session.actions.PointerTap("start-button");
            yield return StageMapActions.WaitForSelection(_session.actions);
            yield return graph.Map("05-fixture-portrait", "short-route-fixture", true);
            int roomEventsBefore = _session.manifest.roomSelections;
            int roundsBefore = _session.manifest.roundsStarted;
            yield return graph.SelectRoom(MapNodeType.Combat);
            _session.output.Check(_session.manifest.roomSelections == roomEventsBefore + 1
                && _session.manifest.roundsStarted == roundsBefore + 1,
                "New expedition dispatches one room selection and one round start after one Toolkit touch");
            yield return battle.Battle("06-fixture-combat", 3);
            yield return graph.Map("07-after-combat", "short-route-fixture", true);
            yield return graph.SelectRoom(MapNodeType.Treasure);
            yield return battle.Reward("08-treasure", 3);
            yield return graph.Map("09-after-treasure", "short-route-fixture", true);
            yield return graph.SelectRoom(MapNodeType.Combat);
            yield return battle.Battle("09b-fixture-second-combat", 3);
            yield return graph.SelectRoom(MapNodeType.Elite);
            // DataManager.GetWavePattern: an Elite room fights its floor's Elite pool, or that floor's Combat pool
            // when no Elite pool covers it. The authored Elite pool spans floors 5 to 7, past this fixture's floors.
            DataManager data = Object.FindAnyObjectByType<DataManager>();
            int eliteFloor = _session.ascension.run.currentFloor;
            List<WavePatternData> eliteWaves = data.GetWavePatterns(MapNodeType.Elite, eliteFloor);
            List<WavePatternData> expectedWaves = eliteWaves.Count > 0 ? eliteWaves
                : data.GetWavePatterns(MapNodeType.Combat, eliteFloor);
            WavePatternData selectedWave = StageMapReadout.Wave(_session.ascension);
            _session.output.Check(expectedWaves.Count > 0 && expectedWaves.Contains(selectedWave),
                "Elite room on floor " + eliteFloor + " selects from its " + (eliteWaves.Count > 0 ? "Elite" : "Combat")
                + " wave pool: " + (selectedWave != null ? selectedWave.name : "none"));
            if (eliteWaves.Count == 0)
            {
                _session.manifest.unobserved.Add("No authored Elite wave pool covers fixture floor " + eliteFloor
                    + ", so the Elite room fought that floor's Combat waves and the Elite pool was not exercised");
            }
            yield return graph.PlanningMap();
            yield return battle.Battle("10-fixture-elite", 4);
            yield return graph.Map("11-after-elite", "short-route-fixture", true);
            yield return RestRoom(graph);

            yield return graph.Map("12-after-rest", "short-route-fixture", true);
            yield return _session.Resize(844, 390);
            yield return graph.Map("13-boss-landscape", "short-route-fixture", true);
            yield return graph.SelectRoom(MapNodeType.Boss);
            _session.output.Check(!_session.ascension.IsOver() && LegacyUiReader.AscensionState(_session.ascension)
                != AscensionGameType.State.GameOver, "Entering the boss room starts a fight rather than ending the run");
            yield return battle.Boss("13b-boss");
            _session.output.Check(_session.ascension.IsOver() && _session.ascension.run.visitedNodes.Count == 6,
                "Beating the boss wins the run after all six selected fixture rooms");
            _session.output.Check(!StageInterfaceOutput.IsVisible(_session.actions.root.Q("map-panel"))
                && StageInterfaceOutput.IsVisible(_session.actions.root.Q("gameover-panel")),
                "Completed run closes map overlay and shows the end of the journey");
            // Recorded, not checked: our panel keeps its authored defeat title, his GameOverView says Victory!
            _session.manifest.unobserved.Add("Toolkit end title after a won run: \""
                + _session.actions.root.Q<Label>("gameover-title").text + "\"");
            yield return _session.Capture("14-run-complete");
            _session.output.Check(_session.actions.legacyModuleReadTouches,
                "Selected StandaloneInputModule consumed the map's synthetic touch samples");
            bool attemptedHealing = _session.manifest.spells.Exists(spell => spell.isHealing);
            _session.output.Check(!attemptedHealing || _session.manifest.spells.Exists(spell => spell.isHealing
                && spell.positiveHealth > 0f),
                "When injured allies prompted healing input, at least one ordinary cast produced observed positive "
                + "health consumers");
            if (!attemptedHealing)
            {
                _session.manifest.unobserved.Add("No healing input was needed: the fixture found no injured target "
                    + "while a healing card was usable");
            }

            _session.output.Check(!_session.manifest.spells.Exists(spell => !spell.isHealing)
                || _session.manifest.spells.Exists(spell => !spell.isHealing && spell.negativeHealth > 0f),
                "When damage assistance was used, an ordinary cast produced observed negative health consumers");
            _session.manifest.unobserved.Add("Physical phone input, Android safe insets and device performance are "
                + "not exercised");
            _session.manifest.unobserved.Add("The full authored ten-floor run is shown but not played to its boss; "
                + "special-room progression uses the labelled five-floor generation fixture");
        }

        // Entering a rest room restores mana, then parks the run on Julien's choice screen (Heal, or Resurrect when
        // someone is dead). The map comes back only after a choice, and the heal lands only on Heal.
        IEnumerator RestRoom(StageMapGraphProof graph)
        {
            StageEventChoice choice = new StageEventChoice(_session, new StageEventRoomRun.Manifest());
            ResourceAttribute mana = _session.manager.player.character.mana;
            using (StageRestObservation rest = new StageRestObservation(_session))
            {
                float manaBefore = mana.Value;
                int healingBefore = _session.manifest.restHealingEvents;
                int injured = rest.CountInjured();
                yield return graph.SelectRoom(MapNodeType.Rest);
                yield return choice.WaitForChoices("Rest");
                _session.output.Check(LegacyUiReader.AscensionState(_session.ascension)
                    == AscensionGameType.State.Rest && !StageInterfaceOutput.IsVisible(
                    _session.actions.root.Q("map-panel")), "Entering the rest room waits on Julien's choice screen "
                    + "and the map has not come back");
                _session.output.Check(_session.manifest.restHealingEvents == healingBefore,
                    "No ally healing has happened before the Heal choice: " + (_session.manifest.restHealingEvents
                    - healingBefore) + " events");
                // A proportional restore cannot move a full pool, so the check is not-less rather than more
                _session.output.Check(mana.Value >= manaBefore, "Entering the rest room does not lower mana: "
                    + manaBefore + " -> " + mana.Value);
                if (mana.Value <= manaBefore)
                {
                    _session.manifest.unobserved.Add("The rest mana restore was not observed to raise mana (" + manaBefore
                        + " -> " + mana.Value + " of " + mana.Max + "); a full pool does not move");
                }

                yield return _session.Capture("11b-rest-choices");
                yield return choice.Tap(choice.Choice("Heal"));
                yield return StageMapActions.WaitForSelection(_session.actions);
                if (injured > 0)
                {
                    _session.output.Check(_session.manifest.restHealingEvents > healingBefore,
                        "The Heal choice enters the original resource consumer flow for " + injured
                        + " injured surviving allies");
                }
                else
                {
                    _session.manifest.unobserved.Add("The rest heal was not observed: every surviving ally was already "
                        + "at full health, or none survived, when the rest room was entered");
                }
            }
        }
    }
}
