using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Highest ascension level each character unlocked, kept between game sessions in a file. A character starts with
// only the level 0, and winning a run at a level unlocks the next one for that character only
[Serializable]
public class AscensionProgress
{
    [Serializable]
    class Entry
    {
        // CharacterData asset name, renaming the asset loses its progress
        public string character;
        public int unlockedLevel;
    }

    const string FileName = "ascension.json";

    [SerializeField] List<Entry> _entries = new List<Entry>();

    public static string defaultPath => Path.Combine(Application.persistentDataPath, FileName);

    public static string GetKey(CharacterData character)
    {
        return character != null ? character.name : null;
    }

    public int GetUnlockedLevel(string character)
    {
        Entry entry = Find(character);
        return entry != null ? entry.unlockedLevel : 0;
    }

    // The level after the one played is unlocked, up to maxLevel. False when nothing new was unlocked: a lower
    // level won again, or the highest one
    public bool RecordWin(string character, int playedLevel, int maxLevel)
    {
        if (string.IsNullOrEmpty(character))
        {
            return false;
        }

        int unlocked = Mathf.Min(playedLevel + 1, maxLevel);
        Entry entry = Find(character);
        if (entry == null)
        {
            entry = new Entry { character = character };
            _entries.Add(entry);
        }
        if (unlocked <= entry.unlockedLevel)
        {
            return false;
        }

        entry.unlockedLevel = unlocked;
        return true;
    }

    Entry Find(string character)
    {
        return _entries.Find(entry => entry.character == character);
    }

    public string ToJson()
    {
        return JsonUtility.ToJson(this, true);
    }

    // Empty when the json can't be read, so a broken file only costs the progress, never the game
    public static AscensionProgress FromJson(string json)
    {
        AscensionProgress progress = null;
        try
        {
            progress = string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<AscensionProgress>(json);
        }
        catch (ArgumentException e)
        {
            Debug.LogWarning($"[AscensionProgress] Could not read the progress: {e.Message}");
        }
        return progress ?? new AscensionProgress();
    }

    // Empty when there is no file yet (first game)
    public static AscensionProgress Load(string path)
    {
        return File.Exists(path) ? FromJson(File.ReadAllText(path)) : new AscensionProgress();
    }

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, ToJson());
    }
}
