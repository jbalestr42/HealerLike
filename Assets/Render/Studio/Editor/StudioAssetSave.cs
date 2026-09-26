using System.IO;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Studio.Editor
{
    // Takes ownership of an unsaved copy. The AssetDatabase owns it only after CreateAsset succeeds.
    public static class StudioAssetSave
    {
        public static AssetType Copy<AssetType>(AssetType source, string path) where AssetType : Object
        {
            AssetType copy = Object.Instantiate(source);
            copy.hideFlags = HideFlags.None;
            copy.name = Path.GetFileNameWithoutExtension(path);
            return Write(copy, path);
        }

        public static AssetType Write<AssetType>(AssetType copy, string path) where AssetType : Object
        {
            try
            {
                AssetDatabase.CreateAsset(copy, path);
                if (!AssetDatabase.Contains(copy))
                {
                    Debug.LogError("[StudioAssetSave] Could not create asset at " + path);
                    return null;
                }

                AssetDatabase.SaveAssetIfDirty(copy);
                return copy;
            }
            finally
            {
                if (copy != null && !AssetDatabase.Contains(copy))
                {
                    Object.DestroyImmediate(copy);
                }
            }
        }
    }
}
