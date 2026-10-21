using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Studio.Editor
{
    // The creature studio's left column: grammar presets in grammar mode, recipe drafts and assets in parts mode,
    // under one search
    public class CreatureLibraryPane
    {
        readonly List<CreatureRecipe> _assets = new List<CreatureRecipe>();
        CreatureStudioWindow _window;
        Vector2 _scroll;
        string _search = "";

        public void Init(CreatureStudioWindow window)
        {
            _window = window;
        }

        // Lists the project's recipes again, by name
        public void Reload()
        {
            _assets.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:CreatureRecipe"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CreatureRecipe recipe = AssetDatabase.LoadAssetAtPath<CreatureRecipe>(path);
                if (recipe != null)
                {
                    _assets.Add(recipe);
                }
            }
            _assets.Sort(CompareRecipes);
        }

        public void DrawParts(Rect rect)
        {
            StudioStyles styles = BeginPane(rect, "CREATURE LIBRARY");
            GUILayout.Label("STARTERS & DRAFTS  ·  " + _window.drafts.items.Count, styles.small);
            foreach (CreatureRecipe recipe in _window.drafts.items)
            {
                DrawRecipe(recipe, false);
            }

            GUILayout.Space(16f);
            GUILayout.Label("SAVED RECIPES  ·  " + _assets.Count, styles.small);
            foreach (CreatureRecipe recipe in _assets)
            {
                DrawRecipe(recipe, true);
            }

            EditorGUILayout.EndScrollView();
            GUILayout.Label("Drafts are kept locally. Save as a recipe asset to use your creature in the renderer.",
                styles.small);
            GUILayout.EndArea();
        }

        public void DrawGrammar(Rect rect)
        {
            StudioStyles styles = BeginPane(rect, "GRAMMAR PRESETS");
            CreatureGrammarMode grammar = _window.grammar;
            GUILayout.Label("LOCAL CHANNEL DRAFTS", styles.small);
            foreach (CreatureGrammarPreset preset in grammar.drafts.items)
            {
                DrawPreset(preset, false);
            }

            GUILayout.Space(16f);
            GUILayout.Label("SAVED PRESETS  ·  " + grammar.assets.Count, styles.small);
            foreach (CreatureGrammarPreset preset in grammar.assets)
            {
                DrawPreset(preset, true);
            }

            if (grammar.assets.Count == 0)
            {
                GUILayout.Label("Save a grammar preset to keep editable channels and vocabulary references.",
                    styles.small);
            }

            GUILayout.Space(18f);
            GUILayout.Label("NATIVE RENDER LIBRARY", styles.section);
            GUILayout.Label(OverrideCounts(grammar.creatureLooks), styles.small);
            if (GUILayout.Button("Vocabulary & presets"))
            {
                OpenLibrary(grammar);
            }

            GUILayout.Space(8f);
            GUILayout.Label("Parts mode retains authored CreatureRecipe assets and manual drafts.", styles.small);
            EditorGUILayout.EndScrollView();
            GUILayout.Label("Channels stay editable. Bake only when you want to change individual parts.",
                styles.small);
            GUILayout.EndArea();
        }

        // The panel, its title and search, and the opened scroll view
        StudioStyles BeginPane(Rect rect, string title)
        {
            StudioStyles styles = _window.styles;
            EditorGUI.DrawRect(rect, StudioStyles.Panel);
            GUILayout.BeginArea(new Rect(rect.x + 10f, rect.y + 12f, rect.width - 20f, rect.height - 24f));
            GUILayout.Label(title, styles.section);
            GUILayout.Space(8f);
            _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
            GUILayout.Space(10f);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            return styles;
        }

        void DrawRecipe(CreatureRecipe recipe, bool isSaved)
        {
            if (recipe == null || !Matches(recipe.name))
            {
                return;
            }

            if (_window.styles.DrawCard(recipe.name, recipe == _window.selected, isSaved))
            {
                _window.SwitchToParts(recipe);
                GUIUtility.ExitGUI();
            }
        }

        void DrawPreset(CreatureGrammarPreset preset, bool isSaved)
        {
            if (preset == null || !Matches(CreatureGrammarDrafts.Label(preset)))
            {
                return;
            }

            bool isSelected = preset == _window.grammar.selected;
            if (_window.styles.DrawCard(CreatureGrammarDrafts.Label(preset), isSelected, isSaved))
            {
                _window.SelectGrammar(preset);
                GUIUtility.ExitGUI();
            }
        }

        bool Matches(string label)
        {
            return string.IsNullOrEmpty(_search) || label.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // The override table when there is one, else the preset's vocabulary
        static void OpenLibrary(CreatureGrammarMode grammar)
        {
            if (grammar.creatureLooks != null)
            {
                RenderGrammarLibraryWindow.OpenAsset(grammar.creatureLooks);
            }
            else if (grammar.selected != null)
            {
                RenderGrammarLibraryWindow.OpenAsset(grammar.selected.vocabulary);
            }
            else
            {
                RenderGrammarLibraryWindow.OpenAsset(null);
            }
        }

        static string OverrideCounts(CreatureLooks looks)
        {
            int entities = 0;
            int characters = 0;
            if (looks != null)
            {
                entities = RenderGrammarSummary.Count(looks.entities);
                characters = RenderGrammarSummary.Count(looks.characters);
            }
            return "Entity overrides: " + entities + "\nCharacter overrides: " + characters;
        }

        static int CompareRecipes(CreatureRecipe a, CreatureRecipe b)
        {
            return string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
