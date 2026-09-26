using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    public static class StoneRepairTransaction
    {
        public static void Apply(StoneRepairManifest report, string folder)
        {
            StoneRepairBackup backup = new StoneRepairBackup(report, Path.Combine(folder, "backup"));
            backup.Create();
            List<int> changed = new List<int>();
            try
            {
                for (int i = 0; i < report.entries.Count; i++)
                {
                    StoneRepairEntry entry = report.entries[i];
                    if (entry.state != StoneOutlineState.Duplicate.ToString())
                    {
                        continue;
                    }

                    Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(entry.path);
                    string source = Path.Combine(report.project, entry.path);
                    if (StoneMeshFingerprint.FileHash(source) != entry.assetHash || EditorUtility.IsDirty(mesh)
                        || StoneMeshFingerprint.Authored(mesh) != entry.authoredHash)
                    {
                        throw new IOException("Saved mesh changed during repair: " + entry.path);
                    }

                    changed.Add(i);
                    if (!StoneOutlineRepair.TryRepair(mesh, out string reason))
                    {
                        throw new InvalidOperationException(entry.path + ": " + reason);
                    }

                    EditorUtility.SetDirty(mesh);
                    AssetDatabase.SaveAssetIfDirty(mesh);
                    AssetDatabase.ImportAsset(entry.path, ImportAssetOptions.ForceSynchronousImport
                        | ImportAssetOptions.ForceUpdate);
                    Verify(report, entry);
                    report.repaired++;
                }

                foreach (StoneRepairEntry entry in report.entries)
                {
                    Verify(report, entry);
                    string path = Path.Combine(report.project, entry.path);
                    entry.afterAssetHash = StoneMeshFingerprint.FileHash(path);
                    if (entry.state == StoneOutlineState.Clean.ToString() && entry.afterAssetHash != entry.assetHash)
                    {
                        throw new IOException("An already-clean asset changed: " + entry.path);
                    }
                }

                report.status = "applied";
                StoneRepairReport.Write(Path.Combine(folder, "result.json"), report);
            }
            catch (Exception failure)
            {
                try
                {
                    backup.Restore(changed);
                    foreach (int index in changed)
                    {
                        AssetDatabase.ImportAsset(report.entries[index].path, ImportAssetOptions.ForceSynchronousImport
                            | ImportAssetOptions.ForceUpdate);
                    }

                    backup.VerifyRestored(changed);
                    report.rolledBack = true;
                    report.repaired = 0;
                    report.status = "rolled-back";
                }
                catch (Exception rollback)
                {
                    report.status = "rollback-failed";
                    throw new AggregateException("Stone repair and rollback both failed.", failure, rollback);
                }

                throw;
            }
        }

        static void Verify(StoneRepairManifest report, StoneRepairEntry entry)
        {
            string path = Path.Combine(report.project, entry.path);
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(entry.path);
            if (StoneOutlineRepair.Inspect(mesh, out string reason) != StoneOutlineState.Clean)
            {
                throw new InvalidOperationException(entry.path + ": " + reason);
            }

            entry.afterAuthoredHash = StoneMeshFingerprint.Authored(mesh);
            if (entry.afterAuthoredHash != entry.authoredHash || AssetDatabase.AssetPathToGUID(entry.path) != entry.guid
                || StoneMeshFingerprint.FileHash(path + ".meta") != entry.metaHash)
            {
                throw new IOException("Authored mesh data or GUID changed: " + entry.path);
            }
        }
    }
}
