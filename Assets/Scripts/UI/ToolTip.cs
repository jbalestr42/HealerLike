using UnityEngine;

public class ToolTip : MonoBehaviour
{
    // Gap between the tooltip and the element it's shown under
    const float BelowOffset = 8f;

    [SerializeField] GameObject _toolTip;
    [SerializeField] TMPro.TMP_Text _text;
    // Between the text and the edges of the panel, the text rect being the panel shrunk by it on each side
    [SerializeField] float _padding = 12f;

    RectTransform _panel;
    Vector2 _defaultPosition;
    Vector2 _defaultPivot;
    // The width set in the prefab: the panel fits its text up to it, a longer text wraps
    float _maxWidth;

    void Awake()
    {
        _panel = (RectTransform)_toolTip.transform;
        _defaultPosition = _panel.anchoredPosition;
        _defaultPivot = _panel.pivot;
        _maxWidth = _panel.sizeDelta.x;
    }

    // At its default place
    public void Show(bool show)
    {
        if (show)
        {
            _panel.pivot = _defaultPivot;
            _panel.anchoredPosition = _defaultPosition;
        }
        _toolTip.SetActive(show);
        if (show)
        {
            FitText();
        }
    }

    // Under the target, aligned on its left edge; above it when there is no room below, aligned on its right edge
    // when it would go past the right of the screen
    public void ShowBelow(RectTransform target)
    {
        _toolTip.SetActive(true);
        FitText();
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        // corners[0] is the bottom left corner, corners[2] the top right one
        Rect targetRect = Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        Vector2 size = Vector2.Scale(_panel.sizeDelta, _panel.lossyScale);
        _panel.pivot = Vector2.zero;
        _panel.position = GetBottomLeft(targetRect, size, new Vector2(Screen.width, Screen.height), BelowOffset * _panel.lossyScale.y);
    }

    // The bottom left corner of a panel of size next to target, all in screen units: below it, else above it, left
    // aligned, else right aligned
    public static Vector2 GetBottomLeft(Rect target, Vector2 size, Vector2 screenSize, float offset)
    {
        float y = target.yMin - offset - size.y;
        if (y < 0f)
        {
            y = target.yMax + offset;
        }
        float x = target.xMin;
        if (x + size.x > screenSize.x)
        {
            x = Mathf.Max(0f, target.xMax - size.x);
        }
        return new Vector2(x, y);
    }

    // The panel resized to the text, now when shown, else once shown
    public void SetText(string text)
    {
        _text.text = text;
        if (_toolTip.activeInHierarchy)
        {
            FitText();
        }
    }

    // A text never shown can't be measured: called once the panel is active
    void FitText()
    {
        float maxTextWidth = _maxWidth - _padding * 2f;
        Vector2 textSize = _text.GetPreferredValues(_text.text, maxTextWidth, 0f);
        _panel.sizeDelta = FitSize(textSize, maxTextWidth, _padding);
    }

    // The size of a panel around a text of textSize, no wider than maxTextWidth plus its padding
    public static Vector2 FitSize(Vector2 textSize, float maxTextWidth, float padding)
    {
        return new Vector2(Mathf.Min(textSize.x, maxTextWidth) + padding * 2f, textSize.y + padding * 2f);
    }
}
