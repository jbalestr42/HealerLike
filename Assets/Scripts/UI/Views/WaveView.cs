using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class WaveView : AView
{
    public UnityEvent<WavePatternData> OnWaveSelected = new UnityEvent<WavePatternData>();

    [SerializeField] GameObject _waveContainer;

    [SerializeField] GameObject _waveItem;

    List<SelectWaveButton> _waveButtons = new List<SelectWaveButton>();
    int _count = 3;

	public void FillChoices(int currentRound)
    {
		for (int i = 0; i < _count; i++)
        {
            GameObject waveButton = Instantiate(_waveItem);
            waveButton.GetComponent<SelectWaveButton>().Init(DataManager.instance.GetWavePattern(currentRound));
            waveButton.transform.SetParent(_waveContainer.transform);
            _waveButtons.Add(waveButton.GetComponent<SelectWaveButton>());
        }
	}

    public void ClearChoices()
    {
        foreach (SelectWaveButton button in _waveButtons)
        {
            Destroy(button.gameObject);
        }
        _waveButtons.Clear();
    }
    
    #region AView

    public override void Show()
    {
        GetComponent<CanvasGroup>().alpha = 1f;
    }

    public override void Hide()
    {
        GetComponent<CanvasGroup>().alpha = 0.1f;
    }

    #endregion
}
