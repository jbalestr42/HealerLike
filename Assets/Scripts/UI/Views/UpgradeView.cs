using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class UpgradeView : AView
{
    public UnityEvent<AItem> OnItemSelected = new UnityEvent<AItem>();
    public UnityEvent<AItem> OnPlayerItemSelected = new UnityEvent<AItem>();

    [SerializeField] GameObject _upgradeContainer;

    [SerializeField] GameObject _upgradeItem;

    [SerializeField] GameObject _upgradePlayerItem;

    List<GameObject> _upgradeButtons = new List<GameObject>();
    int _count = 3;

	public void FillChoices()
    {
		for (int i = 0; i < _count; i++)
        {
            // TODO: improve with a bit of abstraction when we have more upgrade types
            GameObject upgradeButton = null;
            if (Random.Range(0f, 1f) < DataManager.instance.data.playerItemChance)
            {
                upgradeButton = Instantiate(_upgradePlayerItem);
                upgradeButton.GetComponent<SelectPlayerItemUpgradeButton>().Init(DataManager.instance.GetRandomPlayerItem());
            }
            else
            {
                upgradeButton = Instantiate(_upgradeItem);
                upgradeButton.GetComponent<SelectItemUpgradeButton>().Init(DataManager.instance.GetRandomItem());
            }
            upgradeButton.transform.SetParent(_upgradeContainer.transform);
            _upgradeButtons.Add(upgradeButton);
        }
	}

    public void ClearChoices()
    {
        foreach (GameObject button in _upgradeButtons)
        {
            Destroy(button);
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
