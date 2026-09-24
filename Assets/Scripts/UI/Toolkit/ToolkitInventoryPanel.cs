using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// The equipment cards and the moves between the stash and the allies, which keep the chosen slot
public class ToolkitInventoryPanel
{
    ToolkitGameUI _gameUI;
    ToolkitGameContext _context;
    ToolkitGameView _view;
    ToolkitDetailPanel _detailPanel;
    Button _equipButton;
    Button _inventoryEquipButton;
    DropdownField _inventoryTarget;
    DropdownField _inventorySlot;
    List<int> _availableSlots = new List<int>();
    List<Entity> _inventoryTargets = new List<Entity>();

    public void Init(ToolkitGameUI gameUI, ToolkitGameContext context, ToolkitGameView view,
                     ToolkitDetailPanel detailPanel)
    {
        _gameUI = gameUI;
        _context = context;
        _view = view;
        _detailPanel = detailPanel;
        if (detailPanel.actionsHost == null)
        {
            return;
        }

        _equipButton = new Button(TransferSelectedItem);
        _equipButton.text = "Select equipment";
        _equipButton.AddToClassList("button");
        detailPanel.actionsHost.Add(_equipButton);
        _inventoryEquipButton = view.root.Q<Button>("inventory-equip-button");
        if (_inventoryEquipButton != null)
        {
            _inventoryEquipButton.clicked += TransferSelectedItem;
        }

        _inventoryTarget = view.root.Q<DropdownField>("inventory-target");
        _inventorySlot = view.root.Q<DropdownField>("inventory-slot");
        if (_inventoryTarget != null)
        {
            _inventoryTarget.RegisterValueChangedCallback(OnTargetChanged);
        }
    }

    public void Refresh()
    {
        if (_inventoryTarget != null && _context.entities != null)
        {
            RefreshTargets();
        }

        if (_inventorySlot != null && _context.selectedEntity != null)
        {
            RefreshSlots();
        }

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
        bool isValid = isPreparing && !_context.isPaused && _context.selectedItem != null
            && _context.selectedItemOwner != null
            && ToolkitInventoryTransfer.Contains(_context.selectedItemOwner, _context.selectedItem)
            && !isHealerItem && (!isFromStash || _context.selectedEntity != null);
        _equipButton.SetEnabled(isValid);
        _equipButton.text = GetEquipText(isFromStash, isHealerItem);
        _detailPanel.EnableTargeting(isPreparing && _context.selectedEntity != null && !_context.isPaused);
        if (_inventoryEquipButton != null)
        {
            _inventoryEquipButton.SetEnabled(isValid);
            _inventoryEquipButton.text = _equipButton.text;
        }
    }

    void RefreshTargets()
    {
        _inventoryTargets.Clear();
        foreach (GameObject entityGo in _context.entities.GetEntities(Entity.EntityType.Player))
        {
            if (entityGo != null)
            {
                _inventoryTargets.Add(entityGo.GetComponent<Entity>());
            }
        }

        List<string> choices = new List<string>();
        for (int i = 0; i < _inventoryTargets.Count; i++)
        {
            choices.Add($"{i + 1}. {_inventoryTargets[i].data.title}");
        }

        _inventoryTarget.choices = choices;
        int selected = _inventoryTargets.IndexOf(_context.selectedEntity);
        _inventoryTarget.SetValueWithoutNotify(selected >= 0 ? _inventoryTarget.choices[selected] : "Choose an ally");
    }

    void RefreshSlots()
    {
        List<int> slots = ToolkitInventoryTransfer.EmptySlots(_context.selectedEntity.inventoryHandler);
        List<string> labels = new List<string>();
        foreach (int index in slots)
        {
            labels.Add($"Slot {index + 1} · {3 - Mathf.Min(index, 2)}× effect");
        }

        string previous = _inventorySlot.value;
        _availableSlots = slots;
        _inventorySlot.choices = labels;
        _inventorySlot.SetValueWithoutNotify(labels.Contains(previous) ? previous : labels[0]);
        _inventorySlot.SetEnabled(_context.selectedItemOwner == _context.stash);
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

    void OnTargetChanged(ChangeEvent<string> evt)
    {
        int index = _inventoryTarget.index;
        if (index >= 0 && index < _inventoryTargets.Count)
        {
            _context.selectedEntity = _inventoryTargets[index];
            _detailPanel.RefreshEntity();
            _gameUI.Refresh();
        }
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
        if (destination != stash && _inventorySlot != null && _inventorySlot.index >= 0
            && _inventorySlot.index < _availableSlots.Count)
        {
            index = _availableSlots[_inventorySlot.index];
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
