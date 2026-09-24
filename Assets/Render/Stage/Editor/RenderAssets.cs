using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // Loads and wires the layer's assets for the authoring and capture tools, logging what is missing
    public static class RenderAssets
    {
        public static AssetType Load<AssetType>(string path) where AssetType : Object
        {
            AssetType asset = AssetDatabase.LoadAssetAtPath<AssetType>(path);
            if (asset == null)
            {
                Debug.LogError($"[RenderAssets] Missing {path}.");
            }
            return asset;
        }

        public static void SetReference(Object target, string field, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"[RenderAssets] {target.GetType().Name} has no field {field}.");
                return;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
