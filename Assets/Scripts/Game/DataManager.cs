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

    public WavePatternData GetWavePattern(int round)
    {
        GameData.WavePerRound wavePerRound = _data.wavePerRound.Where(x => x.round == round).FirstOrDefault();
        return wavePerRound.wavePatterns[Random.Range(0, wavePerRound.wavePatterns.Count)];
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