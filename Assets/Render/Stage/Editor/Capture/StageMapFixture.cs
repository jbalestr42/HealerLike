using System;
using System.Reflection;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // A temporary generation configuration, used only by the native acceptance capture.
    // Ascension still generates its map and owns every room transition. No asset is edited.
    public sealed class StageMapFixture : IDisposable
    {
        static readonly FieldInfo SettingsField = typeof(AscensionGameType).GetField("_mapSettings",
            BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo SeedField = typeof(AscensionGameType).GetField("_seed",
            BindingFlags.Instance | BindingFlags.NonPublic);
        readonly AscensionGameType _owner;
        readonly MapGenerationSettings _original;
        readonly int _originalSeed;
        public MapGenerationSettings settings { get; private set; }
        public const int Seed = 271828;

        public StageMapFixture(AscensionGameType owner, bool shortRoute)
        {
            if (owner == null || SettingsField == null || SeedField == null)
                throw new InvalidOperationException("Ascension map generation capture contract changed.");
            _owner = owner;
            _original = (MapGenerationSettings)SettingsField.GetValue(owner);
            _originalSeed = (int)SeedField.GetValue(owner);
            if (_original == null) throw new InvalidOperationException("Ascension has no map settings.");
            settings = UnityEngine.Object.Instantiate(_original);
            settings.name = "Temporary capture map settings";
            settings.hideFlags = HideFlags.HideAndDontSave;
            if (shortRoute)
            {
                settings.floorCount = 5;
                settings.columnCount = 3;
                settings.pathCount = 4;
                settings.treasureFloor = 1;
                settings.restBeforeBoss = true;
                settings.firstEliteFloor = 3;
                settings.firstRestFloor = 4;
                settings.combatWeight = 0f;
                settings.eliteWeight = 1f;
                settings.restWeight = 0f;
                settings.treasureWeight = 0f;
            }
            SettingsField.SetValue(owner, settings);
            SeedField.SetValue(owner, Seed);
        }

        public void Dispose()
        {
            if (_owner != null)
            {
                SettingsField.SetValue(_owner, _original);
                SeedField.SetValue(_owner, _originalSeed);
            }
            if (settings != null) UnityEngine.Object.DestroyImmediate(settings);
            settings = null;
        }
    }
}
