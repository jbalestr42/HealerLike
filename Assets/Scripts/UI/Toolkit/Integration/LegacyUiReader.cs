using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

// Presentation reads the existing gameplay/UI state; commands still use the original public actions.
public static class LegacyUiReader
{
    static bool HasSource(UnityEngine.Object source, string member)
    {
        if (source != null)
        {
            return true;
        }

        Debug.LogError($"[LegacyUiReader] Cannot read {member} from a null source");
        return false;
    }

    public static GameManager.GameState GameState(GameManager source)
    {
        return HasSource(source, "GameManager.state") ? source.state : GameManager.GameState.None;
    }

    public static AscensionGameType.State AscensionState(AscensionGameType source)
    {
        return HasSource(source, "AscensionGameType.state") ? source.state : AscensionGameType.State.None;
    }

    public static ViewType CurrentView(UIManager source)
    {
        return HasSource(source, "UIManager.currentView") ? source.currentView : ViewType.None;
    }

    public static GameObject SelectedObject(GameView source)
    {
        if (!HasSource(source, "GameView.selectedPanel") || source.selectedPanel == PanelType.None)
        {
            return null;
        }

        return source.selectedObject;
    }

    public static IReadOnlyList<SelectEntityButton> AvailableEntities(EntityInventory source)
    {
        if (!HasSource(source, "EntityInventory.entityButtons"))
        {
            return Array.Empty<SelectEntityButton>();
        }

        return source.entityButtons;
    }

    public static IReadOnlyList<GameObject> UpgradeChoices(UpgradeView source)
    {
        if (!HasSource(source, "UpgradeView.upgradeButtons"))
        {
            return Array.Empty<GameObject>();
        }

        return source.upgradeButtons;
    }

    // The buttons EventView built for the choices on screen, in his order
    public static IReadOnlyList<UnityEngine.UI.Button> EventChoices(EventView source)
    {
        if (!HasSource(source, "EventView.choiceButtons"))
        {
            return Array.Empty<UnityEngine.UI.Button>();
        }

        return source.choiceButtons;
    }

    // EventView keeps its two texts in private serialized fields and exposes neither. Read through the field
    // until he adds `title` and `description` getters (a two line change on his side), then call those instead.
    public static string EventTitle(EventView source)
    {
        return EventText(source, "_title");
    }

    public static string EventDescription(EventView source)
    {
        return EventText(source, "_description");
    }

    static bool _reportedMissingEventField;

    static string EventText(EventView source, string field)
    {
        if (!HasSource(source, "EventView." + field))
        {
            return string.Empty;
        }

        FieldInfo info = typeof(EventView).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
        if (info == null)
        {
            if (!_reportedMissingEventField)
            {
                _reportedMissingEventField = true;
                Debug.LogError($"[LegacyUiReader] EventView no longer has a field named {field}, the event panel shows no screen text");
            }

            return string.Empty;
        }

        TMP_Text text = info.GetValue(source) as TMP_Text;
        // His Display switches the description off when it is empty, a stale text must not be read
        return text != null && text.gameObject.activeSelf ? text.text : string.Empty;
    }

    public static UseCharacterSkillButton SkillButton(CharacterSkillSlot source)
    {
        return HasSource(source, "CharacterSkillSlot.skillButton") ? source.skillButton : null;
    }

    public static AItem Item(SelectItemUpgradeButton source)
    {
        return HasSource(source, "SelectItemUpgradeButton.item") ? source.item : null;
    }

    public static AItem Item(SelectPlayerItemUpgradeButton source)
    {
        return source != null ? source.item : null;
    }

    public static EntityData Unit(SelectEntityUpgradeButton source)
    {
        return source != null ? source.entity : null;
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

        string cost = presentation.costText;
        string cooldown = presentation.cooldownText;
        return ToolkitPresentation.SkillStatus(
            presentation.hasCost && cost != null, cost,
            presentation.hasCooldown && cooldown != null, cooldown);
    }

    // Non-generic custom items do not need data or a description to remain valid items.
    public static string ItemDescription(AItem item)
    {
        IGameDataSource source = item as IGameDataSource;
        BaseItemData data = source != null ? source.sourceData as BaseItemData : null;
        return data != null && data.description != null ? data.description : string.Empty;
    }
}
