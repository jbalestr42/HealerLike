using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Oisif.Editor
{
    // A window listing the assets of some sections per tab, with a search, and showing the inspector of the one
    // picked, or the draft of a new asset of a section. A project derives from it to give its tabs and sections
    public abstract class AssetBrowserWindow : EditorWindow
    {
        const float ListWidth = 260f;

        // Tabs, the selected one kept through a recompilation
        [SerializeField] int _tab;
        [SerializeField] string _search = "";
        Vector2 _listScroll;
        Vector2 _inspectorScroll;

        // Not kept through a recompilation (Unity would keep the private fields of a window): built again
        [NonSerialized] List<AssetSection> _sections;
        // The assets of each section, built again when the project, the tab or a filter changes
        [NonSerialized] readonly Dictionary<AssetSection, List<ScriptableObject>> _assets = new Dictionary<AssetSection, List<ScriptableObject>>();
        [NonSerialized] bool _isListDirty = true;

        [SerializeField] ScriptableObject _selected;
        [NonSerialized] AssetSection _creatingIn;
        [NonSerialized] ScriptableObject _draft;
        [NonSerialized] string _draftName = "";
        [NonSerialized] UnityEditor.Editor _editor;

        protected abstract string[] tabs { get; }
        // The sections of the tab, asked again whenever the list is rebuilt (e.g. after a filter change)
        protected abstract List<AssetSection> GetSections(string tab);
        // Drawn above the list (e.g. filters); call RefreshList when it changes what is listed
        protected virtual void DrawHeader(string tab) {}

        protected string currentTab => tabs.Length > 0 ? tabs[Mathf.Clamp(_tab, 0, tabs.Length - 1)] : "";
        public ScriptableObject selected => _selected;

        public void RefreshList()
        {
            _isListDirty = true;
            Repaint();
        }

        protected virtual void OnEnable()
        {
            EditorApplication.projectChanged += RefreshList;
        }

        protected virtual void OnDisable()
        {
            EditorApplication.projectChanged -= RefreshList;
            DestroyEditor();
            DestroyDraft();
        }

        void OnGUI()
        {
            int tab = GUILayout.Toolbar(_tab, tabs, GUILayout.Height(25f));
            if (tab != _tab)
            {
                _tab = tab;
                Select(null);
                RefreshList();
            }

            DrawHeader(currentTab);
            if (_isListDirty || _sections == null)
            {
                BuildList();
            }

            EditorGUILayout.BeginHorizontal();
            DrawList();
            DrawSeparator();
            DrawRight();
            EditorGUILayout.EndHorizontal();
        }

        void BuildList()
        {
            _isListDirty = false;
            _assets.Clear();
            _sections = GetSections(currentTab) ?? new List<AssetSection>();
            foreach (AssetSection section in _sections)
            {
                _assets[section] = section.FindAssets();
            }
            if (_selected == null && _creatingIn != null && !_sections.Contains(_creatingIn))
            {
                StopCreating();
            }
        }

        void DrawList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(ListWidth));
            _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
            // A fixed width: the room of the vertical scroll bar always kept (the bar only shown when needed), never a
            // horizontal one, the rows fitting what is left
            _listScroll = EditorGUILayout.BeginScrollView(_listScroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none);
            float rowWidth = ListWidth - GUI.skin.verticalScrollbar.fixedWidth - 8f;
            foreach (AssetSection section in _sections)
            {
                DrawSection(section, rowWidth);
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        void DrawSection(AssetSection section, float rowWidth)
        {
            List<ScriptableObject> assets = _assets.TryGetValue(section, out List<ScriptableObject> found) ? found : new List<ScriptableObject>();
            List<ScriptableObject> shown = string.IsNullOrEmpty(_search)
                ? assets
                : assets.Where(asset => asset != null && asset.name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            const float AddWidth = 24f;
            EditorGUILayout.BeginHorizontal(GUILayout.Width(rowWidth));
            float titleWidth = section.canCreate ? rowWidth - AddWidth - 4f : rowWidth;
            string title = string.IsNullOrEmpty(section.title) ? "" : $"{section.title} ({shown.Count})";
            GUILayout.Label(new GUIContent(title, title), EditorStyles.boldLabel, GUILayout.Width(titleWidth));
            if (section.canCreate && GUILayout.Button(new GUIContent("+", "Create a new asset"), EditorStyles.miniButton, GUILayout.Width(AddWidth)))
            {
                StartCreating(section);
            }
            EditorGUILayout.EndHorizontal();

            foreach (ScriptableObject asset in shown)
            {
                if (asset == null)
                {
                    continue;
                }
                bool isSelected = asset == _selected;
                if (GUILayout.Toggle(isSelected, new GUIContent(asset.name, asset.name), EditorStyles.miniButton, GUILayout.Width(rowWidth)) && !isSelected)
                {
                    Select(asset);
                }
            }
            GUILayout.Space(6f);
        }

        // A thin line between the list and the right side, over the whole height
        static void DrawSeparator()
        {
            GUILayout.Space(4f);
            Rect line = GUILayoutUtility.GetRect(1f, 1f, GUILayout.Width(1f), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(line, EditorGUIUtility.isProSkin ? new Color(0.1f, 0.1f, 0.1f) : new Color(0.6f, 0.6f, 0.6f));
            GUILayout.Space(6f);
        }

        void DrawRight()
        {
            EditorGUILayout.BeginVertical();
            if (_creatingIn != null)
            {
                DrawCreation();
            }
            else if (_selected != null)
            {
                DrawSelected();
            }
            else
            {
                EditorGUILayout.HelpBox("Pick an asset in the list, or + to create one", MessageType.None);
            }
            EditorGUILayout.EndVertical();
        }

        void DrawSelected()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label(AssetDatabase.GetAssetPath(_selected), EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Select", EditorStyles.toolbarButton))
            {
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = _selected;
                EditorGUIUtility.PingObject(_selected);
            }
            if (GUILayout.Button("Delete", EditorStyles.toolbarButton))
            {
                ScriptableObject deleted = _selected;
                if (DataAssets.Delete(deleted))
                {
                    Select(null);
                    RefreshList();
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();

            DrawEditor(_selected);
        }

        void DrawCreation()
        {
            AssetSection section = _creatingIn;
            EditorGUILayout.LabelField($"New {(string.IsNullOrEmpty(section.title) ? DataAssets.GetNiceName(section.assetType) : section.title)}", EditorStyles.boldLabel);

            if (section.pickDerivedType)
            {
                _draftName = EditorGUILayout.TextField("Name", _draftName);
                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_draftName)))
                {
                    if (GUILayout.Button("Create..."))
                    {
                        string assetName = _draftName;
                        DataAssets.ShowTypeMenu(section.assetType, type => Created(section.Save(ScriptableObject.CreateInstance(type), assetName)));
                    }
                }
                EditorGUILayout.LabelField(section.GetSavePath(string.IsNullOrWhiteSpace(_draftName) ? "..." : _draftName), EditorStyles.miniLabel);
                if (GUILayout.Button("Cancel"))
                {
                    StopCreating();
                }
                return;
            }

            if (_draft == null)
            {
                _draft = section.CreateDraft();
            }
            string draftName = section.getAssetName != null ? section.getAssetName(_draft) : _draft.name;
            EditorGUILayout.LabelField(section.GetSavePath(draftName), EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create"))
            {
                ScriptableObject asset = _draft;
                _draft = null;
                DestroyEditor();
                Created(section.Save(asset, draftName));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                GUIUtility.ExitGUI();
            }
            if (GUILayout.Button("Cancel"))
            {
                StopCreating();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();

            DrawEditor(_draft);
        }

        void DrawEditor(UnityEngine.Object target)
        {
            UnityEditor.Editor.CreateCachedEditor(target, null, ref _editor);
            _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);
            _editor.OnInspectorGUI();
            EditorGUILayout.EndScrollView();
            if (AssetPreview.IsLoadingAssetPreviews())
            {
                Repaint();
            }
        }

        void Created(ScriptableObject asset)
        {
            StopCreating();
            RefreshList();
            Select(asset);
            EditorGUIUtility.PingObject(asset);
        }

        protected void Select(ScriptableObject asset)
        {
            StopCreating();
            if (asset != _selected)
            {
                DestroyEditor();
                _inspectorScroll = Vector2.zero;
            }
            _selected = asset;
            Repaint();
        }

        void StartCreating(AssetSection section)
        {
            Select(null);
            _creatingIn = section;
            _draftName = "";
        }

        void StopCreating()
        {
            _creatingIn = null;
            DestroyDraft();
            DestroyEditor();
        }

        void DestroyDraft()
        {
            if (_draft != null && !AssetDatabase.Contains(_draft))
            {
                DestroyImmediate(_draft);
            }
            _draft = null;
        }

        void DestroyEditor()
        {
            if (_editor != null)
            {
                DestroyImmediate(_editor);
            }
            _editor = null;
        }
    }
}
