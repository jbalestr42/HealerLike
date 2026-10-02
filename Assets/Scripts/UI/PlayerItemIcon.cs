using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Icon of an item of the player, its name and description shown in the tooltip on hover. A cursed item
// stands out with a colored outline
public class PlayerItemIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Image _icon;
    [SerializeField] Color _cursedOutlineColor = new Color(0.69f, 0.49f, 1f, 1f);
    [SerializeField] Vector2 _cursedOutlineDistance = new Vector2(3f, -3f);

    AItem _item;
    public AItem item => _item;

    bool _isToolTipShown;

    public void Init(AItem item)
    {
        _item = item;
        _icon.sprite = item.icon;

        // Not QuickOutline's 3D Outline
        UnityEngine.UI.Outline outline = _icon.GetComponent<UnityEngine.UI.Outline>();
        if (CursedTag.IsCursed(item))
        {
            if (outline == null)
            {
                outline = _icon.gameObject.AddComponent<UnityEngine.UI.Outline>();
            }
            outline.effectColor = _cursedOutlineColor;
            outline.effectDistance = _cursedOutlineDistance;
            outline.enabled = true;
        }
        else if (outline != null)
        {
            outline.enabled = false;
        }
    }

    public static string GetToolTipText(AItem item)
    {
        return string.IsNullOrEmpty(item.description) ? $"<b>{item.title}</b>" : $"<b>{item.title}</b>\n{item.description}";
    }

    void ShowToolTip(bool show)
    {
        _isToolTipShown = show;
        ToolTip toolTip = UIManager.instance.GetView<GameView>(ViewType.Game).toolTip;
        if (show)
        {
            toolTip.SetText(GetToolTipText(_item));
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
