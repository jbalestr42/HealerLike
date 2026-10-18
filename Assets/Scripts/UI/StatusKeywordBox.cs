using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The box explaining the statuses a text cites (see StatusKeywords), shown next to that text: a tooltip, a panel, a
// hovered choice. Built in code, hidden while the text cites none
public class StatusKeywordBox : MonoBehaviour
{
    const float Width = 320f;
    const int Padding = 12;
    const float FontSize = 18f;
    // Between the box and the text it explains
    const float Gap = 8f;
    static readonly Color BackgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.95f);

    RectTransform _panel;
    TMP_Text _text;

    public string text => _text.text;
    public bool isShown => gameObject.activeSelf;

    public static StatusKeywordBox Create(Transform parent)
    {
        GameObject go = new GameObject("StatusKeywordBox", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        // Placed by hand next to the text, not by the layout of its parent
        go.AddComponent<LayoutElement>().ignoreLayout = true;
        Image background = go.AddComponent<Image>();
        background.color = BackgroundColor;
        // Never under the pointer, or it would make the hovered element lose the hover
        background.raycastTarget = false;

        VerticalLayoutGroup layout = go.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(Padding, Padding, Padding, Padding);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        StatusKeywordBox box = go.AddComponent<StatusKeywordBox>();
        box._panel = (RectTransform)go.transform;
        box._panel.sizeDelta = new Vector2(Width, 0f);

        GameObject textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        TextMeshProUGUI text = textGo.AddComponent<TextMeshProUGUI>();
        text.fontSize = FontSize;
        text.color = Color.white;
        text.richText = true;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.raycastTarget = false;
        box._text = text;

        go.SetActive(false);
        return box;
    }

    // The box of the canvas of this element, created there the first time
    public static StatusKeywordBox Get(Transform element)
    {
        Canvas canvas = element.GetComponentInParent<Canvas>();
        Transform root = canvas != null ? canvas.rootCanvas.transform : element.root;
        foreach (Transform child in root)
        {
            if (child.TryGetComponent(out StatusKeywordBox box))
            {
                return box;
            }
        }
        return Create(root);
    }

    // While the element is hovered (e.g. an event choice), the box explains the statuses its text cites
    public static void AddHover(GameObject element, Func<string> getText)
    {
        HoverTooltipTrigger trigger = element.AddComponent<HoverTooltipTrigger>();
        StatusKeywordBox shownBox = null;
        trigger.onEnter = rect =>
        {
            StatusKeywordBox box = Get(element.transform);
            shownBox = box.Show(getText(), rect) ? box : null;
        };
        trigger.onExit = () =>
        {
            // Gone with its screen
            if (shownBox != null)
            {
                shownBox.Hide();
            }
            shownBox = null;
        };
    }

    // Next to the element when the formatted text cites a status, hidden otherwise. Whether it is shown
    public bool Show(string formattedText, RectTransform element)
    {
        string explanation = StatusKeywords.GetExplanation(formattedText);
        if (string.IsNullOrEmpty(explanation))
        {
            Hide();
            return false;
        }

        _text.text = explanation;
        gameObject.SetActive(true);
        // Drawn over the other elements of its parent
        transform.SetAsLastSibling();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_panel);

        Vector3[] corners = new Vector3[4];
        element.GetWorldCorners(corners);
        // corners[0] is the bottom left corner, corners[2] the top right one
        Rect elementRect = Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        Vector2 size = Vector2.Scale(_panel.sizeDelta, _panel.lossyScale);
        _panel.pivot = Vector2.zero;
        _panel.position = GetBottomLeft(elementRect, size, new Vector2(Screen.width, Screen.height), Gap * _panel.lossyScale.x);
        return true;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // The bottom left corner of a box of size next to the element, all in screen units: on its right, else on its left,
    // its top aligned on the top of the element, moved up or down to stay on the screen
    public static Vector2 GetBottomLeft(Rect element, Vector2 size, Vector2 screenSize, float gap)
    {
        float x = element.xMax + gap;
        if (x + size.x > screenSize.x)
        {
            x = Mathf.Max(0f, element.xMin - gap - size.x);
        }
        float y = Mathf.Clamp(element.yMax - size.y, 0f, Mathf.Max(0f, screenSize.y - size.y));
        return new Vector2(x, y);
    }
}
