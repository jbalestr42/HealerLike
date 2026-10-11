using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// "Ascension N" in the game HUD, the maluses of the run shown in the tooltip on hover. Hidden at level 0
public class AscensionIndicator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] TMP_Text _label;

    string _tooltipText;
    bool _isToolTipShown;

    public string label => _label.text;
    public string tooltipText => _tooltipText;

    public void Show(AscensionSettings ascension, int level)
    {
        bool isShown = ascension != null && ascension.ClampLevel(level) > 0;
        gameObject.SetActive(isShown);
        if (!isShown)
        {
            return;
        }

        _label.text = $"Ascension {ascension.ClampLevel(level)}";
        _tooltipText = ascension.GetTooltip(level);
    }

    void ShowToolTip(bool show)
    {
        _isToolTipShown = show;
        ToolTip toolTip = UIManager.instance.GetView<GameView>(ViewType.Game).toolTip;
        if (show)
        {
            toolTip.SetText(_tooltipText);
            toolTip.ShowBelow((RectTransform)transform);
        }
        else
        {
            toolTip.Show(false);
        }
    }

    // Hidden while still hovered: the tooltip must not stay behind
    void OnDisable()
    {
        if (_isToolTipShown)
        {
            ShowToolTip(false);
        }
    }

    #region IPointerEnterHandler, IPointerExitHandler

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowToolTip(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ShowToolTip(false);
    }

    #endregion
}
