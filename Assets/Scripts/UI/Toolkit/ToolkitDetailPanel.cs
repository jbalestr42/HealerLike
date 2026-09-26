using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// The inspection panel: the hovered card, or the selected ally with its stats and target priority
public class ToolkitDetailPanel : IDisposable
{
    ToolkitGameContext _context;
    ToolkitGameView _view;
    DropdownField _targeting;
    bool _hadSelectedEntity = false;
    GameObject _lastWorldSelection;
    Entity _inspectedEntity;
    VisualElement _actionsHost;
    public VisualElement actionsHost
    {
        get { return _actionsHost; }
    }

    public void Init(ToolkitGameContext context, ToolkitGameView view)
    {
        Dispose();
        _context = context;
        _view = view;
        _actionsHost = view.root.Q("detail-actions");
        if (!ToolkitTemplates.Require(view.root, "detail-targeting", out _targeting))
        {
            return;
        }

        _targeting.choices = new List<string>(Enum.GetNames(typeof(TargetBehaviourType)));
        _targeting.SetValueWithoutNotify(_targeting.choices[0]);
        _targeting.RegisterValueChangedCallback(OnTargetingChanged);
    }

    public void Dispose()
    {
        if (_targeting != null)
        {
            _targeting.UnregisterValueChangedCallback(OnTargetingChanged);
        }

        _targeting = null;
        _actionsHost = null;
        _lastWorldSelection = null;
        _inspectedEntity = null;
        _hadSelectedEntity = false;
    }

    public void Refresh()
    {
        // A popover owns its inspected subject until dismissed. Legacy selection can change
        // during the same frame, and a spell inspection must never become creature details.
        if (_context.isInspecting)
        {
            if (_inspectedEntity != null && _context.selectedItem == null) RefreshEntity(_inspectedEntity);
            return;
        }
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
        RefreshEntity(_inspectedEntity != null ? _inspectedEntity : _context.selectedEntity);
    }

    void RefreshEntity(Entity entity)
    {
        if (entity == null)
        {
            return;
        }

        ToolkitCardModel model = new ToolkitCardModel();
        model.source = entity;
        model.iconSource = entity;
        model.title = entity.data.title;
        model.description = GetSummary(entity);
        _view.ShowDetail(model);
        _view.SetText("detail-full-stats", GetStats(entity));
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
        _inspectedEntity = model.source as Entity;
        var attributes = _view.root.Q<Foldout>("detail-attributes");
        if (attributes != null) attributes.value = false;
        if (model.source is Entity entity)
        {
            var selectable = entity.GetComponent<SelectableEntity>();
            if (_context.interaction != null && selectable != null)
            {
                _context.interaction.CancelSelection();
                _context.interaction.Select(selectable);
            }
            _context.selectedEntity = entity;
            _context.selectedItem = null;
            RefreshEntity();
        }
        else _view.ShowDetail(model);
    }

    public void OnInspectEnded()
    {
        _context.isInspecting = false;
        _inspectedEntity = null;
        if (_context.selectedEntity != null && _context.selectedItem == null)
        {
            RefreshEntity();
        }
    }

    static string GetSummary(Entity entity)
    {
        var lines = new List<string>();
        if (entity.health != null)
            lines.Add($"Health: {ToolkitPresentation.Resource(entity.health.Value, entity.health.Max)}");
        var combat = new List<string>();
        foreach (AttributeType type in new[] { AttributeType.Damage, AttributeType.AttackRate,
            AttributeType.Range, AttributeType.FlatArmor })
        {
            if (entity.attributeManager != null && entity.attributeManager.Has(type))
            {
                float value = entity.attributeManager.Get(type).Value;
                if (!Mathf.Approximately(value, 0)) combat.Add($"{AttributeLabel(type)}: {value:0.##}");
            }
        }
        for (int i = 0; i < combat.Count; i += 2)
            lines.Add(combat[i] + (i + 1 < combat.Count ? " · " + combat[i + 1] : ""));
        return string.Join("\n", lines);
    }

    static string GetStats(Entity entity)
    {
        var lines = new List<string>();
        if (!string.IsNullOrEmpty(entity.data.description)) lines.Add(entity.data.description);
        if (entity.attributeManager != null)
            foreach (AttributeType type in Enum.GetValues(typeof(AttributeType)))
                if (entity.attributeManager.Has(type))
                    lines.Add($"{AttributeLabel(type)}: {entity.attributeManager.Get(type).Value:0.##}");
        return string.Join("\n", lines);
    }

    public static string AttributeLabel(AttributeType type)
    {
        switch (type)
        {
            case AttributeType.HealthMax: return "Maximum health";
            case AttributeType.AttackRate: return "Attack rate";
            case AttributeType.FlatArmor: return "Armor";
            case AttributeType.PercentArmor: return "Damage reduction";
            case AttributeType.HitArmor: return "Armor per hit";
            case AttributeType.ManaMax: return "Maximum mana";
            case AttributeType.HealPower: return "Healing power";
            case AttributeType.CriticalChance: return "Critical chance";
            case AttributeType.CriticalMultiplier: return "Critical multiplier";
            case AttributeType.CriticalChanceResist: return "Critical resistance";
            default: return type.ToString();
        }
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
