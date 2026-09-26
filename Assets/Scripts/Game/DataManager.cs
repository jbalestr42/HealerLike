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

    // Items having the tag, or one of its descendants
    public List<AItemFactory> GetItemsWithTag(GameplayTag tag)
    {
        return _data.items.FindAll(item => item != null && item.tags.Exists(itemTag => itemTag == tag || itemTag.IsDescendantOf(tag)));
    }

    public List<AItemFactory> GetItemsWithTag(string tagName)
    {
        return GetItemsWithTag(GetTagWithName(tagName));
    }

    public AItem GetRandomItemWithTag(string tagName)
    {
        List<AItemFactory> items = GetItemsWithTag(tagName);
        return items[Random.Range(0, items.Count)].GetItem();
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