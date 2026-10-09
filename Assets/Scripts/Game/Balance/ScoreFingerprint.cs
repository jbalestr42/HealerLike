#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// Hash of everything a score depends on: the wave or the character measured and every data they use, and the
// setup of the measures (plans, dummies, balance team). Only the data assets count, followed from data to data: the
// models, prefabs, materials and scripts are neither hashed nor followed, so what they reference doesn't count
// either
public static class ScoreFingerprint
{
    public static string Compute(WavePatternData wave, IEnumerable<Object> measureSetup)
    {
        return ComputeAll(new[] { wave }, measureSetup)[wave];
    }

    // Data holding no score of their own (e.g. a character and its healer bot): their files are hashed whole. The
    // excluded data (e.g. the other characters, listed by the plans) are neither hashed nor followed
    public static string ComputeForData(IEnumerable<Object> measured, IEnumerable<Object> measureSetup, IEnumerable<Object> excluded = null)
    {
        string[] roots = GetPaths(measured.Concat(measureSetup));
        IEnumerable<string> contents = CollectDataDependencies(roots, null, GetPaths(excluded ?? new Object[0])).OrderBy(path => path, System.StringComparer.Ordinal).Select(ReadContent);
        return WaveScore.ComputeFingerprint(contents);
    }

    // The same as ComputeForData for each data on its own (e.g. every item), the measure setup and the files shared
    // by the data read only once
    public static Dictionary<Object, string> ComputeAllForData(IEnumerable<Object> measured, IEnumerable<Object> measureSetup)
    {
        Dictionary<string, string[]> directDependencies = new Dictionary<string, string[]>();
        HashSet<string> setupDependencies = CollectDataDependencies(GetPaths(measureSetup), directDependencies);
        Dictionary<string, string> readFiles = new Dictionary<string, string>();

        Dictionary<Object, string> fingerprints = new Dictionary<Object, string>();
        foreach (Object data in measured.Where(data => data != null).Distinct())
        {
            HashSet<string> dependencies = new HashSet<string>(setupDependencies);
            dependencies.UnionWith(CollectDataDependencies(GetPaths(new[] { data }), directDependencies));
            List<string> contents = new List<string>();
            foreach (string path in dependencies.OrderBy(path => path, System.StringComparer.Ordinal))
            {
                if (!readFiles.TryGetValue(path, out string content))
                {
                    content = ReadContent(path);
                    readFiles[path] = content;
                }
                contents.Add(content);
            }
            fingerprints[data] = WaveScore.ComputeFingerprint(contents);
        }
        return fingerprints;
    }

    static string[] GetPaths(IEnumerable<Object> assets)
    {
        return assets.Where(asset => asset != null).Select(AssetDatabase.GetAssetPath).Where(path => !string.IsNullOrEmpty(path)).ToArray();
    }

    // The same whatever the line endings of the checkout
    static string ReadContent(string path)
    {
        return path + "\n" + File.ReadAllText(path).Replace("\r\n", "\n");
    }

    // The same for several waves at once: the measure setup and the data shared by the waves are read only once
    public static Dictionary<WavePatternData, string> ComputeAll(IEnumerable<WavePatternData> waves, IEnumerable<Object> measureSetup)
    {
        Dictionary<string, string[]> directDependencies = new Dictionary<string, string[]>();
        string[] setupPaths = measureSetup.Where(setup => setup != null).Select(AssetDatabase.GetAssetPath).Where(path => !string.IsNullOrEmpty(path)).ToArray();
        HashSet<string> setupDependencies = CollectDataDependencies(setupPaths, directDependencies);
        Dictionary<string, string> readFiles = new Dictionary<string, string>();

        Dictionary<WavePatternData, string> fingerprints = new Dictionary<WavePatternData, string>();
        foreach (WavePatternData wave in waves)
        {
            string wavePath = AssetDatabase.GetAssetPath(wave);
            HashSet<string> dependencies = new HashSet<string>(setupDependencies);
            if (!string.IsNullOrEmpty(wavePath))
            {
                dependencies.UnionWith(CollectDataDependencies(new[] { wavePath }, directDependencies));
            }

            List<string> contents = new List<string> { GetLayout(wave) };
            foreach (string path in dependencies.Where(path => path != wavePath).OrderBy(path => path, System.StringComparer.Ordinal))
            {
                if (!readFiles.TryGetValue(path, out string content))
                {
                    content = ReadContent(path);
                    readFiles[path] = content;
                }
                contents.Add(content);
            }
            fingerprints[wave] = WaveScore.ComputeFingerprint(contents);
        }
        return fingerprints;
    }

    // The data assets reached from the roots through data assets only, the roots included, the excluded ones
    // neither counted nor followed. The direct dependencies of each file are cached for the next calls
    public static HashSet<string> CollectDataDependencies(IEnumerable<string> roots, Dictionary<string, string[]> directDependencies = null, ICollection<string> excluded = null)
    {
        directDependencies ??= new Dictionary<string, string[]>();
        HashSet<string> found = new HashSet<string>();
        Stack<string> toVisit = new Stack<string>(roots.Where(CountsInFingerprint));
        if (excluded != null)
        {
            // Never visited: neither hashed nor followed
            found.UnionWith(excluded);
        }
        while (toVisit.Count > 0)
        {
            string path = toVisit.Pop();
            if (!found.Add(path))
            {
                continue;
            }

            if (!directDependencies.TryGetValue(path, out string[] direct))
            {
                direct = AssetDatabase.GetDependencies(path, false);
                directDependencies[path] = direct;
            }
            foreach (string dependency in direct)
            {
                if (!found.Contains(dependency) && CountsInFingerprint(dependency))
                {
                    toVisit.Push(dependency);
                }
            }
        }
        if (excluded != null)
        {
            found.ExceptWith(excluded);
        }
        return found;
    }

    // The data assets only: not the TextMesh Pro fonts either, should a data reference one, whose dynamic atlas is
    // rewritten whenever new characters are shown in play mode, which would make every score out of date
    public static bool CountsInFingerprint(string path)
    {
        if (Path.GetExtension(path) != ".asset")
        {
            return false;
        }
        System.Type type = AssetDatabase.GetMainAssetTypeAtPath(path);
        return type == null || type.Namespace != "TMPro";
    }

    // Size and unit of each slot: the wave file also holds its score, so only its layout counts
    public static string GetLayout(WavePatternData wave)
    {
        StringBuilder layout = new StringBuilder($"{wave.width}x{wave.height}");
        if (wave.slots == null)
        {
            return layout.ToString();
        }

        for (int i = 0; i < wave.slots.GetLength(0); i++)
        {
            for (int j = 0; j < wave.slots.GetLength(1); j++)
            {
                EntityData entity = wave.slots[i, j].entity;
                if (entity != null)
                {
                    string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(entity));
                    layout.Append($" {i},{j}:{(string.IsNullOrEmpty(guid) ? entity.name : guid)}");
                }
            }
        }
        return layout.ToString();
    }
}
#endif
