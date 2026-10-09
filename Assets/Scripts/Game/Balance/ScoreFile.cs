using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// The score measures, apart from the balance simulations: Logs/WaveScore/<measure>/ for the waves,
// Logs/CharacterScore/<measure>/ for the characters and Logs/ItemScore/<measure>/ for the items, each holding the log of every plan of the measure and
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

    [Serializable]
    public class ItemEntry
    {
        public string item;
        public ItemScore score;
    }

    // The scores of a measure, of the waves, of the characters or of the items
    [Serializable]
    public class Measure
    {
        public string date;
        // Damage per second of the balance team against the dummies
        public float teamDps;
        public List<WaveEntry> waves = new List<WaveEntry>();
        public List<CharacterEntry> characters = new List<CharacterEntry>();
        // The measures of the items without item, the reference of their gains
        public ItemScore itemBaseline;
        // Hash of the setup of the item measure (plans, balance team, dummies) when it was played: the fights without
        // item can be taken again while it stays the same
        public string itemSetupFingerprint;
        // Folder (its name, under the item root) whose logs hold the fights without item of the measure: its own,
        // or the one of a previous measure they were taken from
        public string itemBaselineFolder;
        public List<ItemEntry> items = new List<ItemEntry>();
    }

    static string logs => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Logs");
    public static string waveRoot => Path.Combine(logs, "WaveScore");
    public static string characterRoot => Path.Combine(logs, "CharacterScore");
    public static string itemRoot => Path.Combine(logs, "ItemScore");

    public static void Write(string folder, Measure measure)
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, FileName), JsonUtility.ToJson(measure, true));
    }

    // Adds to the measure the scores of the previous one it doesn't hold (e.g. a single wave measured again keeps
    // the scores of the others). Without previous measure, nothing changes
    public static void Merge(Measure measure, Measure previous)
    {
        if (previous == null)
        {
            return;
        }

        measure.waves.AddRange(previous.waves.Where(entry => !measure.waves.Exists(measured => measured.wave == entry.wave)));
        measure.characters.AddRange(previous.characters.Where(entry => !measure.characters.Exists(measured => measured.character == entry.character)));
        measure.items.AddRange(previous.items.Where(entry => !measure.items.Exists(measured => measured.item == entry.item)));
    }

    // The folder holding the fights without item the latest item measure was computed with, when they can be taken
    // again: the setup unchanged since, and the logs still there. Null otherwise, the fights to be played again
    public static string FindItemBaseline(string itemMeasuresRoot, string setupFingerprint, IEnumerable<string> logNames)
    {
        string latest = FindLatest(itemMeasuresRoot);
        Measure measure = latest != null ? Read(latest) : null;
        if (measure == null || string.IsNullOrEmpty(setupFingerprint) || measure.itemSetupFingerprint != setupFingerprint || string.IsNullOrEmpty(measure.itemBaselineFolder))
        {
            return null;
        }

        string baseline = Path.Combine(itemMeasuresRoot, measure.itemBaselineFolder);
        return logNames.All(log => File.Exists(Path.Combine(baseline, log))) ? baseline : null;
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
