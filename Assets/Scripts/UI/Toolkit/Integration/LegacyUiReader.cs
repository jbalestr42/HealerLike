using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace HealerLike.UI.Toolkit.Integration
{
    /// <summary>
    /// Read-only compatibility boundary for legacy state without a public accessor.
    /// All field bindings are cached and checked for name and type. Gameplay commands
    /// continue through the original public methods and button events.
    /// </summary>
    public static class LegacyUiReader
    {
        static readonly FieldInfo _gameState = RequireField(typeof(GameManager), "_state", typeof(GameManager.GameState));
        static readonly FieldInfo _ascensionState = RequireField(typeof(AscensionGameType), "_state", typeof(AscensionGameType.State));
        static readonly FieldInfo _currentView = RequireField(typeof(UIManager), "_currentView", typeof(ViewType));
        static readonly FieldInfo _selectedPanel = RequireField(typeof(GameView), "_selectedPanel", typeof(PanelType));
        static readonly FieldInfo _selectedObject = RequireField(typeof(GameView), "_selectedObject", typeof(GameObject));
        static readonly FieldInfo _entities = RequireField(typeof(EntityInventory), "_entityButtons", typeof(List<SelectEntityButton>));
        static readonly FieldInfo _skillButton = RequireField(typeof(CharacterSkillSlot), "_skillButton", typeof(UseCharacterSkillButton));
        static readonly FieldInfo _costText = RequireField(typeof(UseCharacterSkillButton), "_costText", typeof(Text));
        static readonly FieldInfo _cooldownText = RequireField(typeof(UseCharacterSkillButton), "_cooldownText", typeof(Text));
        static readonly FieldInfo _upgradeChoices = RequireField(typeof(UpgradeView), "_upgradeButtons", typeof(List<GameObject>));
        static readonly FieldInfo _waveChoices = RequireField(typeof(WaveView), "_waveButtons", typeof(List<SelectWaveButton>));
        static readonly FieldInfo _entityItem = RequireField(typeof(SelectItemUpgradeButton), "_item", typeof(AItem));
        static readonly FieldInfo _playerItem = RequireField(typeof(SelectPlayerItemUpgradeButton), "_item", typeof(AItem));
        static readonly FieldInfo _wave = RequireField(typeof(SelectWaveButton), "_wave", typeof(WavePatternData));
        static readonly Dictionary<Type, FieldInfo> _itemData = new Dictionary<Type, FieldInfo>();

        // Accessing any field forces initialization of every fixed binding, including on IL2CPP.
        public static void ValidateContract()
        {
            if (_gameState == null)
            {
                throw new InvalidOperationException("Toolkit legacy bindings were not initialized.");
            }
        }

        static FieldInfo RequireField(Type owner, string name, Type valueType)
        {
            var field = owner.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null || field.FieldType != valueType)
            {
                throw new InvalidOperationException($"Toolkit compatibility binding requires {owner.FullName}.{name} of type {valueType.FullName}. The legacy contract changed; update LegacyUiReader and its link.xml without modifying legacy code.");
            }
            return field;
        }

        static T Read<T>(FieldInfo field, object source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source), $"Cannot read {field.DeclaringType.FullName}.{field.Name} from a null source.");
            }
            return (T)field.GetValue(source);
        }

        public static GameManager.GameState GameState(GameManager source) => Read<GameManager.GameState>(_gameState, source);
        public static AscensionGameType.State AscensionState(AscensionGameType source) => Read<AscensionGameType.State>(_ascensionState, source);
        public static ViewType CurrentView(UIManager source) => Read<ViewType>(_currentView, source);

        public static GameObject SelectedObject(GameView source)
        {
            return Read<PanelType>(_selectedPanel, source) == PanelType.None ? null : Read<GameObject>(_selectedObject, source);
        }

        public static IReadOnlyList<SelectEntityButton> AvailableEntities(EntityInventory source) => Read<List<SelectEntityButton>>(_entities, source).AsReadOnly();
        public static IReadOnlyList<GameObject> UpgradeChoices(UpgradeView source) => Read<List<GameObject>>(_upgradeChoices, source).AsReadOnly();
        public static IReadOnlyList<SelectWaveButton> WaveChoices(WaveView source) => Read<List<SelectWaveButton>>(_waveChoices, source).AsReadOnly();
        public static UseCharacterSkillButton SkillButton(CharacterSkillSlot source) => Read<UseCharacterSkillButton>(_skillButton, source);
        public static AItem Item(SelectItemUpgradeButton source) => Read<AItem>(_entityItem, source);
        public static AItem Item(SelectPlayerItemUpgradeButton source) => source == null ? null : Read<AItem>(_playerItem, source);
        public static WavePatternData Wave(SelectWaveButton source) => Read<WavePatternData>(_wave, source);

        public static bool CanUse(CharacterSkillSlot source)
        {
            var presentation = SkillButton(source);
            return presentation != null && presentation.button != null && presentation.button.interactable;
        }

        public static string SkillStatus(CharacterSkillSlot source)
        {
            var presentation = SkillButton(source);
            if (presentation == null)
            {
                return "Unavailable";
            }
            var cost = Read<Text>(_costText, presentation);
            var cooldown = Read<Text>(_cooldownText, presentation);
            string resource = presentation.hasCost && cost != null ? $"{cost.text} mana" : "Free";
            string timing = presentation.hasCooldown && cooldown != null && !string.IsNullOrEmpty(cooldown.text) ? cooldown.text : "Ready";
            return $"{resource} · {timing}";
        }

        public static string ItemDescription(AItem item)
        {
            if (item == null)
            {
                return string.Empty;
            }
            var type = item.GetType();
            if (!_itemData.TryGetValue(type, out var field))
            {
                // data is the original public AItem<T> contract. Custom non-generic items
                // may have no description; no private members or naming guesses are used.
                for (var current = type; current != null; current = current.BaseType)
                {
                    if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(AItem<>))
                    {
                        field = current.GetField("data", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                        break;
                    }
                }
                _itemData.Add(type, field);
            }
            return field?.GetValue(item) is BaseItemData data ? data.description ?? string.Empty : string.Empty;
        }
    }
}
