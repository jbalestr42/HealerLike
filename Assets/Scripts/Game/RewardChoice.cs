using System.Collections.Generic;
using UnityEngine;

// What a choice of a reward gives
public enum RewardChoiceType
{
    Unit,
    PlayerItem,
    EntityItem,
}

// One choice of a reward: a unit joining the team, an item of the character or an item to give to a unit
public class RewardChoice
{
    public RewardChoiceType type;
    // Null for a unit
    public AItem item;
    // Null for an item
    public EntityData unit;

    // Never offered as a reward: those items only come from events (e.g. the Library)
    public static readonly List<string> ExcludedTags = new List<string> { TagNames.Cursed, TagNames.Library };

    // count choices for the character, each drawn on its own (the same choice can come twice)
    public static List<RewardChoice> Roll(int count, CharacterData character)
    {
        GameData data = DataManager.instance.data;
        List<EntityData> rewardEntities = DataManager.instance.GetRewardEntities(character);
        List<RewardChoice> choices = new List<RewardChoice>();
        for (int i = 0; i < count; i++)
        {
            RewardChoiceType type = PickChoiceType(Random.value, Random.value, data.unitRewardChance, data.playerItemChance, rewardEntities.Count > 0);
            RewardChoice choice = new RewardChoice { type = type };
            if (type == RewardChoiceType.Unit)
            {
                choice.unit = rewardEntities[Random.Range(0, rewardEntities.Count)];
            }
            else
            {
                string tagName = type == RewardChoiceType.PlayerItem ? TagNames.Player : TagNames.Entity;
                choice.item = DataManager.instance.GetRandomItem(new List<string> { tagName }, ExcludedTags, character);
            }
            choices.Add(choice);
        }
        return choices;
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
}
