using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// The score measures, apart from the balance simulations: Logs/WaveScore/<measure>/ for the waves and
// Logs/CharacterScore/<measure>/ for the characters, each holding the log of every plan of the measure and
// scores.json, written only once every plan played to its end
public static class ScoreFile
{
    public const string FileName = "scores.json";

    [Serializable]
    public class WaveEntry
    {
        public string wave;
        public WaveScore score;
    }

    [Serializable]
    public class CharacterEntry
    {
        public string character;
        public CharacterScore score;
    }

    // The scores of a measure, of the waves or of the characters
    [Serializable]
    public class Measure
    {
        public string date;
        // Damage per second of the balance team against the dummies
        public float teamDps;
        public List<WaveEntry> waves = new List<WaveEntry>();
        public List<CharacterEntry> characters = new List<CharacterEntry>();
    }

    static string logs => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Logs");
    public static string waveRoot => Path.Combine(logs, "WaveScore");
    public static string characterRoot => Path.Combine(logs, "CharacterScore");

    public static void Write(string folder, Measure measure)
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, FileName), JsonUtility.ToJson(measure, true));
    }

    // Null when the folder holds no finished measure
    public static Measure Read(string folder)
    {
        string path = Path.Combine(folder, FileName);
        return File.Exists(path) ? JsonUtility.FromJson<Measure>(File.ReadAllText(path)) : null;
    }

    // The folder of the latest finished measure under the root, null without any
    public static string FindLatest(string measuresRoot)
    {
        if (!Directory.Exists(measuresRoot))
        {
            return null;
        }
        return Directory.GetDirectories(measuresRoot)
            .Where(folder => File.Exists(Path.Combine(folder, FileName)))
            .OrderByDescending(folder => Path.GetFileName(folder), StringComparer.Ordinal)
            .FirstOrDefault();
    }
}
