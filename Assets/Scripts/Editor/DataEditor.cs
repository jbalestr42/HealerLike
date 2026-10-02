using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

public class DataEditor : OdinMenuEditorWindow
{
    private static string[] typesToDisplay = { "Characters", "CharacterSkills", "Entities", "Items", "GameData" };
    private string _selectedType = "Items";

    Dictionary<string, List<ABaseDataEditor>> _dataEditors = new Dictionary<string, List<ABaseDataEditor>>();

    // Filter of the items: tags added from a dropdown, each one needed or excluded (right click to switch)
    enum TagFilterState
    {
        Included,
        Excluded,
    }

    // Tabs whose data have tags, the tag filter shown above them
    static readonly HashSet<string> TaggableTypes = new HashSet<string> { "Entities", "Items" };

    const int ChipsPerRow = 4;
    static readonly Color IncludedColor = new Color(0.45f, 0.85f, 0.45f);
    static readonly Color ExcludedColor = new Color(0.95f, 0.45f, 0.45f);

    List<GameplayTag> _tags = new List<GameplayTag>();
    // Reloaded when the project changes, so a tag created while the window is open shows up
    bool _areTagsLoaded = false;
    // In the order they were added
    List<GameplayTag> _filterTags = new List<GameplayTag>();
    Dictionary<GameplayTag, TagFilterState> _tagFilter = new Dictionary<GameplayTag, TagFilterState>();

    [MenuItem("Tools/Data Editor")]
    private static void OpenEditor() => GetWindow<DataEditor>();

    protected override void OnEnable()
    {
        base.OnEnable();
        EditorApplication.projectChanged += OnProjectChanged;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        EditorApplication.projectChanged -= OnProjectChanged;

        foreach (var kvp in _dataEditors)
        {
            foreach (ABaseDataEditor dataEditor in kvp.Value)
            {
                DestroyImmediate(dataEditor.data);
            }
        }
    }

    protected override void OnImGUI()
    {
        if (GUIUtils.SelectButtonList(ref _selectedType, typesToDisplay))
        {
            // Each tab has its own tags (e.g. Player for the items, Druid for the units)
            _tagFilter.Clear();
            _filterTags.Clear();
            ForceMenuTreeRebuild();
        }

        if (TaggableTypes.Contains(_selectedType) && DrawTagFilter())
        {
            ForceMenuTreeRebuild();
        }

        base.OnImGUI();
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

    // True when the filter changed
    bool DrawTagFilter()
    {
        if (!_areTagsLoaded)
        {
            _areTagsLoaded = true;
            _tags.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:GameplayTag"))
            {
                GameplayTag tag = AssetDatabase.LoadAssetAtPath<GameplayTag>(AssetDatabase.GUIDToAssetPath(guid));
                if (tag != null)
                {
                    _tags.Add(tag);
                }
            }
            _tags.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
        }

        bool changed = false;

        // The tags not in the filter yet, after a placeholder
        List<GameplayTag> addable = _tags.FindAll(tag => !_tagFilter.ContainsKey(tag));
        string[] options = new string[addable.Count + 1];
        options[0] = "Filter by tag...";
        for (int i = 0; i < addable.Count; i++)
        {
            options[i + 1] = addable[i].name;
        }
        int picked = EditorGUILayout.Popup(0, options);
        if (picked > 0)
        {
            GameplayTag tag = addable[picked - 1];
            _tagFilter[tag] = TagFilterState.Included;
            _filterTags.Add(tag);
            changed = true;
        }

        // One chip per tag of the filter: "+ Player" needed, "- Cursed" excluded, x to remove it
        GameplayTag removed = null;
        Color defaultColor = GUI.backgroundColor;
        for (int start = 0; start < _filterTags.Count; start += ChipsPerRow)
        {
            Rect rowRect = GUILayoutUtility.GetRect(0, 20);
            for (int i = 0; i < ChipsPerRow && start + i < _filterTags.Count; i++)
            {
                GameplayTag tag = _filterTags[start + i];
                TagFilterState state = _tagFilter[tag];
                Rect chipRect = rowRect.Split(i, ChipsPerRow);
                Rect removeRect = new Rect(chipRect.xMax - 20f, chipRect.y, 20f, chipRect.height);
                Rect labelRect = new Rect(chipRect.x, chipRect.y, chipRect.width - 20f, chipRect.height);

                // Right click switches between needed and excluded
                Event current = Event.current;
                if (current.type == EventType.MouseDown && current.button == 1 && labelRect.Contains(current.mousePosition))
                {
                    _tagFilter[tag] = state == TagFilterState.Included ? TagFilterState.Excluded : TagFilterState.Included;
                    changed = true;
                    current.Use();
                }

                GUI.backgroundColor = state == TagFilterState.Included ? IncludedColor : ExcludedColor;
                string label = state == TagFilterState.Included ? $"+ {tag.name}" : $"- {tag.name}";
                GUI.Label(labelRect, new GUIContent(label, "Right click: needed / excluded"), EditorStyles.miniButtonLeft);
                if (GUI.Button(removeRect, new GUIContent("x", "Remove from the filter"), EditorStyles.miniButtonRight))
                {
                    removed = tag;
                }
            }
        }
        GUI.backgroundColor = defaultColor;

        if (removed != null)
        {
            _tagFilter.Remove(removed);
            _filterTags.Remove(removed);
            changed = true;
        }
        return changed;
    }

    void OnProjectChanged()
    {
        _areTagsLoaded = false;
        // A tag of the filter may have been deleted
        _filterTags.RemoveAll(tag => tag == null);
        List<GameplayTag> deleted = new List<GameplayTag>();
        foreach (GameplayTag tag in _tagFilter.Keys)
        {
            if (tag == null)
            {
                deleted.Add(tag);
            }
        }
        foreach (GameplayTag tag in deleted)
        {
            _tagFilter.Remove(tag);
        }
        Repaint();
    }

    List<GameplayTag> GetTagsInState(TagFilterState state)
    {
        List<GameplayTag> tags = new List<GameplayTag>();
        foreach (KeyValuePair<GameplayTag, TagFilterState> kvp in _tagFilter)
        {
            if (kvp.Value == state)
            {
                tags.Add(kvp.Key);
            }
        }
        return tags;
    }

    static GameplayTag LoadTag(string tagName)
    {
        return AssetDatabase.LoadAssetAtPath<GameplayTag>($"Assets/Prefabs/Tags/{tagName}.asset");
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
        _dataEditors["Entities"].Add(new BaseDataEditor<EntityData>("Entities", "Assets/Data/Entities/") { getDataName = (EntityData data) => data.title + "Entity" });
        _dataEditors["Items"].Add(new BaseDataEditor<ItemFactory>("Entity Items", "Assets/Data/EntityItems/") { getDataName = (ItemFactory item) => item.title + "Item" });
        _dataEditors["Items"].Add(new BaseDataEditor<ItemFactory>("Player Items", "Assets/Data/PlayerItems/") { getDataName = (ItemFactory item) => item.title + "Item" });
        // Player items only offered by events (e.g. the Library, add Cursed for the Dark Library)
        _dataEditors["Items"].Add(new BaseDataEditor<ItemFactory>("Event Items", "Assets/Data/EventItems/")
        {
            getDataName = (ItemFactory item) => item.title + "Item",
            initData = (ItemFactory item) => item.data = new ItemData { tags = new List<GameplayTag> { LoadTag(TagNames.Player), LoadTag(TagNames.Library) } },
        });
        _dataEditors["GameData"].Add(new BaseDataEditor<GameData>("", "Assets/") { createFolder = false, canCreate = false });

        List<GameplayTag> includedTags = GetTagsInState(TagFilterState.Included);
        List<GameplayTag> excludedTags = GetTagsInState(TagFilterState.Excluded);
        if (includedTags.Count > 0 || excludedTags.Count > 0)
        {
            foreach (ABaseDataEditor dataEditor in _dataEditors[_selectedType])
            {
                dataEditor.SetTagFilter(includedTags, excludedTags);
            }
        }

        foreach (ABaseDataEditor dataEditor in _dataEditors[_selectedType])
        {
            dataEditor.AddTree(tree);
        }
        return tree;
    }
}
