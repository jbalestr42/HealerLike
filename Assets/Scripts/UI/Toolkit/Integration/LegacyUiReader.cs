using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

// Read-only access to the legacy UI state that has no public accessor. Every field binding is cached and
// checked for name and type, the gameplay commands still go through the original public methods and buttons
public static class LegacyUiReader
{
    static readonly FieldInfo gameStateField = RequireField(typeof(GameManager), "_state",
        typeof(GameManager.GameState));
    static readonly FieldInfo ascensionStateField = RequireField(typeof(AscensionGameType), "_state",
        typeof(AscensionGameType.State));
    static readonly FieldInfo currentViewField = RequireField(typeof(UIManager), "_currentView", typeof(ViewType));
    static readonly FieldInfo selectedPanelField = RequireField(typeof(GameView), "_selectedPanel", typeof(PanelType));
    static readonly FieldInfo selectedObjectField = RequireField(typeof(GameView), "_selectedObject",
        typeof(GameObject));
    static readonly FieldInfo entityButtonsField = RequireField(typeof(EntityInventory), "_entityButtons",
        typeof(List<SelectEntityButton>));
    static readonly FieldInfo skillButtonField = RequireField(typeof(CharacterSkillSlot), "_skillButton",
        typeof(UseCharacterSkillButton));
    static readonly FieldInfo costTextField = RequireField(typeof(UseCharacterSkillButton), "_costText",
        typeof(Text));
    static readonly FieldInfo cooldownTextField = RequireField(typeof(UseCharacterSkillButton), "_cooldownText",
        typeof(Text));
    static readonly FieldInfo upgradeButtonsField = RequireField(typeof(UpgradeView), "_upgradeButtons",
        typeof(List<GameObject>));
    static readonly FieldInfo waveButtonsField = RequireField(typeof(WaveView), "_waveButtons",
        typeof(List<SelectWaveButton>));
    static readonly FieldInfo entityItemField = RequireField(typeof(SelectItemUpgradeButton), "_item", typeof(AItem));
    static readonly FieldInfo playerItemField = RequireField(typeof(SelectPlayerItemUpgradeButton), "_item",
        typeof(AItem));
    static readonly FieldInfo waveField = RequireField(typeof(SelectWaveButton), "_wave", typeof(WavePatternData));
    static readonly Dictionary<Type, FieldInfo> itemDataFields = new Dictionary<Type, FieldInfo>();

    public static bool IsValid()
    {
        return gameStateField != null && ascensionStateField != null && currentViewField != null
            && selectedPanelField != null && selectedObjectField != null && entityButtonsField != null
            && skillButtonField != null && costTextField != null && cooldownTextField != null
            && upgradeButtonsField != null && waveButtonsField != null && entityItemField != null
            && playerItemField != null && waveField != null;
    }

    static FieldInfo RequireField(Type owner, string name, Type valueType)
    {
        FieldInfo field = owner.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field == null || field.FieldType != valueType)
        {
            Debug.LogError($"[LegacyUiReader] The Toolkit binding requires {owner.FullName}.{name} of type "
                + $"{valueType.FullName}. The legacy contract changed, update LegacyUiReader and its link.xml");
            return null;
        }

        return field;
    }

    static T Read<T>(FieldInfo field, object source)
    {
        if (field == null)
        {
            return default(T);
        }

        if (source == null)
        {
            Debug.LogError($"[LegacyUiReader] Cannot read {field.DeclaringType.FullName}.{field.Name} "
                + "from a null source");
            return default(T);
        }

        return (T)field.GetValue(source);
    }

    public static GameManager.GameState GameState(GameManager source)
    {
        return Read<GameManager.GameState>(gameStateField, source);
    }

    public static AscensionGameType.State AscensionState(AscensionGameType source)
    {
        return Read<AscensionGameType.State>(ascensionStateField, source);
    }

    public static ViewType CurrentView(UIManager source)
    {
        return Read<ViewType>(currentViewField, source);
    }

    public static GameObject SelectedObject(GameView source)
    {
        if (Read<PanelType>(selectedPanelField, source) == PanelType.None)
        {
            return null;
        }

        return Read<GameObject>(selectedObjectField, source);
    }

    public static IReadOnlyList<SelectEntityButton> AvailableEntities(EntityInventory source)
    {
        return Read<List<SelectEntityButton>>(entityButtonsField, source).AsReadOnly();
    }

    public static IReadOnlyList<GameObject> UpgradeChoices(UpgradeView source)
    {
        return Read<List<GameObject>>(upgradeButtonsField, source).AsReadOnly();
    }

    public static IReadOnlyList<SelectWaveButton> WaveChoices(WaveView source)
    {
        return Read<List<SelectWaveButton>>(waveButtonsField, source).AsReadOnly();
    }

    public static UseCharacterSkillButton SkillButton(CharacterSkillSlot source)
    {
        return Read<UseCharacterSkillButton>(skillButtonField, source);
    }

    public static AItem Item(SelectItemUpgradeButton source)
    {
        return Read<AItem>(entityItemField, source);
    }

    public static AItem Item(SelectPlayerItemUpgradeButton source)
    {
        if (source == null)
        {
            return null;
        }

        return Read<AItem>(playerItemField, source);
    }

    public static WavePatternData Wave(SelectWaveButton source)
    {
        return Read<WavePatternData>(waveField, source);
    }

    public static bool CanUse(CharacterSkillSlot source)
    {
        UseCharacterSkillButton presentation = SkillButton(source);
        return presentation != null && presentation.button != null && presentation.button.interactable;
    }

    public static string SkillStatus(CharacterSkillSlot source)
    {
        UseCharacterSkillButton presentation = SkillButton(source);
        if (presentation == null)
        {
            return "Unavailable";
        }

        Text cost = Read<Text>(costTextField, presentation);
        Text cooldown = Read<Text>(cooldownTextField, presentation);
        bool showCost = presentation.hasCost && cost != null;
        bool showCooldown = presentation.hasCooldown && cooldown != null;
        string costText = showCost ? cost.text : null;
        string cooldownText = showCooldown ? cooldown.text : null;
        return ToolkitPresentation.SkillStatus(showCost, costText, showCooldown, cooldownText);
    }

    // data is the public AItem<T> field. A custom non-generic item may have no description,
    // no private member or naming guess is used here
    public static string ItemDescription(AItem item)
    {
        if (item == null)
        {
            return string.Empty;
        }

        Type type = item.GetType();
        FieldInfo field;
        if (!itemDataFields.TryGetValue(type, out field))
        {
            field = FindDataField(type);
            itemDataFields.Add(type, field);
        }

        if (field == null)
        {
            return string.Empty;
        }

        BaseItemData data = field.GetValue(item) as BaseItemData;
        if (data == null || data.description == null)
        {
            return string.Empty;
        }

        return data.description;
    }

    static FieldInfo FindDataField(Type type)
    {
        for (Type current = type; current != null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(AItem<>))
            {
                BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
                return current.GetField("data", flags);
            }
        }

        return null;
    }
}
