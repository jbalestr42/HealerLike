using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Studio.Editor
{
    // The spell studio's left column: the drafts, the saved presets and every gameplay buff handler, under a search
    public class SpellLibraryPane
    {
        readonly List<SpellStudioPreset> _assets = new List<SpellStudioPreset>();
        readonly List<ABuffHandlerFactory> _handlers = new List<ABuffHandlerFactory>();
        SpellStudioWindow _window;
        Vector2 _scroll;
        string _search = "";

        public void Init(SpellStudioWindow window)
        {
            _window = window;
        }

        // Lists the project's handlers and saved presets again, sorted by label
        public void Reload()
        {
            _assets.Clear();
            _handlers.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:ABuffHandlerFactory"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ABuffHandlerFactory handler = AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>(path);
                if (handler != null)
                {
                    _handlers.Add(handler);
                }
            }

            _handlers.Sort(CompareHandlers);
            foreach (string guid in AssetDatabase.FindAssets("t:SpellStudioPreset"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                SpellStudioPreset asset = AssetDatabase.LoadAssetAtPath<SpellStudioPreset>(path);
                if (asset != null)
                {
                    _assets.Add(asset);
                }
            }
            _assets.Sort(ComparePresets);
        }

        public void Draw(Rect rect)
        {
            StudioStyles styles = _window.styles;
            EditorGUI.DrawRect(rect, StudioStyles.Panel);
            GUILayout.BeginArea(new Rect(rect.x + 10f, rect.y + 12f, rect.width - 20f, rect.height - 24f));
            GUILayout.Label("SPELL LIBRARY", styles.section);
            GUILayout.Space(8f);
            _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
            GUILayout.Space(6f);
            if (GUILayout.Button("Grammar & native presets…"))
            {
                RenderGrammarLibraryWindow.OpenSpells();
            }

            if (GUILayout.Button("New grammar preset"))
            {
                _window.NewGrammarDraft();
                GUIUtility.ExitGUI();
            }

            if (GUILayout.Button("Add sample presets"))
            {
                SpellStudioSamples.Create();
                _window.ReloadAssets();
            }

            GUILayout.Space(6f);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawPresets(styles);
            GUILayout.Space(16f);
            GUILayout.Label("GAMEPLAY HANDLERS  ·  " + _handlers.Count, styles.small);
            foreach (ABuffHandlerFactory handler in _handlers)
            {
                DrawHandler(handler);
            }

            EditorGUILayout.EndScrollView();
            GUILayout.Space(8f);
            GUILayout.Label("Vocabulary selections are local drafts. Drafts are kept locally. "
                + "Save as a preset to share them.", styles.small);
            GUILayout.EndArea();
        }

        void DrawPresets(StudioStyles styles)
        {
            List<SpellStudioPreset> drafts = _window.drafts.items;
            GUILayout.Label("VOCABULARY & DRAFTS  ·  " + drafts.Count, styles.small);
            foreach (SpellStudioPreset draft in drafts)
            {
                DrawCard(draft, false);
            }

            GUILayout.Space(16f);
            GUILayout.Label("SAVED PRESETS  ·  " + _assets.Count, styles.small);
            foreach (SpellStudioPreset asset in _assets)
            {
                DrawCard(asset, true);
            }

            if (_assets.Count == 0)
            {
                GUILayout.Label("Save a creation to build your own library.", styles.small);
            }
        }

        void DrawCard(SpellStudioPreset preset, bool isSaved)
        {
            if (preset == null || !Matches(preset))
            {
                return;
            }

            string label = SpellStudioDrafts.Label(preset);
            if (_window.styles.DrawCard(label, _window.selected == preset, isSaved))
            {
                _window.Select(preset);
                GUIUtility.ExitGUI();
            }
        }

        void DrawHandler(ABuffHandlerFactory handler)
        {
            string label = SpellStudioDrafts.HandlerLabel(handler);
            if (!string.IsNullOrEmpty(_search) && label.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            GUIContent content = new GUIContent("↳ " + label, AssetDatabase.GetAssetPath(handler));
            if (GUILayout.Button(content, EditorStyles.miniButton))
            {
                _window.NewHandlerDraft(handler);
                GUIUtility.ExitGUI();
            }
        }

        // The search matches the label or the element the preset resolves to
        bool Matches(SpellStudioPreset preset)
        {
            if (string.IsNullOrEmpty(_search))
            {
                return true;
            }

            string label = SpellStudioDrafts.Label(preset);
            bool isLabelMatched = label.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0;
            string element = preset.resolvedElement.ToString();
            return isLabelMatched || element.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static int CompareHandlers(ABuffHandlerFactory a, ABuffHandlerFactory b)
        {
            string labelA = SpellStudioDrafts.HandlerLabel(a);
            string labelB = SpellStudioDrafts.HandlerLabel(b);
            return string.Compare(labelA, labelB, StringComparison.OrdinalIgnoreCase);
        }

        static int ComparePresets(SpellStudioPreset a, SpellStudioPreset b)
        {
            return string.Compare(SpellStudioDrafts.Label(a), SpellStudioDrafts.Label(b),
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
