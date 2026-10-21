using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

// The kit icons of a class card: one chip per skill (its icon) and per unit (its icon and name). Hovering a chip
// with a mouse, or tapping it, opens its details in the ToolkitPopover through the view's OnInspectRequested, the
// route a creature in the field takes; the popover closes on a mouse leave or on a tap outside it.
public class ToolkitKitRow : IDisposable
{
    public static readonly string ChipClass = "kit-chip";
    public static readonly string IconClass = "kit-chip__icon";
    public static readonly string LabelClass = "kit-chip__label";

    class Chip
    {
        public VisualElement root;
        public VisualElement icon;
        public Label label;
        public ToolkitKitEntry entry;
    }

    readonly ToolkitGameView _view;
    readonly VisualElement _row;
    readonly List<Chip> _chips = new List<Chip>();

    public ToolkitKitRow(ToolkitGameView view, VisualElement row)
    {
        _view = view;
        _row = row;
        _row.style.display = DisplayStyle.None;
    }

    public int count { get { return _chips.Count; } }

    // The chip elements, for the tests
    public VisualElement ChipAt(int index)
    {
        return _chips[index].root;
    }

    public void Refresh(IReadOnlyList<ToolkitKitEntry> entries)
    {
        List<ToolkitKitEntry> shown = new List<ToolkitKitEntry>();
        if (entries != null)
        {
            foreach (ToolkitKitEntry entry in entries)
            {
                if (entry != null)
                {
                    shown.Add(entry);
                }
            }
        }

        while (_chips.Count > shown.Count)
        {
            RemoveChip(_chips[_chips.Count - 1]);
            _chips.RemoveAt(_chips.Count - 1);
        }

        for (int i = 0; i < shown.Count; i++)
        {
            if (i == _chips.Count)
            {
                _chips.Add(CreateChip());
            }

            Bind(_chips[i], shown[i]);
        }

        _row.style.display = shown.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // Resolved through the view like every other icon: portrait, provider, baked catalog, then procedural
    public void RefreshIcons()
    {
        foreach (Chip chip in _chips)
        {
            SetIcon(chip);
        }
    }

    public void Dispose()
    {
        foreach (Chip chip in _chips)
        {
            RemoveChip(chip);
        }

        _chips.Clear();
    }

    Chip CreateChip()
    {
        Chip chip = new Chip();
        chip.root = new VisualElement();
        chip.root.focusable = false;
        chip.root.userData = chip;
        chip.root.AddToClassList(ChipClass);
        chip.icon = new VisualElement { pickingMode = PickingMode.Ignore };
        chip.icon.AddToClassList(IconClass);
        chip.label = new Label { pickingMode = PickingMode.Ignore };
        chip.label.AddToClassList(LabelClass);
        chip.root.Add(chip.icon);
        chip.root.Add(chip.label);
        chip.root.RegisterCallback<PointerEnterEvent>(OnPointerEnter);
        chip.root.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        chip.root.RegisterCallback<PointerDownEvent>(OnPointerDown);
        chip.root.RegisterCallback<ClickEvent>(OnClick);
        _row.Add(chip.root);
        return chip;
    }

    void RemoveChip(Chip chip)
    {
        chip.root.UnregisterCallback<PointerEnterEvent>(OnPointerEnter);
        chip.root.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
        chip.root.UnregisterCallback<PointerDownEvent>(OnPointerDown);
        chip.root.UnregisterCallback<ClickEvent>(OnClick);
        chip.root.RemoveFromHierarchy();
        chip.entry = null;
    }

    void Bind(Chip chip, ToolkitKitEntry entry)
    {
        chip.entry = entry;
        chip.label.text = entry.showLabel ? entry.title : "";
        chip.label.style.display = entry.showLabel ? DisplayStyle.Flex : DisplayStyle.None;
        SetIcon(chip);
    }

    void SetIcon(Chip chip)
    {
        if (chip.entry == null)
        {
            return;
        }

        chip.icon.style.backgroundImage = new StyleBackground(_view.GetIcon(chip.entry.iconSource, out bool isPortrait));
    }

    void Open(VisualElement element)
    {
        Chip chip = element != null ? element.userData as Chip : null;
        if (chip == null || chip.entry == null)
        {
            return;
        }

        _view.inspectAnchor = element.worldBound;
        _view.OnInspectRequested.Invoke(chip.entry.ToModel());
    }

    void OnPointerEnter(PointerEnterEvent evt)
    {
        Open(evt.currentTarget as VisualElement);
    }

    // A touch keeps the popover open until a tap outside it, a mouse closes it as the pointer leaves
    void OnPointerLeave(PointerLeaveEvent evt)
    {
        if (evt.pointerType == PointerType.touch || evt.pointerType == PointerType.pen)
        {
            return;
        }

        _view.ClosePopover();
    }

    // The chip sits inside the card's button: the press ends here, so a tap on an icon reads details instead of
    // choosing the class
    void OnPointerDown(PointerDownEvent evt)
    {
        evt.StopPropagation();
        Open(evt.currentTarget as VisualElement);
    }

    void OnClick(ClickEvent evt)
    {
        evt.StopPropagation();
    }
}
