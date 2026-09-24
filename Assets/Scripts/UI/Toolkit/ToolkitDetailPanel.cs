using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// The inspection panel: the hovered card, or the selected ally with its stats and target priority
public class ToolkitDetailPanel
{
    ToolkitGameContext _context;
    ToolkitGameView _view;
    DropdownField _targeting;
    bool _hadSelectedEntity = false;
    GameObject _lastWorldSelection;

    VisualElement _actionsHost;
    public VisualElement actionsHost { get { return _actionsHost; } }

    public void Init(ToolkitGameContext context, ToolkitGameView view)
    {
        _context = context;
        _view = view;
        _actionsHost = view.root.Q("detail-actions");
        if (_actionsHost == null)
        {
            VisualElement description = view.root.Q("detail-description");
            if (description != null)
            {
                _actionsHost = description.parent;
            }
        }

        if (_actionsHost == null)
        {
            return;
        }

        List<string> targetTypes = new List<string>(Enum.GetNames(typeof(TargetBehaviourType)));
        _targeting = new DropdownField("Target priority", targetTypes, 0);
        _targeting.AddToClassList("detail-targeting");
        _targeting.RegisterValueChangedCallback(OnTargetingChanged);
        _actionsHost.Add(_targeting);
    }

    public void Refresh()
    {
        GameObject worldSelection = LegacyUiReader.SelectedObject(_context.legacy);
        if (worldSelection != _lastWorldSelection)
        {
            _lastWorldSelection = worldSelection;
            _context.selectedEntity = _lastWorldSelection != null ? _lastWorldSelection.GetComponent<Entity>() : null;
            RefreshEntity();
        }

        if (_context.selectedEntity == null)
        {
            if (_hadSelectedEntity && !_context.isInspecting)
            {
                ToolkitCardModel placeholder = new ToolkitCardModel();
                placeholder.title = "Choose a creature";
                placeholder.description = "Select a deployed ally to inspect its stats and equipment.";
                _view.ShowDetail(placeholder);
            }

            _hadSelectedEntity = false;
            EnableTargeting(false);
        }
        else
        {
            _hadSelectedEntity = true;
            if (!_context.isInspecting && _context.selectedItem == null)
            {
                RefreshEntity();
            }
        }
    }

    public void RefreshEntity()
    {
        Entity entity = _context.selectedEntity;
        if (entity == null)
        {
            return;
        }

        ToolkitCardModel model = new ToolkitCardModel();
        model.iconSource = entity.data;
        model.title = entity.data.title;
        model.description = GetStats(entity);
        _view.ShowDetail(model);
        if (_targeting != null && entity.targetProvider != null)
        {
            _targeting.SetValueWithoutNotify(entity.targetProvider.targetBehaviourType.ToString());
        }
    }

    public void EnableTargeting(bool isEnabled)
    {
        if (_targeting != null)
        {
            _targeting.SetEnabled(isEnabled);
        }
    }

    public void OnInspect(ToolkitCardModel model)
    {
        _context.isInspecting = true;
        _view.ShowDetail(model);
    }

    public void OnInspectEnded()
    {
        _context.isInspecting = false;
        if (_context.selectedEntity != null && _context.selectedItem == null)
        {
            RefreshEntity();
        }
    }

    static string GetStats(Entity entity)
    {
        string stats = entity.data.description;
        if (entity.health != null)
        {
            stats += $"\nHealth: {ToolkitPresentation.Resource(entity.health.Value, entity.health.Max)}";
        }

        if (entity.attributeManager != null)
        {
            foreach (AttributeType type in Enum.GetValues(typeof(AttributeType)))
            {
                if (entity.attributeManager.Has(type))
                {
                    stats += $"\n{type}: {entity.attributeManager.Get(type).Value:0.##}";
                }
            }
        }

        return stats;
    }

    void OnTargetingChanged(ChangeEvent<string> evt)
    {
        Entity entity = _context.selectedEntity;
        TargetBehaviourType target;
        if (entity != null && entity.targetProvider != null && Enum.TryParse(evt.newValue, out target))
        {
            entity.targetProvider.targetBehaviourType = target;
        }
    }
}
