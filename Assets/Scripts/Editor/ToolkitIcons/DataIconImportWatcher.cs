using System;
using UnityEditor;

// Schedules an icon bake when a data asset is imported, deleted or moved
public class DataIconImportWatcher : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        if (HasDataAsset(imported) || HasDataAsset(deleted) || HasDataAsset(moved) || HasDataAsset(movedFrom))
        {
            DataIconBaker.Schedule();
        }
    }

    static bool HasDataAsset(string[] paths)
    {
        foreach (string path in paths)
        {
            if (path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith(DataIconBaker.CatalogFolder, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
