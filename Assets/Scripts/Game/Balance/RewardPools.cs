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
    // player items but the cursed and library ones and the ones of another class
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
        pools.unitItems = data.items.FindAll(item => IsReward(item, TagNames.Entity) && IsForCharacter(item, data, character));
        pools.playerItems = data.items.FindAll(item => IsReward(item, TagNames.Player) && IsForCharacter(item, data, character));
        return pools;
    }

    // Without a character (e.g. listing every reward item), no class filter
    static bool IsForCharacter(AItemFactory item, GameData data, CharacterData character)
    {
        return character == null || ClassItemFilter.IsForCharacter(item, character, data.characters);
    }

    static bool IsReward(AItemFactory item, string tagName)
    {
        return item != null && item.HasTag(tagName) && !UpgradeView.RewardExcludedTags.Exists(item.HasTag);
    }
}
