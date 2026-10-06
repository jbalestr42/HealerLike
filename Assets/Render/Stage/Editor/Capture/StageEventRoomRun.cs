using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;
using UiButton = UnityEngine.UI.Button;

namespace HealerLike.Render.Stage
{
    // Plays a rest room then an event room of the merged game under the Toolkit HUD, taking Julien's uGUI choices
    // by touch, and reads what each choice changed in the run. Also measures how often the authored map settings
    // draw an event room.
    public sealed class StageEventRoomRun : AStageRun
    {
        public static readonly string Mode = "event-rooms";

        [Serializable]
        public class Manifest
        {
            public string revision = StagePlay.ReadRevision();
            public bool isPassed;
            public string input = "Multi-frame synthetic Touch samples through StandaloneInputModule and "
                + "StageTouchInput; no Android OS input";
            public List<string> interventions = new List<string>();
            public List<string> enabledHudButtons = new List<string>();
            public List<string> choiceHits = new List<string>();
            public List<string> hudHits = new List<string>();
            public List<string> clicks = new List<string>();
            public float manaBeforeRest;
            public float manaInRest;
            public float manaMax;
            public string resurrected;
            public int unitsBeforeResurrect;
            public int unitsAfterResurrect;
            public string libraryItem;
            public int itemsBeforeLibrary;
            public int itemsAfterLibrary;
            public int sampledMaps;
            public int sampledRooms;
            public int sampledEventRooms;
            public int mapsWithoutEventRoom;
            public List<string> roomShares = new List<string>();
        }

        readonly string _folder = Path.Combine(StagePlay.CaptureFolder, "event-rooms");
        readonly Manifest _manifest = new Manifest();
        readonly StageInterfaceOutput _output;
        StageCaptureSession _session;
        protected override bool shouldStartGame => false;

        public StageEventRoomRun()
        {
            _output = new StageInterfaceOutput(_folder);
        }

        protected override void OnFailed(Exception error)
        {
            _output.Fail(error.ToString());
            Write(false);
            base.OnFailed(error);
        }

        protected override IEnumerator Run()
        {
            bool passed = false;
            using var errors = new StageCaptureErrors();
            try
            {
                _session = new StageCaptureSession(_manager, _output);
                _session.AttachInput();
                yield return _session.Resize(1080, 1920);
                AscensionGameType ascension = Object.FindAnyObjectByType<AscensionGameType>();
                _session.mapFixture = new StageMapFixture(ascension, false);
                MeasureShares(_session.mapFixture.settings);
                Configure(_session.mapFixture.settings);
                yield return _session.actions.PointerTap("start-button");
                yield return Wait(0.8f);
                yield return StageMapActions.WaitForSelection(_session.actions);
                StageEventChoice choice = new StageEventChoice(_session, _manifest);
                yield return Rest(ascension, choice);
                yield return Library(ascension, choice);
                _output.Check(errors.count == 0, "Native runtime errors: " + errors.count + "\n"
                    + string.Join("\n", errors.messages));
                passed = true;
            }
            finally
            {
                _session?.Dispose();
                _output.Write(passed);
                Write(passed);
                StagePlay.Finish(this, passed);
            }
        }

        // Rest on the first floor, the Library on the second, then the boss. The authored settings are cloned
        // by StageMapFixture, nothing is written to the asset
        void Configure(MapGenerationSettings settings)
        {
            settings.floorCount = 2;
            settings.columnCount = 3;
            settings.pathCount = 3;
            settings.startRoomCount = 3;
            settings.maxRoomsPerFloor = 0;
            settings.roomTypes = new List<RoomTypeSettings>
            {
                new RoomTypeSettings { type = MapNodeType.Rest, fixedFloors = new List<int> { 0 } },
                new RoomTypeSettings { type = MapNodeType.Event, fixedFloors = new List<int> { 1 } },
                new RoomTypeSettings { type = MapNodeType.Combat, canFollowItself = true },
            };
            EventRoomChance library = settings.eventRooms.Find(chance =>
                chance.eventRoom is LibraryEventRoom room && !room.isDark);
            if (library == null)
            {
                throw new InvalidOperationException("The authored map settings no longer list the Library event");
            }

            settings.eventRooms = new List<EventRoomChance>
            {
                new EventRoomChance { eventRoom = library.eventRoom, weight = 1f },
            };
            _manifest.interventions.Add("Cloned authored map settings: two floors, Rest then the Library event, "
                + "then the boss; seed 271828");
        }

        IEnumerator Rest(AscensionGameType ascension, StageEventChoice choice)
        {
            // A fresh run has nobody dead, and Resurrect is the one rest choice whose effect is readable without a
            // fight: one dead ally is listed in the run, as a lost fight would
            IReadOnlyList<EntityData> recruits = ascension.GetRewardEntities();
            _output.Check(recruits.Count > 0, "The class has units to resurrect");
            EntityData ally = recruits[0];
            ascension.run.AddDeadAlly(ally);
            _manifest.interventions.Add("RunState.AddDeadAlly(" + ally.title + ") before the rest room, so "
                + "Resurrect is available without a lost fight");
            ResourceAttribute mana = _manager.player.character.mana;
            _manifest.manaBeforeRest = mana.Value;
            _manifest.manaMax = mana.Max;
            EntityInventory units = Object.FindAnyObjectByType<UIManager>()
                .GetView<GameView>(ViewType.Game).entityInventory;

            yield return StageMapActions.SelectFirst(_session.actions, true);
            _output.Check(ascension.run.currentNode.type == MapNodeType.Rest, "The first room is the rest room");
            yield return choice.WaitForChoices("Rest");
            _manifest.manaInRest = mana.Value;
            _output.Check(LegacyUiReader.AscensionState(ascension) == AscensionGameType.State.Rest,
                "Entering the rest room waits on Julien's choice screen");
            yield return _session.Capture("01-rest-choices");
            UiButton resurrect = choice.Choice("Resurrect");
            yield return choice.CheckSeam("rest", resurrect);
            yield return choice.Tap(resurrect);
            yield return choice.WaitForChoices("Resurrect");
            yield return _session.Capture("02-resurrect-choices");

            _manifest.unitsBeforeResurrect = units.entityButtons.Count;
            UiButton target = choice.Choice(ally.title);
            yield return choice.CheckSeam("resurrect", target);
            yield return choice.Tap(target);
            yield return StageMapActions.WaitForSelection(_session.actions);
            _manifest.unitsAfterResurrect = units.entityButtons.Count;
            _manifest.resurrected = ally.title;
            _output.Check(ascension.run.deadAllies.Count == 0, "The touched Resurrect choice took "
                + ally.title + " off the dead allies");
            _output.Check(_manifest.unitsAfterResurrect == _manifest.unitsBeforeResurrect + 1
                && units.entityButtons[units.entityButtons.Count - 1].data == ally,
                "The touched Resurrect choice put " + ally.title + " back in the unit inventory");
            _output.Check(!choice.view.gameObject.activeSelf
                && LegacyUiReader.AscensionState(ascension) == AscensionGameType.State.SelectRoom,
                "After the choice Julien's screen closed and the run offers the next room");
            yield return _session.Capture("03-after-rest");
        }

        IEnumerator Library(AscensionGameType ascension, StageEventChoice choice)
        {
            InventoryHandler inventory = _manager.player.character.inventoryHandler;
            yield return StageMapActions.SelectFirst(_session.actions, true);
            _output.Check(ascension.run.currentNode.type == MapNodeType.Event, "The second room is the event room");
            yield return choice.WaitForChoices(null);
            _output.Check(LegacyUiReader.AscensionState(ascension) == AscensionGameType.State.PlayEvent,
                "Entering the event room waits on Julien's choice screen");
            yield return _session.Capture("04-library-choices");
            UiButton item = null;
            foreach (UiButton button in choice.view.choiceButtons)
            {
                if (button.name != "Choice Leave" && button.IsInteractable())
                {
                    item = button;
                    break;
                }
            }

            _output.Check(item != null, "The Library offers at least one item");
            string title = item.name.Substring("Choice ".Length);
            _manifest.itemsBeforeLibrary = inventory.items.Count;
            yield return choice.CheckSeam("library", item);
            yield return choice.Tap(item);
            yield return StageMapActions.WaitForSelection(_session.actions);
            _manifest.itemsAfterLibrary = inventory.items.Count;
            _manifest.libraryItem = title;
            _output.Check(_manifest.itemsAfterLibrary == _manifest.itemsBeforeLibrary + 1
                && inventory.items[inventory.items.Count - 1].item.title == title,
                "The touched Library choice added " + title + " to the character's items");
            _output.Check(!choice.view.gameObject.activeSelf
                && LegacyUiReader.AscensionState(ascension) == AscensionGameType.State.SelectRoom,
                "After the event Julien's screen closed and the run offers the boss");
            yield return _session.Capture("05-after-library");
        }

        // The share of each room type over many maps of the authored settings, the boss left out
        void MeasureShares(MapGenerationSettings settings)
        {
            Dictionary<MapNodeType, int> counts = new Dictionary<MapNodeType, int>();
            for (int seed = 1; seed <= 1000; seed++)
            {
                int events = 0;
                RunMap map = MapGenerator.Generate(settings, seed);
                foreach (MapNode node in map.GetAllNodes())
                {
                    if (node.type == MapNodeType.Boss)
                    {
                        continue;
                    }

                    counts.TryGetValue(node.type, out int count);
                    counts[node.type] = count + 1;
                    _manifest.sampledRooms++;
                    events += node.type == MapNodeType.Event ? 1 : 0;
                }

                _manifest.sampledEventRooms += events;
                _manifest.mapsWithoutEventRoom += events == 0 ? 1 : 0;
                _manifest.sampledMaps++;
            }

            foreach (KeyValuePair<MapNodeType, int> pair in counts)
            {
                _manifest.roomShares.Add(pair.Key + " " + pair.Value + " ("
                    + (100f * pair.Value / _manifest.sampledRooms).ToString("0.0") + "%)");
            }

            Debug.Log("[StageEventRoomRun] Room shares over " + _manifest.sampledMaps + " maps: "
                + string.Join(", ", _manifest.roomShares));
        }

        void Write(bool passed)
        {
            _manifest.isPassed = passed && _output.manifest.failures.Count == 0;
            Directory.CreateDirectory(_folder);
            File.WriteAllText(Path.Combine(_folder, "event-rooms.json"), JsonUtility.ToJson(_manifest, true));
        }
    }
}
