using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Character select screen of the menu, built from the game characters: one card per character,
// clicking a card picks it
public class CharacterSelectPanel : MonoBehaviour
{
    static readonly Color BackgroundColor = new Color(0f, 0f, 0f, 0.85f);
    static readonly Color CardColor = new Color(0.15f, 0.17f, 0.22f, 1f);
    static readonly Color ButtonColor = new Color(0.25f, 0.28f, 0.35f, 1f);
    static readonly Color HeaderColor = new Color(1f, 0.85f, 0.4f, 1f);
    static readonly Color MutedColor = new Color(0.6f, 0.64f, 0.7f, 1f);

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
                CreateCard(cards.transform, character, () => onChosen(character));
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
        return panel;
    }

    static void CreateCard(Transform parent, CharacterData character, Action onClick)
    {
        GameObject card = CreateUIObject(character.title, parent);
        card.GetComponent<RectTransform>().sizeDelta = new Vector2(360f, 600f);
        card.AddComponent<Image>().color = CardColor;
        card.AddComponent<Button>().onClick.AddListener(() => onClick());

        VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.spacing = 12f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateText(card.transform, character.title, 34, HeaderColor, FontStyles.Bold);
        CreateText(card.transform, CharacterCardText.GetDescription(character), 20, Color.white, FontStyles.Normal);
        CreateText(card.transform, "Skills", 22, MutedColor, FontStyles.Bold);
        CreateText(card.transform, CharacterCardText.GetSkills(character), 20, Color.white, FontStyles.Normal);
        // Most characters start without any item
        string items = CharacterCardText.GetItems(character);
        if (!string.IsNullOrEmpty(items))
        {
            CreateText(card.transform, "Items", 22, MutedColor, FontStyles.Bold);
            CreateText(card.transform, items, 20, Color.white, FontStyles.Normal);
        }
        CreateText(card.transform, "Units", 22, MutedColor, FontStyles.Bold);
        CreateText(card.transform, CharacterCardText.GetUnits(character), 20, Color.white, FontStyles.Normal);
    }

    static void CreateButton(Transform parent, string label, Vector2 size, Action onClick)
    {
        GameObject button = CreateUIObject(label, parent);
        button.GetComponent<RectTransform>().sizeDelta = size;
        button.AddComponent<Image>().color = ButtonColor;
        button.AddComponent<Button>().onClick.AddListener(() => onClick());
        TMP_Text text = CreateText(button.transform, label, 26, Color.white, FontStyles.Bold);
        Stretch(text.rectTransform);
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
