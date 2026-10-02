using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DataManager : Singleton<DataManager>
{
    [SerializeField] GameData _data;
    public GameData data { get { return _data; } set { _data = value; } }

    public List<CharacterData> characters { get { return _data.characters; } set { _data.characters = value; } } 
    public List<AItemFactory> items { get { return _data.items; } set { _data.items = value; } } 

    public CharacterData GetRandomCharacter()
    {
        return _data.characters[Random.Range(0, _data.characters.Count)];
    }

    // The chosen character when it's one of the game characters, a random one otherwise (ex: the
    // Main scene started directly from the Editor, without going through the menu)
    public CharacterData GetCharacter(CharacterData chosen)
    {
        if (chosen != null && _data.characters.Contains(chosen))
        {
            return chosen;
        }
        return GetRandomCharacter();
    }

    // Items having every included tag and none of the excluded ones, a tag also matching its descendants
    // (e.g. the player items that aren't cursed). No item when an included tag isn't registered in the game
    // data, an unknown excluded tag excludes nothing
    public List<AItemFactory> GetItems(List<string> includedTags, List<string> excludedTags = null)
    {
        List<GameplayTag> included = new List<GameplayTag>();
        foreach (string tagName in includedTags)
        {
            GameplayTag tag = GetTagWithName(tagName);
            if (tag == null)
            {
                return new List<AItemFactory>();
            }
            included.Add(tag);
        }

        List<GameplayTag> excluded = new List<GameplayTag>();
        if (excludedTags != null)
        {
            foreach (string tagName in excludedTags)
            {
                GameplayTag tag = GetTagWithName(tagName);
                if (tag != null)
                {
                    excluded.Add(tag);
                }
            }
        }

        return _data.items.FindAll(item => item != null && included.TrueForAll(tag => HasTag(item, tag)) && !excluded.Exists(tag => HasTag(item, tag)));
    }

    // One of those items, null when there is none
    public AItem GetRandomItem(List<string> includedTags, List<string> excludedTags = null)
    {
        List<AItemFactory> items = GetItems(includedTags, excludedTags);
        return items.Count > 0 ? items[Random.Range(0, items.Count)].GetItem() : null;
    }

    static bool HasTag(AItemFactory item, GameplayTag tag)
    {
        return item.tags.Exists(itemTag => itemTag != null && (itemTag == tag || itemTag.IsDescendantOf(tag)));
    }

    // Waves of every pool matching the room type and floor
    public List<WavePatternData> GetWavePatterns(MapNodeType roomType, int floor)
    {
        return _data.wavePools
            .Where(pool => pool.roomType == roomType && floor >= pool.minFloor && floor <= pool.maxFloor)
            .SelectMany(pool => pool.wavePatterns)
            .Where(wave => wave != null)
            .ToList();
    }

    // Rooms without their own waves (elites for now) fight the combat waves of their floor
    public WavePatternData GetWavePattern(MapNodeType roomType, int floor, System.Random random)
    {
        List<WavePatternData> waves = GetWavePatterns(roomType, floor);
        if (waves.Count == 0 && roomType != MapNodeType.Combat)
        {
            waves = GetWavePatterns(MapNodeType.Combat, floor);
        }

        if (waves.Count == 0)
        {
            Debug.LogError($"[DataManager] No wave for a {roomType} room on floor {floor}");
            return null;
        }
        return waves[random.Next(waves.Count)];
    }

    public GameplayTag GetTagWithName(string tagName)
    {
        foreach (GameplayTag tag in _data.tags)
        {
            if (tag.name == tagName)
            {
                return tag;
            }
        }

        return null;
    }
}