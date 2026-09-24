using System.IO;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Studio.Editor
{
    // The creature studio's header: the mode switch and the new, save as and save buttons of the current mode
    public class CreatureStudioHeader
    {
        static readonly string[] modeNames = { "Grammar", "Parts" };

        CreatureStudioWindow _window;

        public void Init(CreatureStudioWindow window)
        {
            _window = window;
        }

        public void Draw(Rect position)
        {
            StudioStyles styles = _window.styles;
            GUI.Label(new Rect(20f, 12f, 250f, 30f), "Creature Studio", styles.title);
            if (GUI.Button(new Rect(270f, 17f, 100f, 24f), "Spells →"))
            {
                EditorApplication.ExecuteMenuItem("Tools/Render/Spell Studio");
            }

            GUI.Label(new Rect(21f, 43f, 570f, 22f),
                "RENDER LAB  /  Assemble, sculpt and animate your creature recipes",
                styles.small);
            int current = 1;
            if (_window.isGrammarMode)
            {
                current = 0;
            }

            int mode = GUI.Toolbar(new Rect(390f, 17f, 180f, 25f), current, modeNames);
            if (mode != current)
            {
                if (mode == 0)
                {
                    _window.SwitchToGrammar();
                }
                else
                {
                    _window.SwitchToParts(_window.partsSelection);
                }
                GUIUtility.ExitGUI();
            }
            DrawSaveButtons(position);
        }

        void DrawSaveButtons(Rect position)
        {
            bool isGrammar = _window.isGrammarMode;
            Object subject = _window.selected;
            if (isGrammar)
            {
                subject = _window.grammar.selected;
            }

            Rect rect = new Rect(position.width - 325f, 24f, 95f, 26f);
            if (GUI.Button(rect, isGrammar ? "New grammar" : "New creature"))
            {
                NewDraft();
                GUIUtility.ExitGUI();
            }

            rect.x += 102f;
            using (new EditorGUI.DisabledScope(subject == null))
            {
                if (GUI.Button(rect, isGrammar ? "Save preset…" : "Save recipe…"))
                {
                    SaveAs();
                    GUIUtility.ExitGUI();
                }
            }

            rect.x += 102f;
            using (new EditorGUI.DisabledScope(subject == null || !AssetDatabase.Contains(subject)))
            {
                if (GUI.Button(rect, "Save"))
                {
                    AssetDatabase.SaveAssetIfDirty(subject);
                    _window.ShowNotification(new GUIContent(isGrammar ? "Grammar preset saved" : "Recipe saved"));
                }
            }
        }

        // A blank grammar draft, or the sprout starter as a new recipe draft
        void NewDraft()
        {
            if (_window.isGrammarMode)
            {
                CreatureGrammarMode grammar = _window.grammar;
                _window.SelectGrammar(grammar.drafts.NewDraft(grammar.creatureLooks));
                return;
            }

            CreatureRecipe draft = CreatureStudioAuthoring.BuildSample(1);
            if (draft == null)
            {
                _window.ShowNotification(new GUIContent("Starter assets are unavailable"));
                return;
            }

            draft.name = "Untitled creature";
            _window.drafts.Add(draft);
            _window.SwitchToParts(draft);
        }

        void SaveAs()
        {
            if (_window.isGrammarMode)
            {
                _window.grammar.SaveAs();
                return;
            }

            CreatureRecipe selected = _window.selected;
            string path = EditorUtility.SaveFilePanelInProject("Save creature recipe", selected.name, "asset",
                "Choose where to save your creature recipe.", "Assets/Render/Creatures/Data");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            CreatureRecipe copy = CreatureStudioAuthoring.Clone(selected);
            copy.hideFlags = HideFlags.None;
            copy.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(copy, AssetDatabase.GenerateUniqueAssetPath(path));
            AssetDatabase.SaveAssetIfDirty(copy);
            _window.drafts.RememberSurface(copy, _window.manualSurface);
            _window.ReloadAssets();
            _window.SwitchToParts(copy);
            EditorGUIUtility.PingObject(copy);
            _window.ShowNotification(new GUIContent("Creature recipe saved"));
        }
    }
}
