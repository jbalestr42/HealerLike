using System.Collections.Generic;

// What the rewards can give, with the chances of the game data
public class RewardPools
{
    public List<EntityData> units = new List<EntityData>();
    public List<AItemFactory> unitItems = new List<AItemFactory>();
    public List<AItemFactory> playerItems = new List<AItemFactory>();
    public float unitChance;
    public float playerItemChance;

    // Same pools as the reward screen: the units of the class of the character tagged Reward, and the unit and
    // player items but the cursed and library ones
    public static RewardPools Create(GameData data, CharacterData character)
    {
        RewardPools pools = new RewardPools
        {
            unitChance = data.unitRewardChance,
            playerItemChance = data.playerItemChance,
        };

        if (character != null && character.classTag != null)
        {
            pools.units = data.entities.FindAll(unit => unit != null && unit.HasTag(character.classTag) && unit.HasTag(TagNames.Reward));
        }
        pools.unitItems = data.items.FindAll(item => IsReward(item, TagNames.Entity));
        pools.playerItems = data.items.FindAll(item => IsReward(item, TagNames.Player));
        return pools;
    }

    static bool IsReward(AItemFactory item, string tagName)
    {
        return item != null && item.HasTag(tagName) && !UpgradeView.RewardExcludedTags.Exists(item.HasTag);
    }
}
