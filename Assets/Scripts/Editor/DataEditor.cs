using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

public class DataEditor : OdinMenuEditorWindow
{
    private static string[] typesToDisplay = { "Characters", "CharacterSkills", "Entities", "Items", "Consumables", "GameData" };
    private string _selectedType = "Items";

    Dictionary<string, List<ABaseDataEditor>> _dataEditors = new Dictionary<string, List<ABaseDataEditor>>();

    [MenuItem("Tools/Data Editor")]
    private static void OpenEditor() => GetWindow<DataEditor>();

    protected override void OnDestroy()
    {
        base.OnDestroy();

        foreach (var kvp in _dataEditors)
        {
            foreach (ABaseDataEditor dataEditor in kvp.Value)
            {
                DestroyImmediate(dataEditor.data);
            }
        }
    }

    protected override void OnGUI()
    {
        if (GUIUtils.SelectButtonList(ref _selectedType, typesToDisplay))
        {
            ForceMenuTreeRebuild();
        }

        base.OnGUI();
    }

    protected override void OnBeginDrawEditors()
    {
        if (this.MenuTree != null)
        {
            OdinMenuTreeSelection selected = this.MenuTree.Selection;

            SirenixEditorGUI.BeginHorizontalToolbar();
            {
                GUILayout.FlexibleSpace();

                ScriptableObject asset = selected.SelectedValue as ScriptableObject;
                if (asset != null)
                {
                    if (SirenixEditorGUI.ToolbarButton("Select"))
                    {
                        EditorUtility.FocusProjectWindow();
                        Selection.activeObject = asset;
                        EditorGUIUtility.PingObject(asset);
                    }

                    if (SirenixEditorGUI.ToolbarButton("Delete"))
                    {
                        string path = AssetDatabase.GetAssetPath(asset);
                        AssetDatabase.DeleteAsset(path);
                        AssetDatabase.SaveAssets();
                    }
                }
            }
            SirenixEditorGUI.EndHorizontalToolbar();
        }
    }

    protected override OdinMenuTree BuildMenuTree()
    {
        var tree = new OdinMenuTree();

        foreach (string type in typesToDisplay)
        {
            _dataEditors[type] = new List<ABaseDataEditor>();
        }

        _dataEditors["Characters"].Add(new BaseDataEditor<CharacterData>("Characters", "Assets/Data/Characters/") { getDataName = (CharacterData data) => data.title + "Character" });
        _dataEditors["CharacterSkills"].Add(new DerivedTypeDataEditor<ACharacterSkillFactory>("CharacterSkills", "Assets/Data/CharacterSkills/"));
        _dataEditors["Entities"].Add(new BaseDataEditor<EnemyData>("Enemies", "Assets/Data/Enemies/") { getDataName = (EnemyData data) => data.title + "Enemy" });
        _dataEditors["Entities"].Add(new BaseDataEditor<TowerData>("Towers", "Assets/Data/Towers/") { getDataName = (TowerData data) => data.title + "Tower" });
        _dataEditors["Entities"].Add(new BaseDataEditor<EntityData>("Entities", "Assets/Data/Entities/") { getDataName = (EntityData data) => data.title + "Entity" });
        _dataEditors["Items"].Add(new BaseDataEditor<ItemFactory>("Items", "Assets/Data/Items/") { getDataName = (ItemFactory item) => item.title + "Item" });
        _dataEditors["Consumables"].Add(new DerivedTypeDataEditor<AConsumableFactory>("Consumables", "Assets/Data/Consumables/"));
        _dataEditors["GameData"].Add(new BaseDataEditor<GameData>("", "Assets/") { createFolder = false, canCreate = false });

        foreach (ABaseDataEditor dataEditor in _dataEditors[_selectedType])
        {
            dataEditor.AddTree(tree);
        }
        return tree;
    }
}
