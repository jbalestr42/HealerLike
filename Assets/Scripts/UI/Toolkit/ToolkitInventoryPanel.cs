using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// The equipment cards and the moves between the stash and the allies, which keep the chosen slot
public class ToolkitInventoryPanel : IDisposable
{
    ToolkitGameUI _gameUI;
    ToolkitGameContext _context;
    ToolkitGameView _view;
    ToolkitDetailPanel _detailPanel;
    Button _equipButton;
    Button _inventoryEquipButton;
    ToolkitInventoryChoices _choices;

    public void Init(
        ToolkitGameUI gameUI,
        ToolkitGameContext context,
        ToolkitGameView view,
        ToolkitDetailPanel detailPanel
    )
    {
        Dispose();
        _gameUI = gameUI;
        _context = context;
        _view = view;
        _detailPanel = detailPanel;
        if (detailPanel.actionsHost == null)
        {
            return;
        }

        if (!ToolkitTemplates.Require(view.root, "detail-equip-button", out _equipButton))
        {
            return;
        }

        _equipButton.clicked += TransferSelectedItem;
        _inventoryEquipButton = view.root.Q<Button>("inventory-equip-button");
        if (_inventoryEquipButton != null)
        {
            _inventoryEquipButton.clicked += TransferSelectedItem;
        }

        _choices = new ToolkitInventoryChoices(context, view.root, OnTargetSelected);
    }

    public void Dispose()
    {
        if (_equipButton != null)
        {
            _equipButton.clicked -= TransferSelectedItem;
        }

        if (_inventoryEquipButton != null)
        {
            _inventoryEquipButton.clicked -= TransferSelectedItem;
        }

        if (_choices != null)
        {
            _choices.Dispose();
            _choices = null;
        }

        _equipButton = null;
        _inventoryEquipButton = null;
    }

    public void Refresh()
    {
        _choices.Refresh();
        List<ToolkitCardModel> models = new List<ToolkitCardModel>();
        AddItems(models, _context.stash, "Stash");
        if (_context.selectedEntity != null)
        {
            AddItems(models, _context.selectedEntity.inventoryHandler, _context.selectedEntity.data.title);
        }

        if (_context.player.character != null)
        {
            AddItems(models, _context.player.character.inventoryHandler, "Healer");
        }

        if (models.Count == 0)
        {
            ToolkitCardModel empty = new ToolkitCardModel();
            empty.title = "No equipment yet";
            empty.description = "Win a wave to earn rewards.";
            empty.isEnabled = false;
            models.Add(empty);
        }

        _view.SetCards("inventory-list", models);
    }

    public void RefreshEquipmentAction(bool isPreparing)
    {
        if (_equipButton == null)
        {
            return;
        }

        InventoryHandler stash = _context.legacy != null ? _context.stash : null;
        bool isFromStash = _context.selectedItemOwner == stash;
        Character character = _context.player != null ? _context.player.character : null;
        bool isHealerItem = character != null && _context.selectedItemOwner == character.inventoryHandler;
        bool isValid =
            isPreparing
            && !_context.isPaused
            && _context.selectedItem != null
            && _context.selectedItemOwner != null
            && ToolkitInventoryTransfer.Contains(_context.selectedItemOwner, _context.selectedItem)
            && !isHealerItem
            && (!isFromStash || _context.selectedEntity != null);
        _equipButton.SetEnabled(isValid);
        _equipButton.text = GetEquipText(isFromStash, isHealerItem);
        _detailPanel.EnableTargeting(isPreparing && _context.selectedEntity != null && !_context.isPaused);
        if (_inventoryEquipButton != null)
        {
            _inventoryEquipButton.SetEnabled(isValid);
            _inventoryEquipButton.text = _equipButton.text;
        }
    }

    void AddItems(List<ToolkitCardModel> models, InventoryHandler owner, string location)
    {
        if (owner == null)
        {
            return;
        }

        foreach (InventoryItemData entry in owner.items)
        {
            AItem item = entry.item;
            if (item == null)
            {
                continue;
            }

            ToolkitItemEntry itemEntry = new ToolkitItemEntry();
            itemEntry.item = item;
            itemEntry.owner = owner;
            itemEntry.location = location;
            ToolkitCardModel model = new ToolkitCardModel();
            model.iconSource = item;
            model.title = item.title;
            model.description = LegacyUiReader.ItemDescription(item);
            if (string.IsNullOrEmpty(model.description))
            {
                model.description = $"{location} · Select to manage equipment";
            }

            model.status = entry.inventoryIndex >= 0 ? $"{location} · Slot {entry.inventoryIndex + 1}" : location;
            model.source = itemEntry;
            model.activate = OnItemActivated;
            models.Add(model);
        }
    }

    string GetEquipText(bool isFromStash, bool isHealerItem)
    {
        if (_context.selectedItem == null)
        {
            return "Select equipment in inventory";
        }

        if (isHealerItem)
        {
            return "Healer upgrade equipped";
        }

        if (!isFromStash)
        {
            return "Return to stash";
        }

        if (_context.selectedEntity == null)
        {
            return "Select a deployed ally";
        }

        return $"Equip to {_context.selectedEntity.data.title}";
    }

    void OnItemActivated(ToolkitCardModel model)
    {
        ToolkitItemEntry itemEntry = (ToolkitItemEntry)model.source;
        _context.selectedItem = itemEntry.item;
        _context.selectedItemOwner = itemEntry.owner;
        _view.ShowDetail(model);
        _view.SetText("inventory-detail", $"{itemEntry.item.title} · {itemEntry.location}");
        RefreshEquipmentAction(true);
    }

    void OnTargetSelected()
    {
        _detailPanel.RefreshEntity();
        _gameUI.Refresh();
    }

    void TransferSelectedItem()
    {
        AItem item = _context.selectedItem;
        InventoryHandler owner = _context.selectedItemOwner;
        if (item == null || owner == null || _context.legacy == null || _context.isPaused || !_context.IsPreparing())
        {
            return;
        }

        InventoryHandler stash = _context.stash;
        InventoryHandler destination = stash;
        if (owner == stash)
        {
            destination = _context.selectedEntity != null ? _context.selectedEntity.inventoryHandler : null;
        }

        if (destination == null || !ToolkitInventoryTransfer.Contains(owner, item))
        {
            return;
        }

        int index = ToolkitInventoryTransfer.FirstEmptySlot(destination);
        if (destination != stash)
        {
            index = _choices.GetSlot(index);
        }

        if (!ToolkitInventoryTransfer.Transfer(item, owner, destination, index))
        {
            return;
        }

        LegacyInventoryAdapter.Synchronize(owner);
        LegacyInventoryAdapter.Synchronize(destination);
        _context.selectedItem = null;
        _context.selectedItemOwner = null;
        _gameUI.Refresh();
    }
}
