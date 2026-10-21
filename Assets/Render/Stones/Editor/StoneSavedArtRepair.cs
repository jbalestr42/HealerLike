using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    // Batch acceptance entrypoints. Inspection and application share one explicit, external evidence folder.
    public static class StoneSavedArtRepair
    {
        public static void Inspect()
        {
            Run(false);
        }

        public static void Apply()
        {
            Run(true);
        }

        static void Run(bool apply)
        {
            StoneRepairManifest report = null;
            string output = null;
            bool passed = false;
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    throw new InvalidOperationException("Stone repair requires Edit mode.");
                }

                string folder = EvidenceFolder();
                output = Path.Combine(folder, apply ? "result.json" : "inspection.json");
                if (File.Exists(output))
                {
                    throw new IOException("Evidence already exists; use a fresh inspection folder: " + output);
                }

                report = StoneRepairScope.Inspect();
                if (report.errors.Count != 0)
                {
                    throw new InvalidOperationException("Stone scope contains refused assets; see report errors.");
                }

                if (apply)
                {
                    string inspected = File.ReadAllText(Path.Combine(folder, "inspection.json"));
                    StoneRepairManifest approved = JsonUtility.FromJson<StoneRepairManifest>(inspected);
                    if (!StoneRepairScope.Matches(approved, report, out string reason))
                    {
                        throw new InvalidOperationException(reason);
                    }

                    StoneRepairTransaction.Apply(report, folder);
                }

                passed = true;
            }
            catch (Exception error)
            {
                if (report == null)
                {
                    report = new StoneRepairManifest();
                }

                if (report.status != "rolled-back" && report.status != "rollback-failed")
                {
                    report.status = "refused";
                }
                report.errors.Add(error.ToString());
                Debug.LogError("[StoneSavedArtRepair] " + error.Message);
            }
            finally
            {
                if (output != null && !File.Exists(output))
                {
                    StoneRepairReport.Write(output, report);
                }
            }

            Debug.Log("[StoneSavedArtRepair] " + report.status + "; repaired " + report.repaired + " assets.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(passed ? 0 : 1);
            }
        }

        static string EvidenceFolder()
        {
            string folder = System.Environment.GetEnvironmentVariable("RENDER_STONE_REPAIR_DIR");
            if (string.IsNullOrEmpty(folder) || !Path.IsPathRooted(folder))
            {
                throw new InvalidOperationException("Set RENDER_STONE_REPAIR_DIR to an absolute evidence folder.");
            }

            folder = Path.GetFullPath(folder);
            string assets = Path.GetFullPath(Application.dataPath).TrimEnd(Path.DirectorySeparatorChar);
            if (folder == assets || folder.StartsWith(assets + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Stone repair evidence must be outside Assets.");
            }

            Directory.CreateDirectory(folder);
            return folder;
        }
    }
}
