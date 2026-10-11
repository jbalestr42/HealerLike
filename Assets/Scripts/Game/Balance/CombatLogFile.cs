using System.IO;
using UnityEngine;

// The combat log: one JSON line per fight, appended to Logs/Balance/combats.jsonl (next to the project in the editor)
public static class CombatLogFile
{
    public const string FileName = "combats.jsonl";

    public static string defaultPath
    {
        get
        {
            string root = Application.isEditor ? Path.Combine(Path.GetDirectoryName(Application.dataPath), "Logs") : Application.persistentDataPath;
            return Path.Combine(root, "Balance", FileName);
        }
    }

    public static void Append(string path, CombatStats stats)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.AppendAllText(path, JsonUtility.ToJson(stats) + "\n");
    }
}
