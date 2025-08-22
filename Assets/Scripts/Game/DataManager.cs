using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DataManager : Singleton<DataManager>
{
    [SerializeField] GameData _data;
    public GameData data { get { return _data; } set { _data = value; } }

    public List<EntityData> entities { get { return _data.entities; } set { _data.entities = value; } } 
    public List<CharacterData> characters { get { return _data.characters; } set { _data.characters = value; } } 
    public List<AItemFactory> items { get { return _data.items; } set { _data.items = value; } } 

    public CharacterData GetRandomCharacter()
    {
        return _data.characters[Random.Range(0, _data.characters.Count)];
    }

    public AItem GetRandomItem()
    {
        return _data.items[Random.Range(0, _data.items.Count)].GetItem();
    }

    public WavePatternData GetWavePattern(int round)
    {
        GameData.WavePerRound wavePerRound = _data.wavePerRound.Where(x => x.round == round).FirstOrDefault();
        return wavePerRound.wavePatterns[Random.Range(0, wavePerRound.wavePatterns.Count)];
    }
}