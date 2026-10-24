using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

// The sandbox tools, opened by the Sandbox button of the toolbar: the character played, the side the units are placed
// on, removing, healing or clearing them, then a button per wave, unit, item and status of the sandbox data. A tool
// used on the board (a unit to place, an item to give, a status to apply) closes the panel so the board can be tapped
public sealed class ToolkitSandboxPanel : IDisposable
{
    public const string PanelName = "sandbox-panel";
    public const string CloseName = "sandbox-close-button";
    public const string ControlsName = "sandbox-controls";
    public const string WavesName = "sandbox-waves";
    public const string UnitsName = "sandbox-units";
    public const string HealerItemsName = "sandbox-healer-items";
    public const string UnitItemsName = "sandbox-unit-items";
    public const string StatusesName = "sandbox-statuses";
    public const string SideName = "sandbox-side-button";

    ToolkitGameContext _context;
    ToolkitGameView _view;
    Action _onChanged;
    bool _isBuilt;
    readonly List<VisualElement> _lists = new List<VisualElement>();

    public bool isBuilt => _isBuilt;

    // onChanged is the host's Refresh, so the other panels follow an open or a close in the same frame
    public void Init(ToolkitGameContext context, ToolkitGameView view, Action onChanged)
    {
        Dispose();
        _context = context;
        _view = view;
        _onChanged = onChanged;
        view.AddClickListener(CloseName, Close);
    }

    public void Dispose()
    {
        if (_view != null)
        {
            _view.RemoveClickListener(CloseName, Close);
        }

        foreach (VisualElement list in _lists)
        {
            list.Clear();
        }

        _lists.Clear();
        _isBuilt = false;
        _context = null;
        _view = null;
        _onChanged = null;
    }

    public void Refresh()
    {
        if (_context == null)
        {
            return;
        }

        SandboxGameType sandbox = _context.sandbox;
        bool isAvailable = sandbox != null && !_context.isMenu && !_context.isPaused;
        if (!isAvailable)
        {
            _context.isSandboxOpen = false;
        }

        if (isAvailable && !_isBuilt && sandbox.isInitialized)
        {
            Build(sandbox);
        }

        _view.Show(PanelName, _context.isSandboxOpen && _isBuilt);
        if (_isBuilt)
        {
            _view.SetButton(SideName, GetSideText(sandbox.placementSide), true);
        }
    }

    public void Close()
    {
        if (_context == null)
        {
            return;
        }

        _context.isSandboxOpen = false;
        Changed();
    }

    public static string GetSideText(Entity.EntityType side)
    {
        return side == Entity.EntityType.Player ? "Side: Ally" : "Side: Enemy";
    }

    public static string GetCharacterText(CharacterData played)
    {
        return "Character: " + (played != null ? played.title : "All skills");
    }

    void Build(SandboxGameType sandbox)
    {
        _isBuilt = true;
        SandboxData data = sandbox.data;
        VisualElement controls = List(ControlsName);
        AddButton(controls, GetCharacterText(sandbox.playedCharacter), sandbox.PlayNextCharacter, false);
        AddButton(controls, GetSideText(sandbox.placementSide), ToggleSide, false).name = SideName;
        AddButton(controls, "Remove units", sandbox.StartRemovingEntities, true);
        AddButton(controls, "Heal all", sandbox.RestoreAll, false);
        AddButton(controls, "Clear all", sandbox.ClearAll, false);

        VisualElement waves = List(WavesName);
        foreach (WavePatternData wave in data.waves)
        {
            if (wave != null)
            {
                AddButton(waves, wave.name, () => sandbox.LoadWave(wave), true);
            }
        }

        VisualElement units = List(UnitsName);
        foreach (EntityData unit in data.entities)
        {
            if (unit != null)
            {
                AddButton(units, unit.title, () => sandbox.SelectEntity(unit), true);
            }
        }

        VisualElement healerItems = List(HealerItemsName);
        VisualElement unitItems = List(UnitItemsName);
        foreach (AItemFactory item in data.items)
        {
            if (item == null)
            {
                continue;
            }

            // A healer item is equipped right away, a unit item is given to the units tapped next
            bool isHealerItem = SandboxPanel.IsPlayerItem(item);
            AddButton(isHealerItem ? healerItems : unitItems, item.title, () => sandbox.GiveItem(item), !isHealerItem);
        }

        VisualElement statuses = List(StatusesName);
        foreach (StatusData status in data.statuses)
        {
            if (status != null)
            {
                AddButton(statuses, status.title, () => sandbox.ApplyStatus(status), true);
            }
        }
    }

    VisualElement List(string name)
    {
        VisualElement list = _view.root.Q(name);
        list.Clear();
        _lists.Add(list);
        return list;
    }

    // closes: used on the board, the panel makes way for it
    Button AddButton(VisualElement list, string text, Action action, bool closes)
    {
        Button button = new Button(() => Use(action, closes)) { text = text };
        button.AddToClassList("button");
        list.Add(button);
        return button;
    }

    void Use(Action action, bool closes)
    {
        action();
        if (closes)
        {
            Close();
        }
        else
        {
            Changed();
        }
    }

    // The side is read when a unit is picked, so the placement under way is cancelled
    void ToggleSide()
    {
        SandboxGameType sandbox = _context.sandbox;
        sandbox.placementSide = sandbox.placementSide == Entity.EntityType.Player ? Entity.EntityType.Computer : Entity.EntityType.Player;
        if (_context.interaction != null)
        {
            _context.interaction.CancelInteraction();
        }
    }

    void Changed()
    {
        if (_onChanged != null)
        {
            _onChanged();
        }
        else
        {
            Refresh();
        }
    }
}
