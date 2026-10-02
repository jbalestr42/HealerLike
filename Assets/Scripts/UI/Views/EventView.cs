using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Screen of an event or rest room: a title, a text and one button per choice, side by side
public class EventView : AView
{
    [SerializeField] TMP_Text _title;
    [SerializeField] TMP_Text _description;
    // Lays the choices out (e.g. a HorizontalLayoutGroup)
    [SerializeField] RectTransform _choiceContainer;

    [SerializeField] Vector2 _choiceSize = new Vector2(320f, 160f);
    [SerializeField] float _fontSize = 24f;
    [SerializeField] Color _choiceColor = new Color(0.2f, 0.25f, 0.35f, 1f);
    [SerializeField] Color _unavailableColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);
    // Rounded sprite of the choices, sliced; plain rectangles when not set
    [SerializeField] Sprite _choiceSprite;

    List<Button> _choiceButtons = new List<Button>();
    public IReadOnlyList<Button> choiceButtons => _choiceButtons;

    bool _hasChosen = false;

    public void Display(string title, string description, IReadOnlyList<EventChoice> choices)
    {
        Clear();
        _hasChosen = false;

        if (_title != null)
        {
            _title.text = title;
        }
        if (_description != null)
        {
            _description.text = description;
            _description.gameObject.SetActive(!string.IsNullOrEmpty(description));
        }

        foreach (EventChoice choice in choices)
        {
            CreateChoice(choice);
        }
    }

    // Only the first pick counts: the choice usually closes the view, a second click would apply it twice
    public void Select(EventChoice choice)
    {
        if (_hasChosen || !choice.isAvailable)
        {
            return;
        }

        _hasChosen = true;
        choice.onSelected?.Invoke();
    }

    // "<b>Rest</b>" then the description below it, smaller
    public static string GetChoiceText(EventChoice choice)
    {
        if (string.IsNullOrEmpty(choice.description))
        {
            return $"<b>{choice.label}</b>";
        }
        return $"<b>{choice.label}</b>\n<size=75%>{choice.description}</size>";
    }

    void Clear()
    {
        foreach (Button button in _choiceButtons)
        {
            if (button != null)
            {
                DestroyElement(button.gameObject);
            }
        }
        _choiceButtons.Clear();
    }

    // Also used from the EditMode tests, where Destroy isn't allowed
    static void DestroyElement(GameObject element)
    {
        if (Application.isPlaying)
        {
            Destroy(element);
        }
        else
        {
            DestroyImmediate(element);
        }
    }

    void CreateChoice(EventChoice choice)
    {
        GameObject buttonGo = TMP_DefaultControls.CreateButton(new TMP_DefaultControls.Resources());
        buttonGo.name = $"Choice {choice.label}";
        buttonGo.transform.SetParent(_choiceContainer, false);
        ((RectTransform)buttonGo.transform).sizeDelta = _choiceSize;

        LayoutElement layout = buttonGo.AddComponent<LayoutElement>();
        layout.preferredWidth = _choiceSize.x;
        layout.preferredHeight = _choiceSize.y;

        Image image = buttonGo.GetComponent<Image>();
        image.color = choice.isAvailable ? _choiceColor : _unavailableColor;
        if (_choiceSprite != null)
        {
            image.sprite = _choiceSprite;
            image.type = Image.Type.Sliced;
        }

        TMP_Text text = buttonGo.GetComponentInChildren<TMP_Text>();
        text.text = GetChoiceText(choice);
        text.fontSize = _fontSize;
        text.color = choice.isAvailable ? Color.white : new Color(1f, 1f, 1f, 0.5f);
        text.margin = new Vector4(12f, 8f, 12f, 8f);

        Button button = buttonGo.GetComponent<Button>();
        button.interactable = choice.isAvailable;
        button.onClick.AddListener(() => Select(choice));
        _choiceButtons.Add(button);
    }

    #region AView

    public override void Show()
    {
        GetComponent<CanvasGroup>().alpha = 1f;
    }

    public override void Hide()
    {
        GetComponent<CanvasGroup>().alpha = 0.1f;
    }

    #endregion
}
