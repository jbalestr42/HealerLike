using System.IO;
using UnityEditor;
using UnityEngine;

// Paints the icons of IconCatalog to PNG sprites under Assets/Icons and sets each one on its data, so they are
// redrawn from code after a change to a glyph or a colour
public static class IconPainter
{
    const int Size = 256;

    [MenuItem("Tools/Paint Icons")]
    public static void PaintAll()
    {
        foreach (IconCatalog.Entry entry in IconCatalog.entries)
        {
            WritePng(entry);
            ImportAsSprite(entry.iconAssetPath);
            SetIcon(entry);
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"Painted {IconCatalog.entries.Count} icons");
    }

    static void WritePng(IconCatalog.Entry entry)
    {
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        texture.LoadRawTextureData(IconCatalog.Paint(entry, Size).ToRgba32(true));
        texture.Apply();
        string path = entry.iconAssetPath;
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
    }

    static void ImportAsSprite(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single
            && !importer.mipmapEnabled && importer.alphaIsTransparency)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }

    // On the data's first field named as the entry's (a status: on the buff putting it there, a prefab: on its components)
    static void SetIcon(IconCatalog.Entry entry)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(entry.iconAssetPath);
        if (AssetDatabase.LoadMainAssetAtPath(entry.assetPath) is GameObject prefab)
        {
            foreach (Component component in prefab.GetComponentsInChildren<Component>(true))
            {
                if (SetField(component, entry.field, sprite))
                {
                    PrefabUtility.SavePrefabAsset(prefab);
                    return;
                }
            }
            Debug.LogError($"No {entry.field} field on {entry.assetPath}");
            return;
        }

        Object data = AssetDatabase.LoadMainAssetAtPath(entry.assetPath);
        if (data is StatusData status)
        {
            data = status.buffHandlerFactory;
        }
        if (data == null)
        {
            Debug.LogError($"No data at {entry.assetPath} for its icon");
            return;
        }

        if (!SetField(data, entry.field, sprite))
        {
            Debug.LogError($"No {entry.field} field on {entry.assetPath}");
        }
    }

    static bool SetField(Object data, string field, Sprite sprite)
    {
        var serialized = new SerializedObject(data);
        SerializedProperty property = serialized.GetIterator();
        while (property.Next(true))
        {
            if (property.name == field && property.propertyType == SerializedPropertyType.ObjectReference)
            {
                property.objectReferenceValue = sprite;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                return true;
            }
        }
        return false;
    }
}
