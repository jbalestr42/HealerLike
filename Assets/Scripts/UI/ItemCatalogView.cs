using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Editor tool, opened by the Items button of the HUD (see AscensionGameType): every item of the game in two
// inventories, copies of the inventory of the player, in a panel styled like the entity panel (EntityInfoPanel), on
// the right next to it so the items can be dragged onto the selected unit. The items of the units are dragged onto
// them, or clicked to go to the inventory of the player; the items of the player are clicked to give them to the
// character
public class ItemCatalogView : MonoBehaviour
{
    const int ColumnCount = 7;
    const float Width = 440f;
    const float MaxHeight = 760f;
    const float Padding = 12f;
    const float FontSize = 16f;
    const float ButtonHeight = 28f;
    const float HeaderHeight = 56f;
    static readonly Vector2 CellSize = new Vector2(52f, 52f);
    // On the left of the entity panel (460 wide, 130 under the top, 10 of margin)
    static readonly Vector2 Position = new Vector2(-470f, -140f);
    static readonly Color BackgroundColor = new Color(0.05f, 0.06f, 0.08f, 0.85f);

    ItemCatalog _unitItems;
    ItemCatalog _playerItems;
    public ItemCatalog unitItems => _unitItems;
    public ItemCatalog playerItems => _playerItems;

    UnityAction<AItem> _giveUnitItem;
    UnityAction<AItem> _givePlayerItem;
    // Where the inventories go, the scrolled body once built
    RectTransform _content;

    // In the canvas of the inventory of the player, a copy of it for each catalog; the button prefab for Close
    public static ItemCatalogView Create(SlotInventory playerInventory, SandboxButton buttonPrefab, IEnumerable<AItemFactory> itemFactories, UnityAction<AItem> giveUnitItem, UnityAction<AItem> givePlayerItem)
    {
        GameObject go = new GameObject("ItemCatalog (Editor)", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(playerInventory.transform.parent, false);
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.one;
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one;
        rect.anchoredPosition = Position;
        rect.sizeDelta = new Vector2(Width, MaxHeight);
        go.GetComponent<Image>().color = BackgroundColor;

        ItemCatalogView view = go.AddComponent<ItemCatalogView>();
        view.Build(buttonPrefab);
        view.Init(playerInventory, itemFactories, giveUnitItem, givePlayerItem);
        return view;
    }

    public void Init(SlotInventory playerInventory, IEnumerable<AItemFactory> itemFactories, UnityAction<AItem> giveUnitItem, UnityAction<AItem> givePlayerItem)
    {
        _giveUnitItem = giveUnitItem;
        _givePlayerItem = givePlayerItem;

        List<AItemFactory> unitItemFactories = new List<AItemFactory>();
        List<AItemFactory> playerItemFactories = new List<AItemFactory>();
        foreach (AItemFactory itemFactory in itemFactories)
        {
            if (itemFactory == null)
            {
                continue;
            }
            if (SandboxPanel.IsPlayerItem(itemFactory))
            {
                playerItemFactories.Add(itemFactory);
            }
            else
            {
                unitItemFactories.Add(itemFactory);
            }
        }

        _unitItems = new ItemCatalog(unitItemFactories);
        _unitItems.inventoryHandler.OnItemClicked.AddListener(OnUnitItemClicked);
        // Never on a unit: given to the character by a click
        _playerItems = new ItemCatalog(playerItemFactories, isLocked: true);
        _playerItems.inventoryHandler.OnItemClicked.AddListener(OnPlayerItemClicked);

        if (playerInventory != null)
        {
            CreateInventory(playerInventory, _unitItems, "Unit Items", "drag onto a unit, click: to the inventory");
            CreateInventory(playerInventory, _playerItems, "Player Items", "click: to the character");
        }
    }

    public void Show(bool show)
    {
        gameObject.SetActive(show);
    }

    // Like the entity panel: the title and Close on top, the body scrolled under
    void Build(SandboxButton buttonPrefab)
    {
        RectTransform rect = (RectTransform)transform;
        TMP_Text title = CreateText("Title", rect);
        title.text = $"<size={FontSize + 6f}><b>Items</b></size>\n<color={EntityInfoFormatter.MutedColor}>Editor only</color>";
        SetTopStretch(title.rectTransform, Padding, Padding);
        title.rectTransform.sizeDelta = new Vector2(title.rectTransform.sizeDelta.x, HeaderHeight - Padding);

        if (buttonPrefab != null)
        {
            SandboxButton closeButton = Instantiate(buttonPrefab, rect);
            closeButton.Init("Close", () => Show(false));
            RectTransform closeRect = (RectTransform)closeButton.transform;
            closeRect.anchorMin = Vector2.one;
            closeRect.anchorMax = Vector2.one;
            closeRect.pivot = Vector2.one;
            closeRect.anchoredPosition = new Vector2(-Padding, -Padding);
            closeRect.sizeDelta = new Vector2(120f, ButtonHeight);
        }

        // Scrollable body, a transparent image catches the mouse wheel
        RectTransform viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect)).GetComponent<RectTransform>();
        viewport.SetParent(rect, false);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(Padding, Padding);
        viewport.offsetMax = new Vector2(-Padding, -(Padding + HeaderHeight));
        viewport.GetComponent<Image>().color = Color.clear;

        _content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        _content.SetParent(viewport, false);
        SetTopStretch(_content, 0f, 0f);
        VerticalLayoutGroup layout = _content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        _content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = viewport.GetComponent<ScrollRect>();
        scrollRect.viewport = viewport;
        scrollRect.content = _content;
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30f;
    }

    // A section: its header, then a copy of the inventory of the player showing the catalog
    void CreateInventory(SlotInventory playerInventory, ItemCatalog catalog, string title, string hint)
    {
        Transform parent = _content != null ? _content : transform;
        TMP_Text header = CreateText(title + " Title", parent);
        header.text = $"<color={EntityInfoFormatter.HeaderColor}><b>{title}</b></color>\n<color={EntityInfoFormatter.MutedColor}>{hint}</color>";

        SlotInventory inventory = Instantiate(playerInventory, parent);
        inventory.name = title;
        inventory.gameObject.SetActive(true);
        // The copy comes with the slots and items of the player
        for (int i = inventory.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(inventory.transform.GetChild(i).gameObject);
        }
        // Sized by the layout of the panel
        ContentSizeFitter fitter = inventory.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            DestroyImmediate(fitter);
        }
        // The panel is the background
        Image background = inventory.GetComponent<Image>();
        if (background != null)
        {
            background.color = Color.clear;
        }
        GridLayoutGroup grid = inventory.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = ColumnCount;
            grid.cellSize = CellSize;
            grid.padding = new RectOffset();
        }
        inventory.inventoryHandler = catalog.inventoryHandler;
        inventory.RefreshInventory();
        FitSlots(inventory.transform, CellSize);
    }

    // Each slot lays its item out with a grid of its own, sized for the inventory of the player: the item must take
    // the smaller cell of the catalog, or it overlaps the next ones
    public static void FitSlots(Transform inventory, Vector2 cellSize)
    {
        foreach (Transform slot in inventory)
        {
            GridLayoutGroup slotGrid = slot.GetComponent<GridLayoutGroup>();
            if (slotGrid != null)
            {
                slotGrid.cellSize = cellSize;
            }
        }
    }

    static TMP_Text CreateText(string name, Transform parent)
    {
        TextMeshProUGUI text = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        text.transform.SetParent(parent, false);
        text.fontSize = FontSize;
        text.color = Color.white;
        text.richText = true;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    // Anchored on the top edge, stretched between the side paddings
    static void SetTopStretch(RectTransform rect, float sidePadding, float top)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(sidePadding, rect.offsetMin.y);
        rect.offsetMax = new Vector2(-sidePadding, rect.offsetMax.y);
        rect.anchoredPosition = new Vector2(0f, -top);
    }

    void OnUnitItemClicked(AItem item)
    {
        _giveUnitItem?.Invoke(_unitItems.Copy(item));
    }

    void OnPlayerItemClicked(AItem item)
    {
        _givePlayerItem?.Invoke(_playerItems.Copy(item));
    }
}
