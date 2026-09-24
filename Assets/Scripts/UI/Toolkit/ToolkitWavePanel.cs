using System.Collections.Generic;

// The choice of the next encounter shown by the legacy wave view
public class ToolkitWavePanel
{
    ToolkitGameContext _context;
    ToolkitGameView _view;

    public void Init(ToolkitGameContext context, ToolkitGameView view)
    {
        _context = context;
        _view = view;
    }

    public void Refresh()
    {
        if (LegacyUiReader.CurrentView(_context.ui) != ViewType.Wave)
        {
            return;
        }

        List<ToolkitCardModel> models = new List<ToolkitCardModel>();
        foreach (SelectWaveButton choice in LegacyUiReader.WaveChoices(_context.ui.GetView<WaveView>(ViewType.Wave)))
        {
            if (choice == null)
            {
                continue;
            }

            WavePatternData wave = LegacyUiReader.Wave(choice);
            if (wave == null)
            {
                continue;
            }

            ToolkitCardModel model = new ToolkitCardModel();
            model.iconSource = wave;
            model.title = wave.name;
            model.status = "Choose wave";
            model.source = choice;
            model.activate = OnWaveActivated;
            models.Add(model);
        }

        _view.SetCards("selection-list", models);
    }

    void OnWaveActivated(ToolkitCardModel model)
    {
        ((SelectWaveButton)model.source).SelectWave();
    }
}
