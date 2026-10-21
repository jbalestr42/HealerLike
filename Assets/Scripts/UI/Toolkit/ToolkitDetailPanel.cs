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
    // The foldouts are rebuilt a few times a second, the summary every refresh
    const float SectionPeriod = 0.25f;
    List<Section> _sections = new List<Section>();
    Entity _sectionEntity;
    float _nextSectionTime = 0f;
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
        BuildSections();
    }

    public void Dispose()
    {
        if (_targeting != null)
        {
            _targeting.UnregisterValueChangedCallback(OnTargetingChanged);
        }

        _targeting = null;
        _actionsHost = null;
        _sections.Clear();
        _sectionEntity = null;
        _nextSectionTime = 0f;
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
            if (_inspectedEntity != null && _context.selectedItem == null)
            {
                RefreshEntity(_inspectedEntity);
            }

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
                RefreshEntity(_context.selectedEntity);
            }
        }
    }

    // Explicit refreshes (selection, equipment change) rebuild the foldouts right away
    public void RefreshEntity()
    {
        _nextSectionTime = 0f;
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
        RefreshSections(entity);
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
        Foldout attributes = _view.root.Q<Foldout>("detail-attributes");
        if (attributes != null)
        {
            attributes.value = false;
        }

        foreach (Section section in _sections)
        {
            section.foldout.value = false;
        }

        if (model.source is Entity entity)
        {
            SelectableEntity selectable = entity.GetComponent<SelectableEntity>();
            if (_context.interaction != null && selectable != null)
            {
                _context.interaction.CancelSelection();
                _context.interaction.Select(selectable);
            }
            _context.selectedEntity = entity;
            _context.selectedItem = null;
            RefreshEntity();
        }
        else
        {
            _view.ShowDetail(model);
        }
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
        List<string> lines = new List<string>();
        string health = ToolkitEntityInfo.GetHealthLine(entity);
        if (health.Length > 0)
        {
            lines.Add(health);
        }

        List<string> combat = new List<string>();
        foreach (AttributeType type in new[] { AttributeType.Damage, AttributeType.AttackRate,
            AttributeType.Range, AttributeType.FlatArmor })
        {
            if (entity.attributeManager != null && entity.attributeManager.Has(type))
            {
                float value = entity.attributeManager.Get(type).Value;
                if (!Mathf.Approximately(value, 0))
                {
                    combat.Add($"{AttributeLabel(type)}: {value:0.##}");
                }
            }
        }
        for (int i = 0; i < combat.Count; i += 2)
        {
            lines.Add(combat[i] + (i + 1 < combat.Count ? " · " + combat[i + 1] : ""));
        }

        return string.Join("\n", lines);
    }

    static string GetStats(Entity entity)
    {
        return ToolkitEntityInfo.Join(ToolkitEntityInfo.GetAttributeLines(entity));
    }

    void BuildSections()
    {
        _sections.Clear();
        AddSection("detail-aiming", "Targeting details", false, ToolkitEntityInfo.GetTargeting);
        AddSection("detail-skills", "Skills", true, ToolkitEntityInfo.GetSkills);
        AddSection("detail-items", "Items", true, ToolkitEntityInfo.GetItems);
        AddSection("detail-effects", "Effects", true, ToolkitEntityInfo.GetEffects);
    }

    void AddSection(string name, string heading, bool showCount, Func<Entity, ToolkitInfoSection> build)
    {
        Foldout foldout = _view.root.Q<Foldout>(name);
        Label body = _view.root.Q<Label>(name + "-text");
        if (foldout == null || body == null)
        {
            Debug.LogError("[ToolkitDetailPanel] Detail section '" + name + "' is missing.");
            return;
        }

        Section section = new Section();
        section.foldout = foldout;
        section.body = body;
        section.heading = heading;
        section.showCount = showCount;
        section.build = build;
        _sections.Add(section);
    }

    // The summary follows the creature every refresh. The foldouts and the attribute list only
    // rebuild a few times a second: they join many lines, and nobody reads them faster.
    void RefreshSections(Entity entity)
    {
        if (entity == _sectionEntity && Time.unscaledTime < _nextSectionTime)
        {
            return;
        }

        _sectionEntity = entity;
        _nextSectionTime = Time.unscaledTime + SectionPeriod;
        _view.SetText("detail-full-stats", GetStats(entity));
        foreach (Section section in _sections)
        {
            ToolkitInfoSection info = section.build(entity);
            bool hasContent = info.lines.Count > 0;
            section.foldout.EnableInClassList("is-hidden", !hasContent);
            if (!hasContent)
            {
                continue;
            }

            section.foldout.text = section.showCount ? $"{section.heading} ({info.count})" : section.heading;
            string text = ToolkitEntityInfo.Join(info.lines);
            if (section.body.text != text)
            {
                section.body.text = text;
            }
        }
    }

    class Section
    {
        public Foldout foldout;
        public Label body;
        public string heading;
        public bool showCount;
        public Func<Entity, ToolkitInfoSection> build;
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
