using System.Collections.Generic;
using UnityEngine;

public class ToolkitPartyPanel
{
    ToolkitGameContext _context;
    ToolkitGameView _view;
    readonly ToolkitRoster _roster = new ToolkitRoster();
    public void Init(ToolkitGameContext context, ToolkitGameView view, ToolkitDetailPanel detailPanel)
    { _context = context; _view = view; _roster.Clear(); }

    public void Refresh(bool canDeploy)
    {
        var choices = _context.legacy.entityInventory != null
            ? LegacyUiReader.AvailableEntities(_context.legacy.entityInventory) : System.Array.Empty<SelectEntityButton>();
        var entities = new List<Entity>();
        if (_context.entities != null && _context.entities.entities != null)
            foreach (GameObject go in _context.entities.GetEntities(Entity.EntityType.Player))
                if (go != null) entities.Add(go.GetComponent<Entity>());
        _roster.Sync(choices, entities);
        var models = new List<ToolkitCardModel>();
        foreach (ToolkitRoster.Entry entry in _roster.entries)
        {
            Entity entity = entry.entity;
            var model = new ToolkitCardModel { key = entry.key, iconSource = entity != null ? (object)entity : entry.data,
                title = entry.data.title, description = entry.data.description,
                source = entity != null ? (object)entity : entry.data, canDrag = entity == null && canDeploy,
                isEnabled = true, activate = Inspect, deployed = spawned => { entry.entity = spawned; entry.choice = null; } };
            if (entity != null && entity.health != null)
                model.healthFraction = entity.health.Value / Mathf.Max(1, entity.health.Max);
            models.Add(model);
        }
        _view.SetCards("party-list", models);
    }
    void Inspect(ToolkitCardModel model) { _view.OnInspectRequested.Invoke(model); }
}
