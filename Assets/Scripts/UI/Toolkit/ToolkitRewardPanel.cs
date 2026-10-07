using System.Collections.Generic;
using UnityEngine;

// The reward choice shown by the legacy upgrade view after a wave
public class ToolkitRewardPanel
{
    ToolkitGameUI _gameUI;
    ToolkitGameContext _context;
    ToolkitGameView _view;

    public void Init(ToolkitGameUI gameUI, ToolkitGameContext context, ToolkitGameView view)
    {
        _gameUI = gameUI;
        _context = context;
        _view = view;
    }

    public void Refresh()
    {
        if (LegacyUiReader.CurrentView(_context.ui) != ViewType.Upgrade)
        {
            return;
        }

        UpgradeView upgradeView = _context.ui.GetView<UpgradeView>(ViewType.Upgrade);
        _view.SetCards("upgrade-list", BuildModels(LegacyUiReader.UpgradeChoices(upgradeView)));
    }

    // One card per choice of the legacy view: an item for the party, an upgrade for the healer, or a new unit
    public List<ToolkitCardModel> BuildModels(IReadOnlyList<GameObject> choices)
    {
        List<ToolkitCardModel> models = new List<ToolkitCardModel>();
        foreach (GameObject choiceGo in choices)
        {
            if (choiceGo == null)
            {
                continue;
            }

            ToolkitCardModel model = BuildModel(choiceGo);
            if (model == null)
            {
                continue;
            }

            model.activate = OnRewardActivated;
            models.Add(model);
        }

        return models;
    }

    static ToolkitCardModel BuildModel(GameObject choiceGo)
    {
        SelectEntityUpgradeButton unitChoice = choiceGo.GetComponent<SelectEntityUpgradeButton>();
        if (unitChoice != null)
        {
            // Reward-only units, tagged for rewards but outside the character's roster, come through the same button
            EntityData entity = LegacyUiReader.Unit(unitChoice);
            if (entity == null)
            {
                return null;
            }

            ToolkitCardModel unitModel = new ToolkitCardModel();
            unitModel.iconSource = entity;
            unitModel.title = SelectEntityUpgradeButton.GetTitle(entity);
            unitModel.description = CharacterCardText.GetUnitDetails(entity);
            unitModel.status = "New unit · Choose reward";
            unitModel.source = unitChoice;
            return unitModel;
        }

        SelectItemUpgradeButton entityChoice = choiceGo.GetComponent<SelectItemUpgradeButton>();
        SelectPlayerItemUpgradeButton playerChoice = choiceGo.GetComponent<SelectPlayerItemUpgradeButton>();
        AItem item = entityChoice != null ? LegacyUiReader.Item(entityChoice) : LegacyUiReader.Item(playerChoice);
        if (item == null)
        {
            return null;
        }

        ToolkitCardModel model = new ToolkitCardModel();
        model.iconSource = item;
        model.title = item.title;
        model.description = LegacyUiReader.ItemDescription(item);
        if (entityChoice != null)
        {
            model.status = "Party equipment · Choose reward";
            model.source = entityChoice;
        }
        else
        {
            model.status = "Healer upgrade · Choose reward";
            model.source = playerChoice;
        }

        return model;
    }

    void OnRewardActivated(ToolkitCardModel model)
    {
        if (model.source is SelectEntityUpgradeButton)
        {
            ((SelectEntityUpgradeButton)model.source).SelectUpgrade();
        }
        else if (model.source is SelectItemUpgradeButton)
        {
            ((SelectItemUpgradeButton)model.source).SelectUpgrade();
        }
        else
        {
            ((SelectPlayerItemUpgradeButton)model.source).SelectUpgrade();
        }

        _gameUI.Refresh();
    }
}
