using System;
using System.Collections.Generic;
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
