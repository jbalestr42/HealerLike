using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Studio.Editor
{
    // Cached side-by-side production previews, with the real shared assets editable beside them through Odin.
    public class CreatureRosterPane
    {
        readonly CreatureRoster _roster = new CreatureRoster();
        LookVocabulary _vocabulary;
        LookPalette _palette;
        UnityEditor.Editor _vocabularyEditor;
        UnityEditor.Editor _paletteEditor;
        EntityData _selected;
        LookSide _side;
        Vector2 _scroll;
        Vector2 _inspectorScroll;
        int _vocabularyVersion;
        int _paletteVersion;
        int _revision;
        int _assetTab;

        public void Init()
        {
            _vocabulary = AssetDatabase.LoadAssetAtPath<LookVocabulary>(RenderGrammarLibraryWindow.AssetPaths[0]);
            Reload();
        }

        public void Reload()
        {
            _roster.Reload(_vocabulary, Side());
            RememberRevision();
        }

        public bool RefreshIfChanged()
        {
            if (_vocabularyVersion == Version(_vocabulary) && _paletteVersion == Version(_vocabulary ?
                _vocabulary.palette : null) && _palette == (_vocabulary ? _vocabulary.palette : null))
            {
                return false;
            }
            Refresh();
            return true;
        }

        public void Refresh()
        {
            _roster.Rebuild(_vocabulary, Side());
            RememberRevision();
        }

        public void Draw(Rect area)
        {
            Rect cards = new Rect(area.x, area.y, area.width - 380f, area.height);
            GUILayout.BeginArea(cards);
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label(_roster.rows.Count + " real units");
            LookSide side = (LookSide)EditorGUILayout.EnumPopup(_side, GUILayout.Width(90f));
            if (side != _side)
            {
                _side = side;
                Refresh();
            }
            if (GUILayout.Button("Reload assets", EditorStyles.toolbarButton))
            {
                Reload();
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("Derived from every EntityData. Shared side is an audition; authored overrides are not applied.",
                EditorStyles.wordWrappedMiniLabel);
            _scroll = GUILayout.BeginScrollView(_scroll);
            int columns = Mathf.Max(2, Mathf.FloorToInt((cards.width - 20f) / 245f));
            float width = (cards.width - 25f) / columns - 8f;
            for (int i = 0; i < _roster.rows.Count; i += columns)
            {
                GUILayout.BeginHorizontal();
                for (int c = i; c < Mathf.Min(i + columns, _roster.rows.Count); c++)
                {
                    DrawRow(_roster.rows[c], width);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            DrawInspector(new Rect(area.xMax - 370f, area.y, 370f, area.height));
        }

        void DrawRow(CreatureRosterRow row, float width)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(width));
            if (GUILayout.Toggle(_selected == row.source, new GUIContent(row.source.name, row.path), "Button"))
            {
                _selected = row.source;
            }
            Rect image = GUILayoutUtility.GetRect(width, width * 0.83f);
            if (Event.current.type == EventType.Repaint)
            {
                Texture2D texture = row.Image();
                if (texture)
                {
                    GUI.DrawTexture(image, texture, ScaleMode.ScaleToFit);
                }
                else
                {
                    GUI.Label(image, "Recipe unavailable");
                }
            }
            GUILayout.Label(row.ChannelLabel(), EditorStyles.wordWrappedMiniLabel);
            if (GUILayout.Button("Select source asset", EditorStyles.miniButton))
            {
                _selected = row.source;
                Selection.activeObject = row.source;
                EditorGUIUtility.PingObject(row.source);
            }
            GUILayout.EndVertical();
        }

        void DrawInspector(Rect area)
        {
            GUILayout.BeginArea(area, EditorStyles.helpBox);
            GUILayout.Label("Shared vocabulary and palette", EditorStyles.boldLabel);
            GUILayout.Label("Preview revision " + _revision + ". Edits apply to the live render layer in Play mode.",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUI.BeginChangeCheck();
            LookVocabulary next = (LookVocabulary)EditorGUILayout.ObjectField("Vocabulary", _vocabulary,
                typeof(LookVocabulary), false);
            if (EditorGUI.EndChangeCheck())
            {
                _vocabulary = next;
                Refresh();
            }
            _assetTab = GUILayout.Toolbar(_assetTab, new[] { "Vocabulary", "Palette" });
            Object asset = _assetTab == 0 ? (Object)_vocabulary : _palette;
            GUILayout.Label(asset ? AssetDatabase.GetAssetPath(asset) : "No asset", EditorStyles.wordWrappedMiniLabel);
            if (_selected)
            {
                GUILayout.Label("Selected unit: " + _selected.name, EditorStyles.boldLabel);
            }
            if (asset && GUILayout.Button("Save shared asset"))
            {
                AssetDatabase.SaveAssetIfDirty(asset);
            }
            _inspectorScroll = GUILayout.BeginScrollView(_inspectorScroll);
            UnityEditor.Editor inspector = _assetTab == 0 ? _vocabularyEditor : _paletteEditor;
            if (inspector)
            {
                EditorGUI.BeginChangeCheck();
                inspector.OnInspectorGUI();
                if (EditorGUI.EndChangeCheck())
                {
                    Refresh();
                    RenderGrammarLibraryWindow.OnAssetChanged.Invoke(asset);
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        void RememberRevision()
        {
            LookPalette palette = _vocabulary ? _vocabulary.palette : null;
            if (!_vocabularyEditor || _vocabularyEditor.target != _vocabulary)
            {
                ReleaseEditor(ref _vocabularyEditor);
                _vocabularyEditor = RenderGrammarLibraryWindow.CreateNativeInspector(_vocabulary);
            }
            if (_palette != palette || !_paletteEditor)
            {
                ReleaseEditor(ref _paletteEditor);
                _palette = palette;
                _paletteEditor = RenderGrammarLibraryWindow.CreateNativeInspector(_palette);
            }
            _vocabularyVersion = Version(_vocabulary);
            _paletteVersion = Version(_palette);
            _revision++;
        }

        static int Version(Object asset)
        {
            return asset ? EditorUtility.GetDirtyCount(asset) : 0;
        }

        Entity.EntityType Side()
        {
            return _side == LookSide.Plant ? Entity.EntityType.Player : Entity.EntityType.Computer;
        }

        public void Dispose()
        {
            _roster.Dispose();
            ReleaseEditor(ref _vocabularyEditor);
            ReleaseEditor(ref _paletteEditor);
        }

        static void ReleaseEditor(ref UnityEditor.Editor editor)
        {
            if (editor)
            {
                Object.DestroyImmediate(editor);
            }
            editor = null;
        }
    }
}
