using System.Collections.Generic;
using Oisif.Editor;
using UnityEditor;
using UnityEngine;

// The data of the game by kind (characters, skills, units, items, game data): each one listed, edited and created
// in one window, the units and items filtered by tags
public class DataEditor : AssetBrowserWindow
{
    static readonly string[] Tabs = { "Characters", "CharacterSkills", "Entities", "Items", "GameData" };

    // Filter of the items: tags added from a dropdown, each one needed or excluded (right click to switch)
    enum TagFilterState
    {
        Included,
        Excluded,
    }

    // Tabs whose data have tags, the tag filter shown above them
    static readonly HashSet<string> TaggableTabs = new HashSet<string> { "Entities", "Items" };

    const int ChipsPerRow = 4;
    static readonly Color IncludedColor = new Color(0.45f, 0.85f, 0.45f);
    static readonly Color ExcludedColor = new Color(0.95f, 0.45f, 0.45f);

    readonly List<GameplayTag> _tags = new List<GameplayTag>();
    // Reloaded when the project changes, so a tag created while the window is open shows up
    bool _areTagsLoaded = false;
    // In the order they were added; lists so the window keeps them through a recompilation
    [SerializeField] List<GameplayTag> _filterTags = new List<GameplayTag>();
    // The state of each tag of the filter, same index
    [SerializeField] List<TagFilterState> _filterStates = new List<TagFilterState>();
    string _filteredTab;

    [MenuItem("Tools/Data Editor")]
    static void OpenEditor() => GetWindow<DataEditor>("Data Editor");

    protected override string[] tabs => Tabs;

    protected override void OnEnable()
    {
        base.OnEnable();
        EditorApplication.projectChanged += OnProjectChanged;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        EditorApplication.projectChanged -= OnProjectChanged;
    }

    protected override List<AssetSection> GetSections(string tab)
    {
        List<AssetSection> sections = new List<AssetSection>();
        switch (tab)
        {
            case "Characters":
                sections.Add(new AssetSection("Characters", "Assets/Data/Characters", typeof(CharacterData)) { getAssetName = data => ((CharacterData)data).title + "Character" });
                break;
            case "CharacterSkills":
                sections.Add(new AssetSection("Character Skills", "Assets/Data/CharacterSkills", typeof(ACharacterSkillFactory)) { pickDerivedType = true });
                break;
            case "Entities":
                sections.Add(new AssetSection("Entities", "Assets/Data/Entities", typeof(EntityData)) { getAssetName = data => ((EntityData)data).title + "Entity" });
                break;
            case "Items":
                sections.Add(new AssetSection("Entity Items", "Assets/Data/EntityItems", typeof(ItemFactory)) { getAssetName = GetItemName });
                sections.Add(new AssetSection("Player Items", "Assets/Data/PlayerItems", typeof(ItemFactory)) { getAssetName = GetItemName });
                // Player items only offered by events (e.g. the Library, add Cursed for the Dark Library)
                sections.Add(new AssetSection("Event Items", "Assets/Data/EventItems", typeof(ItemFactory))
                {
                    getAssetName = GetItemName,
                    initDraft = data => ((ItemFactory)data).data = new ItemData { tags = new List<GameplayTag> { LoadTag(TagNames.Player), LoadTag(TagNames.Library) } },
                });
                break;
            case "GameData":
                sections.Add(new AssetSection("", "Assets", typeof(GameData)) { canCreate = false, createFolder = false });
                break;
        }

        if (TaggableTabs.Contains(tab))
        {
            List<GameplayTag> includedTags = GetTagsInState(TagFilterState.Included);
            List<GameplayTag> excludedTags = GetTagsInState(TagFilterState.Excluded);
            if (includedTags.Count > 0 || excludedTags.Count > 0)
            {
                foreach (AssetSection section in sections)
                {
                    section.filter = data => TagFilter.Matches(data as ITaggable, includedTags, excludedTags);
                }
            }
        }
        return sections;
    }

    static string GetItemName(ScriptableObject data) => ((ItemFactory)data).title + "Item";

    protected override void DrawHeader(string tab)
    {
        // Each tab has its own tags (e.g. Player for the items, Druid for the units)
        if (_filteredTab != tab)
        {
            _filteredTab = tab;
            if (_filterTags.Count > 0)
            {
                _filterTags.Clear();
                _filterStates.Clear();
                RefreshList();
            }
        }
        if (TaggableTabs.Contains(tab) && DrawTagFilter())
        {
            RefreshList();
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
        KeepFilterConsistent();
        List<GameplayTag> addable = _tags.FindAll(tag => !_filterTags.Contains(tag));
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
            _filterTags.Add(tag);
            _filterStates.Add(TagFilterState.Included);
            changed = true;
        }

        // One chip per tag of the filter: "+ Player" needed, "- Cursed" excluded, x to remove it
        GameplayTag removed = null;
        Color defaultColor = GUI.backgroundColor;
        for (int start = 0; start < _filterTags.Count; start += ChipsPerRow)
        {
            Rect rowRect = GUILayoutUtility.GetRect(0, 20);
            float chipWidth = rowRect.width / ChipsPerRow;
            for (int i = 0; i < ChipsPerRow && start + i < _filterTags.Count; i++)
            {
                int index = start + i;
                GameplayTag tag = _filterTags[index];
                TagFilterState state = _filterStates[index];
                Rect chipRect = new Rect(rowRect.x + i * chipWidth, rowRect.y, chipWidth, rowRect.height);
                Rect removeRect = new Rect(chipRect.xMax - 20f, chipRect.y, 20f, chipRect.height);
                Rect labelRect = new Rect(chipRect.x, chipRect.y, chipRect.width - 20f, chipRect.height);

                // Right click switches between needed and excluded
                Event current = Event.current;
                if (current.type == EventType.MouseDown && current.button == 1 && labelRect.Contains(current.mousePosition))
                {
                    _filterStates[index] = state == TagFilterState.Included ? TagFilterState.Excluded : TagFilterState.Included;
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
            int index = _filterTags.IndexOf(removed);
            _filterTags.RemoveAt(index);
            _filterStates.RemoveAt(index);
            changed = true;
        }
        return changed;
    }

    void OnProjectChanged()
    {
        _areTagsLoaded = false;
        KeepFilterConsistent();
        Repaint();
    }

    // A deleted tag leaves the filter, and every tag has a state (needed by default)
    void KeepFilterConsistent()
    {
        while (_filterStates.Count < _filterTags.Count)
        {
            _filterStates.Add(TagFilterState.Included);
        }
        if (_filterStates.Count > _filterTags.Count)
        {
            _filterStates.RemoveRange(_filterTags.Count, _filterStates.Count - _filterTags.Count);
        }
        for (int i = _filterTags.Count - 1; i >= 0; i--)
        {
            if (_filterTags[i] == null)
            {
                _filterTags.RemoveAt(i);
                _filterStates.RemoveAt(i);
            }
        }
    }

    List<GameplayTag> GetTagsInState(TagFilterState state)
    {
        KeepFilterConsistent();
        List<GameplayTag> tags = new List<GameplayTag>();
        for (int i = 0; i < _filterTags.Count; i++)
        {
            if (_filterStates[i] == state)
            {
                tags.Add(_filterTags[i]);
            }
        }
        return tags;
    }

    static GameplayTag LoadTag(string tagName)
    {
        return AssetDatabase.LoadAssetAtPath<GameplayTag>($"Assets/Prefabs/Tags/{tagName}.asset");
    }
}
