using System;
using UnityEngine;
using UnityEngine.EventSystems;

// Calls back when the pointer enters or leaves the UI element, e.g. to show a tooltip about it. Clicks
// still go to the parents (e.g. a card button behind the element).
public class HoverTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Action<RectTransform> onEnter;
    public Action onExit;

    // Hidden while still hovered (e.g. the screen is closed): the tooltip must not stay behind
    void OnDisable()
    {
        onExit?.Invoke();
    }

    #region IPointerEnterHandler, IPointerExitHandler

    public void OnPointerEnter(PointerEventData eventData)
    {
        onEnter?.Invoke((RectTransform)transform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        onExit?.Invoke();
    }

    #endregion
}
