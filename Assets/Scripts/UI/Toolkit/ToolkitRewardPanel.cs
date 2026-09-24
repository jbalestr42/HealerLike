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

        List<ToolkitCardModel> models = new List<ToolkitCardModel>();
        UpgradeView upgradeView = _context.ui.GetView<UpgradeView>(ViewType.Upgrade);
        foreach (GameObject choiceGo in LegacyUiReader.UpgradeChoices(upgradeView))
        {
            if (choiceGo == null)
            {
                continue;
            }

            SelectItemUpgradeButton entityChoice = choiceGo.GetComponent<SelectItemUpgradeButton>();
            SelectPlayerItemUpgradeButton playerChoice = choiceGo.GetComponent<SelectPlayerItemUpgradeButton>();
            AItem item = entityChoice != null ? LegacyUiReader.Item(entityChoice) : LegacyUiReader.Item(playerChoice);
            if (item == null)
            {
                continue;
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

            model.activate = OnRewardActivated;
            models.Add(model);
        }

        _view.SetCards("upgrade-list", models);
    }

    void OnRewardActivated(ToolkitCardModel model)
    {
        if (model.source is SelectItemUpgradeButton)
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
