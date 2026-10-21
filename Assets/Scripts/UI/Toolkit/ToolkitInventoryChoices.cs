using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ToolkitInventoryChoices : IDisposable
{
    readonly ToolkitGameContext _context;
    readonly DropdownField _inventoryTarget;
    readonly DropdownField _inventorySlot;
    readonly Action _selected;
    List<int> _availableSlots = new List<int>();
    readonly List<Entity> _inventoryTargets = new List<Entity>();

    public ToolkitInventoryChoices(ToolkitGameContext context, VisualElement root, Action selected)
    {
        _context = context;
        _selected = selected;
        _inventoryTarget = root.Q<DropdownField>("inventory-target");
        _inventorySlot = root.Q<DropdownField>("inventory-slot");
        _inventoryTarget.RegisterValueChangedCallback(OnTargetChanged);
    }

    public void Refresh()
    {
        if (_context.entities != null)
        {
            RefreshTargets();
        }

        if (_context.selectedEntity != null)
        {
            RefreshSlots();
        }
    }

    public int GetSlot(int fallback)
    {
        int index = _inventorySlot.index;
        return index >= 0 && index < _availableSlots.Count ? _availableSlots[index] : fallback;
    }

    public void Dispose()
    {
        _inventoryTarget.UnregisterValueChangedCallback(OnTargetChanged);
    }

    void OnTargetChanged(ChangeEvent<string> evt)
    {
        int index = _inventoryTarget.index;
        if (index >= 0 && index < _inventoryTargets.Count)
        {
            _context.selectedEntity = _inventoryTargets[index];
            _selected.Invoke();
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
}
