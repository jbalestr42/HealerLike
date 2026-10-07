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
