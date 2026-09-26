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
            List<WavePatternData> eliteWaves
                = Object.FindAnyObjectByType<DataManager>().GetWavePatterns(MapNodeType.Elite,
                _session.ascension.run.currentFloor);
            WavePatternData selectedWave = StageMapReadout.Wave(_session.ascension);
            _session.output.Check(eliteWaves.Count > 0 && eliteWaves.Contains(selectedWave),
                "Elite room selects an authored Elite wave pool entry without Combat fallback");
            yield return graph.PlanningMap();
            yield return battle.Battle("10-fixture-elite", 4);
            yield return graph.Map("11-after-elite", "short-route-fixture", true);
            using (StageRestObservation rest = new StageRestObservation(_session))
            {
                yield return graph.SelectRoom(MapNodeType.Rest);
                yield return StageMapActions.WaitForSelection(_session.actions);
                _session.output.Check(_session.manifest.restHealingEvents > 0,
                    "Rest enters the original resource consumer flow for surviving allies");
            }

            yield return graph.Map("12-after-rest", "short-route-fixture", true);
            yield return _session.Resize(844, 390);
            yield return graph.Map("13-boss-landscape", "short-route-fixture", true);
            yield return graph.SelectRoom(MapNodeType.Boss);
            _session.output.Check(_session.ascension.IsOver() && _session.ascension.run.visitedNodes.Count == 6,
                "Original boss placeholder ends the run after all six selected fixture rooms");
            _session.output.Check(!StageInterfaceOutput.IsVisible(_session.actions.root.Q("map-panel")),
                "Completed run closes map overlay");
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
    }
}
