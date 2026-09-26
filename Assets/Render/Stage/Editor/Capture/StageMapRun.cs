using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Stage
{
    // Ordinary Toolkit input drives every room, battle and reward transition. A short generation
    // fixture makes special rooms reachable without changing gameplay state or awarding a forced win.
    public sealed class StageMapRun : AStageRun
    {
        [Serializable] public sealed class MapFrame
        {
            public string name;
            public string scenario;
            public int width;
            public int height;
            public Rect dialog;
            public Rect viewport;
            public Rect graph;
            public int currentFloor;
            public int visited;
            public int expectedEdges;
            public int drawnEdges;
            public List<Room> rooms = new List<Room>();
        }

        [Serializable] public sealed class Room
        {
            public int floor;
            public int column;
            public string type;
            public string state;
            public bool enabled;
            public Rect bounds;
        }

        [Serializable] public sealed class Manifest
        {
            public string revision = Environment.GetEnvironmentVariable("RENDER_CAPTURE_REVISION") ?? "unspecified";
            public bool isPassed;
            public string input = "Multi-frame synthetic Touch samples through StandaloneInputModule and StageTouchInput; no Android OS input";
            public List<string> interventions = new List<string>();
            public List<string> unobserved = new List<string>();
            public List<MapFrame> maps = new List<MapFrame>();
            public int roomSelections;
            public int roundsStarted;
            public int battlesStarted;
            public int restHealingEvents;
        }

        readonly string _folder = Path.Combine(StagePlay.CaptureFolder, "expedition-map");
        readonly Manifest _manifest = new Manifest();
        readonly StageInterfaceActions _actions = new StageInterfaceActions();
        readonly StageInterfaceOutput _output;
        StageGameViewSize _size;
        StageMapFixture _fixture;
        AscensionGameType _ascension;
        MapView _mapView;
        int _roomEvents;
        int _roundEvents;
        int _battleEvents;

        public StageMapRun() { _output = new StageInterfaceOutput(_folder); }
        protected override bool shouldStartGame { get { return false; } }

        protected override void OnFailed(Exception error)
        {
            _output.Fail(error.ToString());
            Write(false);
            base.OnFailed(error);
        }

        protected override IEnumerator Run()
        {
            bool passed = false;
            AscensionGameType.OnRoundStart.AddListener(OnRoundStart);
            AscensionGameType.OnBattleStart.AddListener(OnBattleStart);
            try
            {
                _output.Check(Regex.IsMatch(_manifest.revision, "^[0-9a-fA-F]{40}$"), "Exact capture revision recorded");
                Attach();
                _fixture = new StageMapFixture(_ascension, false);
                _manifest.interventions.Add("Default authored map settings cloned without layout changes; seed fixed to 271828 in this capture only");
                UnityEngine.Random.InitState(StageMapFixture.Seed);
                yield return Resize(1080, 1920);
                yield return _actions.PointerTap("start-button");
                yield return StageMapActions.WaitForSelection(_actions);
                yield return Map("01-default-portrait", "authored-settings", true);
                _actions.ui.safeAreaProvider = () => new Rect(0f, 34f / 844f, 1f, 1f - 78f / 844f);
                yield return Wait(0.3f);
                yield return Map("01b-default-simulated-notch", "authored-settings-simulated-insets", true);
                _actions.ui.safeAreaProvider = null;
                yield return LockedRoom();
                yield return Resize(1440, 900);
                yield return Map("01c-default-desktop", "authored-settings", true);
                yield return Resize(844, 390);
                yield return Map("02-default-landscape", "authored-settings", true);
                yield return SelectRoom(MapNodeType.Combat);
                yield return PlacementBlocksMap();
                yield return PlanningMap();
                yield return Capture("03-combat-planning");
                yield return NewExpedition();

                _fixture = new StageMapFixture(_ascension, true);
                _manifest.interventions.Add("Temporary cloned map generation settings: five floors, Combat / Treasure / Combat / Elite / Rest / Boss; no RunState mutation, forced victory, reward injection or authored asset write");
                UnityEngine.Random.InitState(StageMapFixture.Seed);
                yield return _actions.PointerTap("start-button");
                yield return StageMapActions.WaitForSelection(_actions);
                yield return Map("05-fixture-portrait", "short-route-fixture", true);
                int roomEventsBefore = _roomEvents;
                int roundsBefore = _roundEvents;
                yield return SelectRoom(MapNodeType.Combat);
                _output.Check(_roomEvents == roomEventsBefore + 1 && _roundEvents == roundsBefore + 1,
                    "New expedition dispatches one room selection and one round start after one Toolkit touch");
                yield return DeployParty();
                yield return Battle("06-fixture-combat", 3);
                yield return Map("07-after-combat", "short-route-fixture", true);
                yield return SelectRoom(MapNodeType.Treasure);
                yield return Reward("08-treasure", 3);
                yield return Map("09-after-treasure", "short-route-fixture", true);
                yield return SelectRoom(MapNodeType.Combat);
                yield return Battle("09b-fixture-second-combat", 3);
                yield return SelectRoom(MapNodeType.Elite);
                List<WavePatternData> eliteWaves = Object.FindAnyObjectByType<DataManager>().GetWavePatterns(MapNodeType.Elite, _ascension.run.currentFloor);
                WavePatternData selectedWave = (WavePatternData)typeof(AscensionGameType).GetField("_currentWave",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(_ascension);
                _output.Check(eliteWaves.Count > 0 && eliteWaves.Contains(selectedWave),
                    "Elite room selects an authored Elite wave pool entry without Combat fallback");
                yield return PlanningMap();
                yield return Battle("10-fixture-elite", 4);
                yield return Map("11-after-elite", "short-route-fixture", true);
                foreach (GameObject ally in _manager.entityManager.GetEntities(Entity.EntityType.Player))
                    ally.GetComponent<Entity>().health.OnAllConsumerProcessed.AddListener(OnRestConsumer);
                yield return SelectRoom(MapNodeType.Rest);
                yield return StageMapActions.WaitForSelection(_actions);
                _output.Check(_manifest.restHealingEvents > 0, "Rest enters the original resource consumer flow for surviving allies");
                foreach (GameObject ally in _manager.entityManager.GetEntities(Entity.EntityType.Player))
                    ally.GetComponent<Entity>().health.OnAllConsumerProcessed.RemoveListener(OnRestConsumer);
                yield return Map("12-after-rest", "short-route-fixture", true);
                yield return Resize(844, 390);
                yield return Map("13-boss-landscape", "short-route-fixture", true);
                yield return SelectRoom(MapNodeType.Boss);
                _output.Check(_ascension.IsOver() && _ascension.run.visitedNodes.Count == 6,
                    "Original boss placeholder ends the run after all six selected fixture rooms");
                _output.Check(!StageInterfaceOutput.IsVisible(_actions.root.Q("map-panel")), "Completed run closes map overlay");
                yield return Capture("14-run-complete");
                _output.Check(_actions.legacyModuleReadTouches, "Selected StandaloneInputModule consumed the map's synthetic touch samples");
                _manifest.unobserved.Add("Physical phone input, Android safe insets and device performance are not exercised");
                _manifest.unobserved.Add("The full authored ten-floor run is shown but not played to its boss; special-room progression uses the labelled five-floor generation fixture");
                passed = true;
            }
            finally
            {
                AscensionGameType.OnRoundStart.RemoveListener(OnRoundStart);
                AscensionGameType.OnBattleStart.RemoveListener(OnBattleStart);
                if (_mapView != null) _mapView.OnNodeSelected.RemoveListener(OnRoomSelected);
                _fixture?.Dispose();
                _size?.Dispose();
                Time.timeScale = 1f;
                _output.Write(passed);
                Write(passed);
                StagePlay.Finish(this, passed);
            }
        }

        void Attach()
        {
            _actions.ui = Object.FindAnyObjectByType<ToolkitGameUI>();
            _output.Check(_actions.ui != null && Object.FindObjectsByType<ToolkitGameUI>(FindObjectsSortMode.None).Length == 1,
                "Exactly one live Toolkit host");
            _actions.ConfigureLegacyInput();
            _ascension = Object.FindAnyObjectByType<AscensionGameType>();
            _mapView = Object.FindAnyObjectByType<UIManager>()?.GetView<MapView>(ViewType.Map);
            if (_mapView != null) _mapView.OnNodeSelected.AddListener(OnRoomSelected);
        }

        IEnumerator Resize(int width, int height)
        {
            _size?.Dispose();
            _size = new StageGameViewSize(width, height);
            yield return Wait(0.7f);
            _output.Check(Screen.width == width && Screen.height == height, "Map viewport is " + width + "x" + height);
        }

        IEnumerator Capture(string name) { yield return _output.Capture(_actions.ui, name); }

        IEnumerator Map(string name, string scenario, bool canTravel)
        {
            yield return Wait(0.25f);
            VisualElement dialog = _actions.root.Q("map-dialog");
            ScrollView scroll = _actions.root.Q<ScrollView>("map-scroll");
            VisualElement canvas = _actions.root.Q("map-canvas");
            _output.Check(StageInterfaceOutput.IsVisible(dialog) && scroll != null && canvas != null,
                "Toolkit graph and map dialog are visible: " + name);
            _output.Check(Contains(_actions.root.worldBound, dialog.worldBound), "Map dialog remains within screen: " + name);
            _output.Check(LegacyUiReader.CurrentView(Object.FindAnyObjectByType<UIManager>()) == ViewType.Map,
                "Map is backed by the original UI view stack: " + name);
            foreach (Canvas legacy in _mapView.GetComponentsInChildren<Canvas>(true))
                _output.Check(!legacy.enabled || !legacy.gameObject.activeInHierarchy,
                    "Original map canvas stays suppressed: " + legacy.name);
            Canvas parentCanvas = _mapView.GetComponentInParent<Canvas>();
            _output.Check(parentCanvas != null && (!parentCanvas.enabled || !parentCanvas.gameObject.activeInHierarchy),
                "The original map parent canvas is present and suppressed");
            RunState run = _ascension.run;
            MapFrame frame = new MapFrame { name = name, scenario = scenario, width = Screen.width, height = Screen.height,
                dialog = dialog.worldBound, viewport = scroll.contentViewport.worldBound, graph = canvas.worldBound,
                currentFloor = run.currentFloor, visited = run.visitedNodes.Count };
            int count = 0;
            foreach (MapNode node in run.map.GetAllNodes())
            {
                Button button = StageMapActions.ButtonFor(_actions, node);
                MapNodeState state = run.GetNodeState(node);
                _output.Check(button != null && ReferenceEquals(button.userData, node), "Room button retains original gameplay node identity: " + node);
                _output.Check(button.enabledInHierarchy == (canTravel && run.CanTravelTo(node)), "Room availability matches live RunState: " + node);
                _output.Check(button.ClassListContains("is-" + state.ToString().ToLowerInvariant()), "Room style matches live visit state: " + node);
                _output.Check(Contains(canvas.worldBound, button.worldBound), "Room button remains within scrollable graph: " + node);
                _output.Check(button.worldBound.width >= 44f && button.worldBound.height >= 44f,
                    "Room touch target is at least 44 panel units: " + node);
                frame.rooms.Add(new Room { floor = node.floor, column = node.column, type = node.type.ToString(),
                    state = state.ToString(), enabled = button.enabledInHierarchy, bounds = button.worldBound });
                frame.expectedEdges += node.next.Count;
                count++;
            }
            _output.Check(canvas.Query<Button>(className: "map-node").ToList().Count == count,
                "Map renders exactly one button per live room");
            ToolkitMapConnections connections = canvas.Q<ToolkitMapConnections>("map-connections");
            _output.Check(connections != null && connections.edgeCount == frame.expectedEdges,
                "Visible path painter carries every live directed map connection");
            frame.drawnEdges = connections.edgeCount;
            _manifest.maps.Add(frame);
            yield return Capture(name);
        }

        IEnumerator LockedRoom()
        {
            MapNode locked = _ascension.run.map.floors[1][0];
            Button button = StageMapActions.ButtonFor(_actions, locked);
            yield return _actions.BringIntoView(button);
            _output.Check(!button.enabledInHierarchy, "Unreachable room is disabled");
            yield return _actions.TouchGesture(StageInterfaceActions.ScreenPoint(button));
            yield return Wait(0.2f);
            _output.Check(_ascension.run.currentNode == null && _ascension.run.visitedNodes.Count == 0 && _roomEvents == 0,
                "Touching a locked room cannot travel or dispatch a room selection");
        }

        IEnumerator SelectRoom(MapNodeType expected)
        {
            RunState run = _ascension.run;
            int visited = run.visitedNodes.Count;
            int events = _roomEvents;
            _output.Check(run.GetAvailableNodes().Count > 0 && run.GetAvailableNodes()[0].type == expected,
                "Next capture room is " + expected);
            yield return StageMapActions.SelectFirst(_actions, true);
            _output.Check(run.currentNode.type == expected && run.visitedNodes.Count == visited + 1 && _roomEvents == events + 1,
                "One eligible touch selects exactly one " + expected + " room through original MapView event");
        }

        IEnumerator PlanningMap()
        {
            int visited = _ascension.run.visitedNodes.Count;
            int events = _roomEvents;
            yield return _actions.PointerTap("map-button");
            yield return Wait(0.3f);
            yield return Map("planning-map-" + _roundEvents + "-" + Screen.width, "inspect-only", false);
            Button current = StageMapActions.ButtonFor(_actions, _ascension.run.currentNode);
            yield return _actions.BringIntoView(current);
            int entities = Object.FindObjectsByType<Entity>(FindObjectsSortMode.None).Length;
            yield return _actions.TouchGesture(StageInterfaceActions.ScreenPoint(current));
            _output.Check(_ascension.run.visitedNodes.Count == visited && _roomEvents == events,
                "Inspect-only map touch cannot advance travel");
            _output.Check(Object.FindObjectsByType<Entity>(FindObjectsSortMode.None).Length == entities
                && Object.FindAnyObjectByType<InteractionManager>().GetInteraction() == null,
                "Map-origin input neither deploys nor selects a battlefield entity");
            yield return _actions.PointerTap("map-close-button");
            yield return Wait(0.3f);
            _output.Check(!StageInterfaceOutput.IsVisible(_actions.root.Q("map-panel"))
                && LegacyUiReader.AscensionState(_ascension) == AscensionGameType.State.WaitForRoundToStart,
                "Map Back returns to the same combat preparation");
        }

        IEnumerator PlacementBlocksMap()
        {
            yield return _actions.PointerTap("party-button");
            yield return Wait(0.2f);
            yield return _actions.SelectCardByTouch(_actions.Cards("party-list")[0]);
            yield return Wait(0.2f);
            InteractionManager interaction = Object.FindAnyObjectByType<InteractionManager>();
            _output.Check(interaction.GetInteraction() is EntityGridInteraction && _manager.placement.preview != null,
                "Party touch begins creature placement with the generated preview");
            Button map = _actions.root.Q<Button>("map-button");
            _output.Check(!map.enabledInHierarchy, "Map inspection is disabled during active placement");
            int entities = Object.FindObjectsByType<Entity>(FindObjectsSortMode.None).Length;
            yield return _actions.TouchGesture(StageInterfaceActions.ScreenPoint(map));
            _output.Check(!StageInterfaceOutput.IsVisible(_actions.root.Q("map-panel"))
                && Object.FindObjectsByType<Entity>(FindObjectsSortMode.None).Length == entities,
                "Touching unavailable Map neither opens a modal nor deploys through the HUD");
            yield return _actions.PointerTap("cancel-button");
            yield return Wait(0.2f);
            _output.Check(interaction.GetInteraction() == null && _manager.placement.preview == null,
                "Cancel releases creature placement before map inspection");
        }

        IEnumerator DeployParty()
        {
            Vector3[] offsets = { Vector3.left * 2f, Vector3.left * 3f + Vector3.back, Vector3.left * 2f + Vector3.forward * 2f,
                Vector3.left * 4f + Vector3.forward, Vector3.left * 4f + Vector3.back * 2f, Vector3.left * 3f + Vector3.forward * 3f };
            for (int i = 0; i < offsets.Length; i++)
            {
                yield return _actions.PointerTap("party-button");
                yield return Wait(0.2f);
                List<Button> cards = _actions.Cards("party-list").FindAll(button => button.enabledInHierarchy
                    && button.Q<Label>("card-status").text == "Deploy");
                if (cards.Count == 0)
                {
                    yield return _actions.PointerTap("party-close-button");
                    break;
                }
                yield return _actions.SelectCardByTouch(cards[Mathf.Min(i, cards.Count - 1)]);
                int before = _manager.entityManager.GetEntities(Entity.EntityType.Player).Count;
                Vector3 point = _manager.player.grid.GetNearestWalkablePosition(offsets[i]);
                yield return _actions.TouchGesture(_manager.gameCamera.WorldToScreenPoint(point));
                yield return Wait(0.4f);
                _output.Check(_manager.entityManager.GetEntities(Entity.EntityType.Player).Count == before + 1,
                    "Ordinary Toolkit party and board touches deploy fixture ally " + i);
            }
            _output.Check(_manager.entityManager.GetEntities(Entity.EntityType.Player).Count >= 3,
                "At least three allies were deployed through the ordinary party interface");
        }

        IEnumerator Battle(string prefix, int rewards)
        {
            int starts = _battleEvents;
            yield return _actions.PointerTap("wave-button");
            yield return Wait(0.3f);
            _output.Check(_battleEvents == starts + 1, "One battle button touch dispatches one battle start");
            yield return Capture(prefix + "-battle");
            float deadline = Time.realtimeSinceStartup + 120f;
            float nextHeal = 0f;
            while (!StageInterfaceOutput.IsVisible(_actions.root.Q("upgrade-panel")))
            {
                _output.Check(!StageInterfaceOutput.IsVisible(_actions.root.Q("gameover-panel")), "Party survives " + prefix);
                _output.Check(Time.realtimeSinceStartup < deadline, "Natural battle reaches reward within 120 seconds: " + prefix);
                if (Time.realtimeSinceStartup >= nextHeal)
                {
                    nextHeal = Time.realtimeSinceStartup + 2f;
                    yield return HealThroughInterface();
                }
                yield return Wait(0.5f);
            }
            yield return Reward(prefix + "-reward", rewards);
        }

        IEnumerator HealThroughInterface()
        {
            Entity target = null;
            foreach (GameObject ally in _manager.entityManager.GetEntities(Entity.EntityType.Player))
            {
                Entity entity = ally.GetComponent<Entity>();
                if (entity.health.percent < 0.8f && (target == null || entity.health.percent < target.health.percent))
                    target = entity;
            }
            if (target == null) yield break;
            foreach (Button card in _actions.Cards("spell-list"))
            {
                string title = card.Q<Label>("card-title")?.text ?? "";
                if (!card.enabledInHierarchy || title.IndexOf("heal", StringComparison.OrdinalIgnoreCase) < 0) continue;
                yield return _actions.SelectCardByTouch(card);
                InteractionManager interaction = Object.FindAnyObjectByType<InteractionManager>();
                AInteraction spell = interaction.GetInteraction();
                if (target != null && spell != null && spell.IsValidTarget(target.gameObject))
                {
                    yield return _actions.TouchGesture(_manager.gameCamera.WorldToScreenPoint(RenderTargets.Point(target.gameObject)));
                    yield return Wait(0.15f);
                }
                if (interaction.GetInteraction() != null)
                    yield return _actions.PointerTap("cancel-button");
                yield break;
            }
        }

        IEnumerator Reward(string name, int expectedChoices)
        {
            yield return Wait(0.3f);
            List<Button> rewards = _actions.Cards("upgrade-list");
            _output.Check(StageInterfaceOutput.IsVisible(_actions.root.Q("upgrade-panel")) && rewards.Count == expectedChoices,
                "Original room reward offers " + expectedChoices + " choices: " + name);
            yield return Capture(name);
            yield return _actions.SelectCardByTouch(rewards[0]);
            yield return StageMapActions.WaitForSelection(_actions);
            _output.Check(!StageInterfaceOutput.IsVisible(_actions.root.Q("upgrade-panel")), "Reward touch returns to expedition map");
        }

        IEnumerator NewExpedition()
        {
            MapView oldMap = _mapView;
            oldMap.OnNodeSelected.RemoveListener(OnRoomSelected);
            RenderManager original = _manager;
            yield return _actions.PointerTap("pause-button");
            yield return Wait(0.2f);
            yield return _actions.PointerTap("menu-button");
            yield return Wait(1f);
            _fixture.Dispose();
            _fixture = null;
            Attach();
            _output.Check(oldMap == null && SceneManager.GetActiveScene().path == StageInterface.MenuPath,
                "Menu destroys the previous gameplay map and scene");
            yield return Capture("04-menu");
            yield return _actions.PointerTap("start-button");
            yield return Wait(1.2f);
            Attach();
            _output.Check(_manager == original && _ascension != null && _mapView != null,
                "New expedition reuses its RenderManager with a fresh gameplay map");
            _output.Check(Object.FindObjectsByType<UIManager>(FindObjectsSortMode.None).Length == 1
                && Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length == 1,
                "Scene restart leaves one UI manager and one game manager");
            yield return Resize(1080, 1920);
        }

        void OnRoomSelected(MapNode node) { _roomEvents++; _manifest.roomSelections++; }
        void OnRoundStart() { _roundEvents++; _manifest.roundsStarted++; }
        void OnBattleStart() { _battleEvents++; _manifest.battlesStarted++; }
        void OnRestConsumer(GameObject owner, ResourceModifier modifier, float value, bool critical)
        {
            if (value > 0f) _manifest.restHealingEvents++;
        }

        static bool Contains(Rect outer, Rect inner)
        {
            return inner.xMin >= outer.xMin - 1f && inner.yMin >= outer.yMin - 1f
                && inner.xMax <= outer.xMax + 1f && inner.yMax <= outer.yMax + 1f;
        }

        void Write(bool passed)
        {
            _manifest.isPassed = passed && _output.manifest.failures.Count == 0;
            Directory.CreateDirectory(_folder);
            File.WriteAllText(Path.Combine(_folder, "map.json"), JsonUtility.ToJson(_manifest, true));
        }
    }
}
