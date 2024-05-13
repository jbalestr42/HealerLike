using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class UpgradeView : AView
{
    public UnityEvent<AItem> OnItemSelected = new UnityEvent<AItem>();

    [SerializeField] GameObject _upgradeContainer;

    [SerializeField] GameObject _upgradeItem;

    List<SelectUpgradeButton> _upgradeButtons = new List<SelectUpgradeButton>();
    int _count = 3;

	public void FillChoices()
    {
		for (int i = 0; i < _count; i++)
        {
            GameObject upgradeButton = Instantiate(_upgradeItem);
            upgradeButton.GetComponent<SelectUpgradeButton>().Init(DataManager.instance.GetRandomItem());
            upgradeButton.transform.SetParent(_upgradeContainer.transform);
            _upgradeButtons.Add(upgradeButton.GetComponent<SelectUpgradeButton>());
        }
	}

    public void ClearChoices()
    {
        foreach (SelectUpgradeButton button in _upgradeButtons)
        {
            Destroy(button.gameObject);
        }
        _upgradeButtons.Clear();
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
