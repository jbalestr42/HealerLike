using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Icon of an item of the player, its name and description shown in the tooltip on hover. An item with a
// styled tag (e.g. cursed, ascension) stands out with the outline and the label of its ItemStyle
public class PlayerItemIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    static readonly Vector2 OutlineDistance = new Vector2(3f, -3f);

    [SerializeField] Image _icon;

    AItem _item;
    public AItem item => _item;

    bool _isToolTipShown;

    public void Init(AItem item)
    {
        _item = item;
        _icon.sprite = item.icon;
        ApplyOutline(_icon, GetStyle(item));
    }

    // The style of the item in the ItemStyleSet of the game, null for a regular item
    public static ItemStyle GetStyle(AItem item)
    {
        // No game data out of play mode (e.g. in the tests, which pass the style themselves)
        if (!Application.isPlaying || DataManager.instance == null || DataManager.instance.data == null)
        {
            return null;
        }
        ItemStyleSet styles = DataManager.instance.data.itemStyles;
        return styles != null ? styles.GetStyle(item) : null;
    }

    // Outlines the icon of an item with a style, removes the outline of a regular one (the icon is reused)
    public static void ApplyOutline(Image icon, ItemStyle style)
    {
        // Not QuickOutline's 3D Outline
        UnityEngine.UI.Outline outline = icon.GetComponent<UnityEngine.UI.Outline>();
        if (style != null)
        {
            if (outline == null)
            {
                outline = icon.gameObject.AddComponent<UnityEngine.UI.Outline>();
            }
            outline.effectColor = style.outlineColor;
            outline.effectDistance = OutlineDistance;
            outline.enabled = true;
        }
        else if (outline != null)
        {
            outline.enabled = false;
        }
    }

    // The name, the label of its style under it (e.g. "Ascension"), then the description
    public static string GetToolTipText(AItem item, ItemStyle style)
    {
        string text = $"<b>{item.title}</b>";
        if (style != null && !string.IsNullOrEmpty(style.label))
        {
            text += $"\n<color=#{ColorUtility.ToHtmlStringRGB(style.outlineColor)}>{style.label}</color>";
        }
        return string.IsNullOrEmpty(item.description) ? text : $"{text}\n{item.description}";
    }

    void ShowToolTip(bool show)
    {
        _isToolTipShown = show;
        ToolTip toolTip = UIManager.instance.GetView<GameView>(ViewType.Game).toolTip;
        if (show)
        {
            toolTip.SetText(GetToolTipText(_item, GetStyle(_item)));
            toolTip.ShowBelow((RectTransform)transform);
        }
        else
        {
            toolTip.Show(false);
        }
    }

    void OnDisable()
    {
        // Removed while hovered: the pointer never exits it
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
