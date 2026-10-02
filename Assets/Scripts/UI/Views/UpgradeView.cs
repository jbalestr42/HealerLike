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

    // Never offered as a reward: the cursed items only come from the Library
    static readonly List<string> RewardExcludedTags = new List<string> { CursedTag.Name };

	public void FillChoices(int count)
    {
        GameData data = DataManager.instance.data;
        List<EntityData> rewardEntities = PlayerBehaviour.instance.character.data.GetRewardEntities();
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
                upgradeButton.GetComponent<SelectPlayerItemUpgradeButton>().Init(DataManager.instance.GetRandomItem(new List<string> { "Player" }, RewardExcludedTags));
            }
            else
            {
                upgradeButton = Instantiate(_upgradeItem);
                upgradeButton.GetComponent<SelectItemUpgradeButton>().Init(DataManager.instance.GetRandomItem(new List<string> { "Entity" }, RewardExcludedTags));
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
