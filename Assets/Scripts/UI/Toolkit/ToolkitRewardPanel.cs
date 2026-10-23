using System.Collections.Generic;

// The choice of a reward after a fight or in a treasure room, a card per choice
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
        if (_context.ascension == null || !_context.IsCurrentView(GameScreen.Reward))
        {
            return;
        }

        _view.SetCards("upgrade-list", BuildModels(_context.ascension.rewardChoices));
    }

    // One card per choice: an item for the party, an upgrade for the healer, or a new unit
    public List<ToolkitCardModel> BuildModels(IReadOnlyList<RewardChoice> choices)
    {
        List<ToolkitCardModel> models = new List<ToolkitCardModel>();
        foreach (RewardChoice choice in choices)
        {
            ToolkitCardModel model = BuildModel(choice);
            if (model == null)
            {
                continue;
            }

            model.source = choice;
            model.activate = OnRewardActivated;
            models.Add(model);
        }

        return models;
    }

    static ToolkitCardModel BuildModel(RewardChoice choice)
    {
        if (choice == null)
        {
            return null;
        }

        ToolkitCardModel model = new ToolkitCardModel();
        if (choice.type == RewardChoiceType.Unit)
        {
            if (choice.unit == null)
            {
                return null;
            }

            model.iconSource = choice.unit;
            // "New unit: Zealot", so a unit can't be mistaken for an item
            model.title = $"New unit: {choice.unit.title}";
            model.description = CharacterCardText.GetUnitDetails(choice.unit);
            model.status = "New unit · Choose reward";
            return model;
        }

        if (choice.item == null)
        {
            return null;
        }

        model.iconSource = choice.item;
        model.title = choice.item.title;
        model.description = ToolkitItemEntry.GetDescription(choice.item);
        model.status = choice.type == RewardChoiceType.EntityItem
            ? "Party equipment · Choose reward" : "Healer upgrade · Choose reward";
        return model;
    }

    void OnRewardActivated(ToolkitCardModel model)
    {
        _context.ascension.SelectReward((RewardChoice)model.source);
        if (_gameUI != null)
        {
            _gameUI.Refresh();
        }
    }
}
