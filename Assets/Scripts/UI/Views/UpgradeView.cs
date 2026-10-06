using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// What a choice of a reward gives
public enum RewardChoiceType
{
    Unit,
    PlayerItem,
    EntityItem,
}

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
    List<GameObject> _upgradeButtonsSource;
    System.Collections.ObjectModel.ReadOnlyCollection<GameObject> _upgradeButtonsView;

    public IReadOnlyList<GameObject> upgradeButtons
    {
        get
        {
            if (_upgradeButtonsView == null || _upgradeButtonsSource != _upgradeButtons)
            {
                _upgradeButtonsSource = _upgradeButtons;
                _upgradeButtonsView = _upgradeButtons.AsReadOnly();
            }
            return _upgradeButtonsView;
        }
    }

    // Never offered as a reward: those items only come from events (e.g. the Library)
    public static readonly List<string> RewardExcludedTags = new List<string> { TagNames.Cursed, TagNames.Library };

	public void FillChoices(int count)
    {
        GameData data = DataManager.instance.data;
        List<EntityData> rewardEntities = DataManager.instance.GetRewardEntities(PlayerBehaviour.instance.character.data);
		for (int i = 0; i < count; i++)
        {
            // TODO: improve with a bit of abstraction when we have more upgrade types
            GameObject upgradeButton = null;
            RewardChoiceType type = PickChoiceType(Random.value, Random.value, data.unitRewardChance, data.playerItemChance, rewardEntities.Count > 0);
            if (type == RewardChoiceType.Unit)
            {
                upgradeButton = Instantiate(_upgradeEntity);
                upgradeButton.GetComponent<SelectEntityUpgradeButton>().Init(rewardEntities[Random.Range(0, rewardEntities.Count)]);
            }
            else if (type == RewardChoiceType.PlayerItem)
            {
                upgradeButton = Instantiate(_upgradePlayerItem);
                upgradeButton.GetComponent<SelectPlayerItemUpgradeButton>().Init(DataManager.instance.GetRandomItem(new List<string> { TagNames.Player }, RewardExcludedTags));
            }
            else
            {
                upgradeButton = Instantiate(_upgradeItem);
                upgradeButton.GetComponent<SelectItemUpgradeButton>().Init(DataManager.instance.GetRandomItem(new List<string> { TagNames.Entity }, RewardExcludedTags));
            }
            upgradeButton.transform.SetParent(_upgradeContainer.transform);
            _upgradeButtons.Add(upgradeButton);
        }
	}

    // A unit first, when the character has units to offer, then a player item, otherwise an entity item. Each
    // roll is in [0, 1)
    public static RewardChoiceType PickChoiceType(float unitRoll, float itemRoll, float unitChance, float playerItemChance, bool canOfferUnit)
    {
        if (canOfferUnit && unitRoll < unitChance)
        {
            return RewardChoiceType.Unit;
        }
        return itemRoll < playerItemChance ? RewardChoiceType.PlayerItem : RewardChoiceType.EntityItem;
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
