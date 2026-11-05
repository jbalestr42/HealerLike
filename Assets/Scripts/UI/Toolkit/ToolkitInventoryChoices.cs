using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ToolkitInventoryChoices : IDisposable
{
    readonly ToolkitGameContext _context;
    readonly DropdownField _inventoryTarget;
    readonly Action _selected;
    readonly List<Entity> _inventoryTargets = new List<Entity>();

    public ToolkitInventoryChoices(ToolkitGameContext context, VisualElement root, Action selected)
    {
        _context = context;
        _selected = selected;
        _inventoryTarget = root.Q<DropdownField>("inventory-target");
        _inventoryTarget.RegisterValueChangedCallback(OnTargetChanged);
    }

    public void Refresh()
    {
        if (_context.entities != null)
        {
            RefreshTargets();
        }
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
}
