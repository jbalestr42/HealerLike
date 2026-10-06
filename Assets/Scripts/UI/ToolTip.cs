using UnityEngine;

public class ToolTip : MonoBehaviour
{
    // Gap between the tooltip and the element it's shown under
    const float BelowOffset = 8f;

    [SerializeField] GameObject _toolTip;
    [SerializeField] TMPro.TMP_Text _text;

    RectTransform _panel;
    Vector2 _defaultPosition;
    Vector2 _defaultPivot;

    void Awake()
    {
        _panel = (RectTransform)_toolTip.transform;
        _defaultPosition = _panel.anchoredPosition;
        _defaultPivot = _panel.pivot;
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
    }

    // Under the target, aligned on its left edge
    public void ShowBelow(RectTransform target)
    {
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        _panel.pivot = new Vector2(0f, 1f);
        // corners[0] is the bottom left corner
        _panel.position = corners[0] + Vector3.down * BelowOffset * _panel.lossyScale.y;
        _toolTip.SetActive(true);
    }

    public void SetText(string text)
    {
        _text.text = text;
    }
}
