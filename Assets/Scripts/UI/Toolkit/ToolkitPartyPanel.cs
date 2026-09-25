using System.Collections.Generic;
using UnityEngine;

// The creature cards: the ones left to deploy, then the deployed allies
public class ToolkitPartyPanel
{
    ToolkitGameContext _context;
    ToolkitGameView _view;
    ToolkitDetailPanel _detailPanel;

    public void Init(ToolkitGameContext context, ToolkitGameView view, ToolkitDetailPanel detailPanel)
    {
        _context = context;
        _view = view;
        _detailPanel = detailPanel;
    }

    public void Refresh(bool canDeploy)
    {
        List<ToolkitCardModel> models = new List<ToolkitCardModel>();
        if (_context.legacy.entityInventory != null)
        {
            foreach (SelectEntityButton choice in LegacyUiReader.AvailableEntities(_context.legacy.entityInventory))
            {
                if (choice == null || choice.data == null)
                {
                    continue;
                }

                models.Add(CreateDeployCard(choice.data, canDeploy));
            }
        }

        if (_context.entities != null && _context.entities.entities != null)
        {
            foreach (GameObject entityGo in _context.entities.GetEntities(Entity.EntityType.Player))
            {
                if (entityGo == null)
                {
                    continue;
                }

                models.Add(CreateEntityCard(entityGo));
            }
        }

        _view.SetCards("party-list", models);
    }

    ToolkitCardModel CreateDeployCard(EntityData data, bool canDeploy)
    {
        ToolkitCardModel model = new ToolkitCardModel();
        model.key = $"deploy-{data.GetEntityId()}";
        model.iconSource = data;
        model.title = data.title;
        model.description = data.description;
        model.status = "Deploy";
        model.isEnabled = canDeploy;
        model.source = data;
        model.activate = OnDeployActivated;
        return model;
    }

    ToolkitCardModel CreateEntityCard(GameObject entityGo)
    {
        Entity entity = entityGo.GetComponent<Entity>();
        ToolkitCardModel model = new ToolkitCardModel();
        model.key = $"entity-{entityGo.GetEntityId()}";
        model.iconSource = entity.data;
        model.title = entity.data.title;
        model.description = entity.data.description;
        model.status = "Deployed";
        if (entity.health != null)
        {
            model.status = $"HP {ToolkitPresentation.Resource(entity.health.Value, entity.health.Max)}";
        }

        model.source = entity;
        model.activate = OnEntityActivated;
        return model;
    }

    void OnDeployActivated(ToolkitCardModel model)
    {
        if (_context.interaction != null)
        {
            _context.interaction.SetInteraction(new EntityGridInteraction((EntityData)model.source));
        }

        _view.SetText("status-label", "Tap an open tile to deploy, or Cancel.");
    }

    void OnEntityActivated(ToolkitCardModel model)
    {
        _context.selectedEntity = (Entity)model.source;
        _context.selectedItem = null;
        _detailPanel.RefreshEntity();
    }
}
