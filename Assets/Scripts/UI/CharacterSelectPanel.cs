using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Character select screen of the menu, built from the game characters: one card per character, picked
// with the Select button at its bottom. Hovering a skill icon or a unit shows its details in a tooltip.
public class CharacterSelectPanel : MonoBehaviour
{
    static readonly Color BackgroundColor = new Color(0f, 0f, 0f, 0.85f);
    static readonly Color CardColor = new Color(0.15f, 0.17f, 0.22f, 1f);
    static readonly Color ButtonColor = new Color(0.25f, 0.28f, 0.35f, 1f);
    static readonly Color HeaderColor = new Color(1f, 0.85f, 0.4f, 1f);
    static readonly Color MutedColor = new Color(0.6f, 0.64f, 0.7f, 1f);
    static readonly Color TooltipColor = new Color(0.08f, 0.09f, 0.12f, 0.97f);
    const float SkillIconSize = 56f;
    const float TooltipWidth = 420f;
    // Gap between the tooltip and the hovered element
    const float TooltipOffset = 8f;

    GameObject _tooltip;
    TMP_Text _tooltipText;

    public GameObject tooltip => _tooltip;
    public string tooltipText => _tooltipText.text;

    public static CharacterSelectPanel Create(Transform parent, List<CharacterData> characters, Action<CharacterData> onChosen, Action onBack)
    {
        GameObject root = CreateUIObject("CharacterSelectPanel", parent);
        Stretch(root.GetComponent<RectTransform>());
        root.AddComponent<Image>().color = BackgroundColor;
        CharacterSelectPanel panel = root.AddComponent<CharacterSelectPanel>();

        VerticalLayoutGroup rootLayout = root.AddComponent<VerticalLayoutGroup>();
        rootLayout.childAlignment = TextAnchor.MiddleCenter;
        rootLayout.spacing = 30f;
        rootLayout.childControlWidth = false;
        rootLayout.childControlHeight = false;
        rootLayout.childForceExpandWidth = false;
        rootLayout.childForceExpandHeight = false;

        CreateText(root.transform, "Choose your character", 44, HeaderColor, FontStyles.Bold, new Vector2(900f, 60f));

        GameObject cards = CreateUIObject("Cards", root.transform);
        HorizontalLayoutGroup cardsLayout = cards.AddComponent<HorizontalLayoutGroup>();
        cardsLayout.childAlignment = TextAnchor.MiddleCenter;
        cardsLayout.spacing = 30f;
        cardsLayout.childControlWidth = false;
        cardsLayout.childControlHeight = false;
        cardsLayout.childForceExpandWidth = false;
        cardsLayout.childForceExpandHeight = false;
        cards.GetComponent<RectTransform>().sizeDelta = new Vector2(1200f, 600f);

        foreach (CharacterData character in characters)
        {
            if (character != null)
            {
                panel.CreateCard(cards.transform, character, CreatePreview(character, root.transform), () => onChosen(character));
            }
        }

        List<CharacterData> choices = characters.FindAll(character => character != null);
        if (choices.Count > 0)
        {
            CreateButton(root.transform, "Random", new Vector2(200f, 60f), () => onChosen(choices[UnityEngine.Random.Range(0, choices.Count)]));
        }
        // No way back when the panel is the first screen
        if (onBack != null)
        {
            CreateButton(root.transform, "Back", new Vector2(200f, 60f), onBack);
        }
        // Last, so it's drawn over the cards
        panel.CreateTooltip(root.transform);
        return panel;
    }

    // The character built with its stats only (attributes, mana, starting items), so the card shows its real
    // stats and skill values. Hidden, outside the layout, and destroyed with the screen.
    public static Character CreatePreview(CharacterData data, Transform parent)
    {
        GameObject go = new GameObject(data.title + " Preview");
        go.transform.SetParent(parent, false);
        go.AddComponent<AttributeManager>();
        go.AddComponent<BuffManager>();
        Character character = go.AddComponent<Character>();
        character.data = data;
        character.InitAttributes();
        character.InitItems();
        character.ApplyBuffs();
        return character;
    }

    void CreateCard(Transform parent, CharacterData character, Character preview, Action onSelect)
    {
        GameObject card = CreateUIObject(character.title, parent);
        card.GetComponent<RectTransform>().sizeDelta = new Vector2(360f, 600f);
        card.AddComponent<Image>().color = CardColor;

        VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.spacing = 12f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateText(card.transform, character.title, 34, HeaderColor, FontStyles.Bold);
        CreateText(card.transform, CharacterCardText.GetDescription(character), 20, Color.white, FontStyles.Normal);
        CreateText(card.transform, "Stats", 22, MutedColor, FontStyles.Bold);
        CreateText(card.transform, CharacterCardText.GetStats(preview), 20, Color.white, FontStyles.Normal);
        CreateText(card.transform, "Skills", 22, MutedColor, FontStyles.Bold);
        CreateSkillIcons(card.transform, character, preview);
        // Most characters start without any item
        string items = CharacterCardText.GetItems(character);
        if (!string.IsNullOrEmpty(items))
        {
            CreateText(card.transform, "Items", 22, MutedColor, FontStyles.Bold);
            CreateText(card.transform, items, 20, Color.white, FontStyles.Normal);
        }
        CreateText(card.transform, "Units", 22, MutedColor, FontStyles.Bold);
        CreateUnitLabels(card.transform, character);

        // Takes the room left, so the button is always at the bottom of the card
        CreateUIObject("Space", card.transform).AddComponent<LayoutElement>().flexibleHeight = 1f;
        GameObject select = CreateButton(card.transform, "Select", new Vector2(200f, 60f), onSelect);
        select.AddComponent<LayoutElement>().preferredHeight = 60f;
    }

    // One icon per skill, its details shown on hover
    void CreateSkillIcons(Transform parent, CharacterData character, Character preview)
    {
        GameObject row = CreateUIObject("Skills", parent);
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 10f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        row.AddComponent<LayoutElement>().minHeight = SkillIconSize;

        if (character.skills == null)
        {
            return;
        }
        foreach (ACharacterSkillFactory skill in character.skills)
        {
            if (skill == null)
            {
                continue;
            }
            CharacterSkillData data = skill.Create().GetData();
            GameObject icon = CreateUIObject(data.name, row.transform);
            icon.GetComponent<RectTransform>().sizeDelta = new Vector2(SkillIconSize, SkillIconSize);
            Image image = icon.AddComponent<Image>();
            image.sprite = data.icon;
            image.preserveAspect = true;
            if (data.icon == null)
            {
                image.color = ButtonColor;
            }
            AddTooltip(icon, CharacterCardText.GetSkillTooltip(skill, preview));
        }
    }

    // One label per unit the character can recruit, its details shown on hover
    void CreateUnitLabels(Transform parent, CharacterData character)
    {
        GameObject grid = CreateUIObject("Units", parent);
        GridLayoutGroup layout = grid.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(150f, 36f);
        layout.spacing = new Vector2(10f, 10f);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 2;

        if (character.entities == null)
        {
            return;
        }
        foreach (EntityData entity in character.entities)
        {
            if (entity == null)
            {
                continue;
            }
            GameObject label = CreateUIObject(entity.title, grid.transform);
            label.AddComponent<Image>().color = ButtonColor;
            TMP_Text text = CreateText(label.transform, entity.title, 18, Color.white, FontStyles.Normal);
            Stretch(text.rectTransform);
            AddTooltip(label, CharacterCardText.GetUnitTooltip(entity));
        }
    }

    void AddTooltip(GameObject target, string text)
    {
        HoverTooltipTrigger trigger = target.AddComponent<HoverTooltipTrigger>();
        trigger.onEnter = rectTransform => ShowTooltip(text, rectTransform);
        trigger.onExit = HideTooltip;
    }

    void CreateTooltip(Transform parent)
    {
        _tooltip = CreateUIObject("Tooltip", parent);
        // Placed by hand next to the hovered element, not by the layout of the screen
        _tooltip.AddComponent<LayoutElement>().ignoreLayout = true;
        Image background = _tooltip.AddComponent<Image>();
        background.color = TooltipColor;
        // Never under the pointer, or it would make the hovered element lose the hover
        background.raycastTarget = false;

        VerticalLayoutGroup layout = _tooltip.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 12, 12);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        ContentSizeFitter fitter = _tooltip.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        _tooltip.GetComponent<RectTransform>().sizeDelta = new Vector2(TooltipWidth, 0f);

        _tooltipText = CreateText(_tooltip.transform, "", 20, Color.white, FontStyles.Normal);
        _tooltipText.alignment = TextAlignmentOptions.TopLeft;
        _tooltip.SetActive(false);
    }

    // Under the element, on the side of the screen with the most room so it stays visible
    public void ShowTooltip(string text, RectTransform target)
    {
        _tooltipText.text = text;
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        RectTransform tooltipTransform = (RectTransform)_tooltip.transform;
        bool isOnTheRight = target.position.x > ((RectTransform)transform).position.x;
        // corners[0] is the bottom left corner, corners[3] the bottom right one
        tooltipTransform.pivot = new Vector2(isOnTheRight ? 1f : 0f, 1f);
        tooltipTransform.position = (isOnTheRight ? corners[3] : corners[0]) + Vector3.down * TooltipOffset * tooltipTransform.lossyScale.y;
        _tooltip.SetActive(true);
    }

    public void HideTooltip()
    {
        // Already gone with the screen
        if (_tooltip != null)
        {
            _tooltip.SetActive(false);
        }
    }

    static GameObject CreateButton(Transform parent, string label, Vector2 size, Action onClick)
    {
        GameObject button = CreateUIObject(label, parent);
        button.GetComponent<RectTransform>().sizeDelta = size;
        button.AddComponent<Image>().color = ButtonColor;
        button.AddComponent<Button>().onClick.AddListener(() => onClick());
        TMP_Text text = CreateText(button.transform, label, 26, Color.white, FontStyles.Bold);
        Stretch(text.rectTransform);
        return button;
    }

    static TMP_Text CreateText(Transform parent, string content, float fontSize, Color color, FontStyles style, Vector2? size = null)
    {
        GameObject go = CreateUIObject("Text", parent);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        if (size.HasValue)
        {
            text.rectTransform.sizeDelta = size.Value;
        }
        return text;
    }

    static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
