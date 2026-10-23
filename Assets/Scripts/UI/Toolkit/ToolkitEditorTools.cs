#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

// Editor only, never in a build: the buttons to test a run beside Start battle, Kill wave (skips the fight) and Items
// (every item of the game, a tap on a unit item puts a new copy in the stash, a tap on a healer item gives it to the
// healer). Built in code, the layout of the game knows nothing about them
public sealed class ToolkitEditorTools : IDisposable
{
    public const string KillWaveName = "kill-wave-button";
    public const string ItemsName = "item-catalog-button";
    public const string PanelName = "item-catalog-panel";
    public const string CloseName = "item-catalog-close-button";
    public const string UnitListName = "item-catalog-unit-list";
    public const string HealerListName = "item-catalog-healer-list";

    ToolkitGameContext _context;
    ToolkitGameView _view;
    readonly List<VisualElement> _created = new List<VisualElement>();
    ItemCatalog _unitItems;
    ItemCatalog _healerItems;
    bool _isOpen;

    public bool isOpen => _isOpen;
    public ItemCatalog unitItems => _unitItems;
    public ItemCatalog healerItems => _healerItems;

    public void Init(ToolkitGameContext context, ToolkitGameView view)
    {
        Dispose();
        _context = context;
        _view = view;
        VisualElement actions = view.root.Q("encounter-actions");
        VisualElement hud = view.root.Q("hud-root");
        if (actions == null || hud == null)
        {
            return;
        }

        actions.Add(Track(new Button { name = KillWaveName, text = "Kill wave" }));
        actions.Add(Track(new Button { name = ItemsName, text = "Items" }));
        foreach (string name in new[] { KillWaveName, ItemsName })
        {
            actions.Q(name).AddToClassList("button");
        }

        hud.Add(Track(BuildPanel()));
        view.AddClickListener(KillWaveName, KillWave);
        view.AddClickListener(ItemsName, Toggle);
        view.AddClickListener(CloseName, Close);
    }

    public void Dispose()
    {
        if (_view != null)
        {
            _view.RemoveClickListener(KillWaveName, KillWave);
            _view.RemoveClickListener(ItemsName, Toggle);
            _view.RemoveClickListener(CloseName, Close);
        }

        foreach (VisualElement element in _created)
        {
            element.RemoveFromHierarchy();
        }

        _created.Clear();
        _context = null;
        _view = null;
        _unitItems = null;
        _healerItems = null;
        _isOpen = false;
    }

    // Only in a run, on the board: the field toolbar stays up in battle to reach Kill wave
    public void Refresh()
    {
        if (_view == null)
        {
            return;
        }

        bool isAvailable = !_context.isMenu && _context.ascension != null && _context.IsCurrentView(GameScreen.Game)
            && !_context.isPaused;
        if (!isAvailable)
        {
            _isOpen = false;
        }

        _view.Show(KillWaveName, isAvailable);
        _view.Show(ItemsName, isAvailable);
        if (isAvailable)
        {
            _view.Show("field-toolbar", true);
        }

        _view.Show(PanelName, _isOpen);
        if (_isOpen)
        {
            _view.SetCards(UnitListName, GetModels(_unitItems, "To the stash", GiveUnitItem));
            _view.SetCards(HealerListName, GetModels(_healerItems, "To the healer", GiveHealerItem));
        }
    }

    public void Toggle()
    {
        if (_isOpen)
        {
            Close();
            return;
        }

        if (_context == null || _context.ascension == null || _context.data == null)
        {
            return;
        }

        // Made the first time it is opened, the items of the units apart from the ones of the healer
        if (_unitItems == null)
        {
            List<AItemFactory> unitItems = new List<AItemFactory>();
            List<AItemFactory> healerItems = new List<AItemFactory>();
            foreach (AItemFactory itemFactory in _context.data.items)
            {
                if (itemFactory != null)
                {
                    (SandboxPanel.IsPlayerItem(itemFactory) ? healerItems : unitItems).Add(itemFactory);
                }
            }

            _unitItems = new ItemCatalog(unitItems);
            _healerItems = new ItemCatalog(healerItems);
        }

        _isOpen = true;
        Refresh();
    }

    public void Close()
    {
        _isOpen = false;
        Refresh();
    }

    void KillWave()
    {
        _context?.ascension?.KillEnemyWave();
    }

    // A new copy each time: the catalog keeps its own
    void GiveUnitItem(ToolkitCardModel model)
    {
        _context.ascension.AddUnitItem(_unitItems.Copy((AItem)model.source));
    }

    void GiveHealerItem(ToolkitCardModel model)
    {
        _context.ascension.AddPlayerItem(_healerItems.Copy((AItem)model.source));
    }

    static List<ToolkitCardModel> GetModels(ItemCatalog catalog, string status, Action<ToolkitCardModel> give)
    {
        List<ToolkitCardModel> models = new List<ToolkitCardModel>();
        foreach (InventoryItemData itemData in catalog.inventoryHandler.items)
        {
            AItem item = itemData.item;
            models.Add(new ToolkitCardModel
            {
                key = item.title,
                iconSource = item,
                title = item.title,
                description = ToolkitItemEntry.GetDescription(item),
                status = status,
                isCursed = ToolkitInventoryPanel.IsCursed(item),
                source = item,
                activate = model => give(model)
            });
        }

        return models;
    }

    // A dialog like the inventory's: its header with Close, then a list per kind of item
    VisualElement BuildPanel()
    {
        VisualElement panel = new VisualElement { name = PanelName };
        panel.AddToClassList("modal-layer");
        panel.AddToClassList("is-hidden");
        VisualElement dialog = AddChild(panel, new VisualElement(), "dialog", "dialog--wide", "surface");
        VisualElement header = AddChild(dialog, new VisualElement(), "dialog-header");
        VisualElement titles = AddChild(header, new VisualElement());
        AddChild(titles, new Label("EDITOR ONLY"), "eyebrow");
        AddChild(titles, new Label("Items"), "dialog-title");
        AddChild(header, new Button { name = CloseName, text = "Close" }, "button", "button--quiet");
        ScrollView body = AddChild(dialog, new ScrollView(), "dialog-body");
        body.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        AddSection(body, "Unit items · tap: a copy in the stash", UnitListName);
        AddSection(body, "Healer items · tap: a copy to the healer", HealerListName);
        return panel;
    }

    static void AddSection(VisualElement body, string title, string listName)
    {
        AddChild(body, new Label(title), "eyebrow");
        // Not a scroll view like the other lists, which wrap their cards in their content container
        VisualElement list = AddChild(body, new VisualElement { name = listName }, "collection-list");
        list.style.flexDirection = FlexDirection.Row;
        list.style.flexWrap = Wrap.Wrap;
    }

    static T AddChild<T>(VisualElement parent, T child, params string[] classes) where T : VisualElement
    {
        foreach (string className in classes)
        {
            child.AddToClassList(className);
        }

        parent.Add(child);
        return child;
    }

    VisualElement Track(VisualElement element)
    {
        _created.Add(element);
        return element;
    }
}
#endif
