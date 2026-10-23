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
    public static readonly string OutlinedClass = "kit-chip--outlined";

    class Chip
    {
        public VisualElement root;
        public VisualElement icon;
        public Label label;
        public ToolkitKitEntry entry;
        public ToolkitInfoTrigger trigger;
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
        chip.trigger = new ToolkitInfoTrigger(_view, chip.root, () => chip.entry != null ? chip.entry.ToModel() : null);
        _row.Add(chip.root);
        return chip;
    }

    void RemoveChip(Chip chip)
    {
        chip.trigger.Dispose();
        chip.root.RemoveFromHierarchy();
        chip.entry = null;
    }

    void Bind(Chip chip, ToolkitKitEntry entry)
    {
        chip.entry = entry;
        string label = GetLabel(entry);
        chip.label.text = label;
        chip.label.style.display = label.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        chip.root.EnableInClassList(OutlinedClass, entry.outline.HasValue);
        if (entry.outline.HasValue)
        {
            chip.root.style.borderLeftColor = entry.outline.Value;
            chip.root.style.borderRightColor = entry.outline.Value;
            chip.root.style.borderTopColor = entry.outline.Value;
            chip.root.style.borderBottomColor = entry.outline.Value;
        }
        else
        {
            chip.root.style.borderLeftColor = StyleKeyword.Null;
            chip.root.style.borderRightColor = StyleKeyword.Null;
            chip.root.style.borderTopColor = StyleKeyword.Null;
            chip.root.style.borderBottomColor = StyleKeyword.Null;
        }
        SetIcon(chip);
    }

    // Its badge (e.g. a count), else its name when it keeps one, else nothing
    public static string GetLabel(ToolkitKitEntry entry)
    {
        if (!string.IsNullOrEmpty(entry.badge))
        {
            return entry.badge;
        }

        return entry.showLabel ? entry.title : "";
    }

    void SetIcon(Chip chip)
    {
        if (chip.entry == null)
        {
            return;
        }

        chip.icon.style.backgroundImage = new StyleBackground(_view.GetIcon(chip.entry.iconSource, out bool isPortrait));
    }
}
