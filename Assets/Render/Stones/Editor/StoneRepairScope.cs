using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    public static class StoneRepairScope
    {
        // The repair is pinned to the twelve saved variants already inspected, independent of future bakes.
        public static readonly int AssetCount = 12;

        public static string AssetPath(int index)
        {
            return "Assets/Render/Stones/Meshes/StoneVariant" + index.ToString("00") + ".asset";
        }

        public static StoneRepairManifest Inspect()
        {
            StoneRepairManifest report = new StoneRepairManifest();
            report.project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            report.unityVersion = Application.unityVersion;
            report.sourceRevision = System.Environment.GetEnvironmentVariable("RENDER_CAPTURE_REVISION");
            report.createdUtc = DateTime.UtcNow.ToString("O");
            report.status = "inspected";
            StoneVariants variants = AssetDatabase.LoadAssetAtPath<StoneVariants>(StoneVariantBaker.VariantsPath);
            if (variants == null || variants.meshes == null || variants.meshes.Length != StoneRepairScope.AssetCount)
            {
                report.errors.Add("The saved StoneVariants catalog must contain exactly twelve meshes.");
                return report;
            }

            for (int i = 0; i < StoneRepairScope.AssetCount; i++)
            {
                string path = AssetPath(i);
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                StoneRepairEntry entry = new StoneRepairEntry();
                entry.path = path;
                report.entries.Add(entry);
                string file = Path.Combine(report.project, path);
                if (mesh == null || variants.meshes[i] != mesh || !AssetDatabase.IsMainAsset(mesh)
                    || AssetDatabase.LoadAllAssetsAtPath(path).Length != 1 || EditorUtility.IsDirty(mesh)
                    || !File.Exists(file) || !File.Exists(file + ".meta"))
                {
                    entry.state = StoneOutlineState.Refused.ToString();
                    entry.reason = "Expected the catalog's clean, standalone saved mesh and its existing meta.";
                    report.errors.Add(path + ": " + entry.reason);
                    continue;
                }

                entry.guid = AssetDatabase.AssetPathToGUID(path);
                entry.assetHash = StoneMeshFingerprint.FileHash(file);
                entry.metaHash = StoneMeshFingerprint.FileHash(file + ".meta");
                entry.subMeshCount = mesh.subMeshCount;
                StoneOutlineState state = StoneOutlineRepair.Inspect(mesh, out entry.reason);
                entry.state = state.ToString();
                if (state == StoneOutlineState.Refused)
                {
                    report.errors.Add(path + ": " + entry.reason);
                    continue;
                }

                entry.authoredHash = StoneMeshFingerprint.Authored(mesh);
            }

            return report;
        }

        public static bool Matches(StoneRepairManifest approved, StoneRepairManifest current, out string reason)
        {
            reason = "Inspection is invalid or belongs to another project.";
            if (approved == null || approved.version != 1 || approved.project != current.project
                || approved.unityVersion != current.unityVersion
                || approved.status != "inspected"
                || approved.errors == null || approved.errors.Count != 0 || current.errors.Count != 0
                || approved.entries == null || approved.entries.Count != StoneRepairScope.AssetCount
                || current.entries.Count != StoneRepairScope.AssetCount)
            {
                return false;
            }

            for (int i = 0; i < current.entries.Count; i++)
            {
                StoneRepairEntry before = approved.entries[i];
                StoneRepairEntry now = current.entries[i];
                if (before == null || before.path != AssetPath(i) || before.path != now.path
                    || before.guid != now.guid || before.assetHash != now.assetHash || before.metaHash != now.metaHash
                    || before.authoredHash != now.authoredHash || before.state != now.state)
                {
                    reason = "Saved mesh changed since inspection: " + AssetPath(i);
                    return false;
                }
            }

            reason = null;
            return true;
        }
    }
}
