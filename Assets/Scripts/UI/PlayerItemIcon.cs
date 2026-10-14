using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Icon of an item of the player, its name and description shown in the tooltip on hover. An item with a
// styled tag (e.g. cursed, ascension) stands out with the outline and the label of its ItemStyle. An item counting
// toward an effect (e.g. the Rosary, every fifth spell free) shows its count
public class PlayerItemIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    static readonly Vector2 OutlineDistance = new Vector2(3f, -3f);

    [SerializeField] Image _icon;
    [SerializeField] TMPro.TMP_Text _counter;

    AItem _item;
    public AItem item => _item;

    BuffManager _holderBuffManager;
    ICounterBuff _counterBuff;
    public ICounterBuff counterBuff => _counterBuff;

    bool _isToolTipShown;

    // The holder's buff manager, to find the count among the buffs the item gives
    public void Init(AItem item, BuffManager holderBuffManager = null)
    {
        _item = item;
        _icon.sprite = item.icon;
        ApplyOutline(_icon, GetStyle(item));

        _holderBuffManager = holderBuffManager;
        if (_holderBuffManager != null)
        {
            // The buffs of the item are applied on the next update of the buff manager, and may be removed
            _holderBuffManager.OnBuffAdded.AddListener(OnBuffAdded);
            _holderBuffManager.OnBuffRemoved.AddListener(OnBuffRemoved);
        }
        BindCounter();
    }

    void OnBuffAdded(BuffManager.BuffData buffData)
    {
        // Only a counter can be it: a type check for the other buffs, the icons of the items without counter included
        if (_counterBuff == null && buffData.first is ICounterBuff)
        {
            BindCounter();
        }
    }

    void OnBuffRemoved(BuffManager.BuffData buffData)
    {
        // Only a counter followed can be taken away
        if (_counterBuff != null)
        {
            BindCounter();
        }
    }

    void BindCounter()
    {
        ICounterBuff counterBuff = ItemCounter.Find(_item, _holderBuffManager);
        if (counterBuff != _counterBuff)
        {
            if (_counterBuff != null)
            {
                _counterBuff.OnCounterChanged.RemoveListener(RefreshCounter);
            }
            _counterBuff = counterBuff;
            if (_counterBuff != null)
            {
                _counterBuff.OnCounterChanged.AddListener(RefreshCounter);
            }
        }
        RefreshCounter();
    }

    void RefreshCounter()
    {
        if (_counter == null)
        {
            return;
        }
        _counter.gameObject.SetActive(_counterBuff != null);
        if (_counterBuff != null)
        {
            _counter.text = ItemCounter.GetText(_counterBuff);
        }
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

    void OnDestroy()
    {
        if (_holderBuffManager != null)
        {
            _holderBuffManager.OnBuffAdded.RemoveListener(OnBuffAdded);
            _holderBuffManager.OnBuffRemoved.RemoveListener(OnBuffRemoved);
        }
        if (_counterBuff != null)
        {
            _counterBuff.OnCounterChanged.RemoveListener(RefreshCounter);
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
