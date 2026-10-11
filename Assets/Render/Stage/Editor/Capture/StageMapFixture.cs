using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // A temporary generation configuration, used only by the native acceptance capture.
    // Ascension still generates its map and owns every room transition. No asset is edited.
    public class StageMapFixture : IDisposable
    {
        static readonly FieldInfo settingsField = typeof(AscensionGameType).GetField("_mapSettings",
            BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo seedField = typeof(AscensionGameType).GetField("_seed",
            BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo runField = typeof(AscensionGameType).GetField("_run",
            BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo randomField = typeof(AscensionGameType).GetField("_random",
            BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo seedUsedField = typeof(AscensionGameType).GetField("_seedUsed",
            BindingFlags.Instance | BindingFlags.NonPublic);
        readonly AscensionGameType _owner;
        readonly MapGenerationSettings _original;
        readonly int _originalSeed;
        public MapGenerationSettings settings { get; private set; }

        public static readonly int Seed = 271828;
        public StageMapFixture(AscensionGameType owner, bool shortRoute)
        {
            if (owner == null || settingsField == null || seedField == null)
            {
                throw new InvalidOperationException("Ascension map generation capture contract changed.");
            }

            _owner = owner;
            _original = (MapGenerationSettings)settingsField.GetValue(owner);
            _originalSeed = (int)seedField.GetValue(owner);
            if (_original == null)
            {
                throw new InvalidOperationException("Ascension has no map settings.");
            }

            settings = UnityEngine.Object.Instantiate(_original);
            settings.name = "Temporary capture map settings";
            settings.hideFlags = HideFlags.HideAndDontSave;
            if (shortRoute)
            {
                settings.floorCount = 5;
                settings.columnCount = 3;
                settings.pathCount = 4;
                settings.startRoomCount = 3;
                settings.maxRoomsPerFloor = 0;
                // Every floor fixed, so the route is combat, treasure, combat, elite, rest whatever the paths.
                // Floor 2 is no type's fixed floor and no type has weight, so it takes the map's combat fallback.
                // No event room: the capture has no event screen to walk through yet
                settings.roomTypes = new List<RoomTypeSettings>
                {
                    new RoomTypeSettings { type = MapNodeType.Combat, fixedFloors = new List<int> { 0 }, canFollowItself = true },
                    new RoomTypeSettings { type = MapNodeType.Treasure, fixedFloors = new List<int> { 1 } },
                    new RoomTypeSettings { type = MapNodeType.Elite, fixedFloors = new List<int> { 3 }, firstFloor = 3 },
                    new RoomTypeSettings { type = MapNodeType.Rest, fixedFloors = new List<int> { 4 }, firstFloor = 4 },
                    new RoomTypeSettings { type = MapNodeType.Event },
                };
                settings.eventRooms.Clear();
            }

            settingsField.SetValue(owner, settings);
            seedField.SetValue(owner, Seed);
            Regenerate(owner, settings);
        }

        // For a capture that edits settings after construction: generates the map again from them, under the same
        // first-room condition
        public void Reapply()
        {
            Regenerate(_owner, settings);
        }

        // The run starts with the scene (GameManager.Start), so the map is usually generated before the capture can
        // install its configuration: while the first room is still to choose, the map is generated again from it
        static void Regenerate(AscensionGameType owner, MapGenerationSettings settings)
        {
            if (owner.state != AscensionGameType.State.SelectRoom || owner.run == null || owner.run.currentNode != null)
            {
                return;
            }

            if (runField == null || randomField == null || seedUsedField == null)
            {
                throw new InvalidOperationException("Ascension run generation capture contract changed.");
            }

            randomField.SetValue(owner, new System.Random(Seed));
            seedUsedField.SetValue(owner, Seed);
            runField.SetValue(owner, new RunState(MapGenerator.Generate(settings, Seed)));
            MapView mapView = UnityEngine.Object.FindAnyObjectByType<MapView>(FindObjectsInactive.Include);
            if (mapView != null)
            {
                mapView.Display(owner.run, true);
            }
        }

        public void Dispose()
        {
            if (settings == null)
            {
                return;
            }

            if (_owner != null)
            {
                settingsField.SetValue(_owner, _original);
                seedField.SetValue(_owner, _originalSeed);
            }

            if (settings != null)
            {
                UnityEngine.Object.DestroyImmediate(settings);
            }

            settings = null;
        }
    }
}
