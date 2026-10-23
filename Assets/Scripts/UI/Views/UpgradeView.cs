using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class UpgradeView : AView
{
    public UnityEvent<AItem> OnItemSelected = new UnityEvent<AItem>();
    public UnityEvent<AItem> OnPlayerItemSelected = new UnityEvent<AItem>();
    public UnityEvent<EntityData> OnEntitySelected = new UnityEvent<EntityData>();

    [SerializeField] GameObject _upgradeContainer;

    [SerializeField] GameObject _upgradeItem;

    [SerializeField] GameObject _upgradePlayerItem;

    [SerializeField] GameObject _upgradeEntity;

    List<GameObject> _upgradeButtons = new List<GameObject>();

    // A button per choice
    public void FillChoices(IReadOnlyList<RewardChoice> choices)
    {
        ClearChoices();
        foreach (RewardChoice choice in choices)
        {
            GameObject upgradeButton = null;
            if (choice.type == RewardChoiceType.Unit)
            {
                upgradeButton = Instantiate(_upgradeEntity);
                upgradeButton.GetComponent<SelectEntityUpgradeButton>().Init(choice.unit);
            }
            else if (choice.type == RewardChoiceType.PlayerItem)
            {
                upgradeButton = Instantiate(_upgradePlayerItem);
                upgradeButton.GetComponent<SelectPlayerItemUpgradeButton>().Init(choice.item);
            }
            else
            {
                upgradeButton = Instantiate(_upgradeItem);
                upgradeButton.GetComponent<SelectItemUpgradeButton>().Init(choice.item);
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
