using System.IO;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Studio.Editor
{
    // The creature studio's middle column: the rig preview, its timeline and the readouts the rig draws
    // (vitality, charge, glow), with the PNG export
    public class CreatureViewport
    {
        // Idle motion loops over this many seconds in the timeline
        static readonly float length = 8f;

        readonly StudioTimeline _timeline = new StudioTimeline();
        CreatureStudioWindow _window;
        CreatureStudioPreview _preview;

        public CreatureStudioPreview preview { get { return _preview; } }

        public void Init(CreatureStudioWindow window)
        {
            _window = window;
            _preview = new CreatureStudioPreview();
            _preview.Init();
            _timeline.Init();
        }

        public void Dispose()
        {
            _preview.Dispose();
        }

        public void Refresh()
        {
            _preview.Refresh();
        }

        public void Restart()
        {
            _timeline.time = 0f;
        }

        public void Tick()
        {
            if (_timeline.Tick(length, _window.selected != null))
            {
                _window.Repaint();
            }
        }

        // Space plays and pauses, F frames the subject, unless a text field has the keyboard
        public void HandleShortcuts()
        {
            Event input = Event.current;
            bool isModified = input.alt || input.control || input.command;
            if (input.type != EventType.KeyDown || EditorGUIUtility.editingTextField || isModified)
            {
                return;
            }

            if (input.keyCode == KeyCode.Space)
            {
                _timeline.Toggle(length);
                input.Use();
                _window.Repaint();
            }
            else if (input.keyCode == KeyCode.F)
            {
                _preview.ResetCamera();
                input.Use();
                _window.Repaint();
            }
        }

        public void Draw(Rect rect)
        {
            StudioStyles styles = _window.styles;
            EditorGUI.DrawRect(rect, StudioStyles.Panel);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 10f, 220f, 20f), Title(), styles.section);
            CreatureRecipe selected = _window.selected;
            if (selected == null)
            {
                return;
            }

            if (GUI.Button(new Rect(rect.xMax - 180f, rect.y + 7f, 87f, 23f), "Export PNG"))
            {
                Export();
            }

            if (GUI.Button(new Rect(rect.xMax - 87f, rect.y + 7f, 75f, 23f), "Reset view"))
            {
                _preview.ResetCamera();
                _window.Repaint();
            }

            Rect render = new Rect(rect.x + 1f, rect.y + 38f, rect.width - 2f, rect.height - 223f);
            _preview.side = _window.previewSide;
            _preview.selectedPart = SelectedPart();
            _preview.Draw(render, selected, _timeline.time);
            GUI.Label(new Rect(render.x + 14f, render.y + 12f, render.width - 28f, 22f), selected.name,
                EditorStyles.boldLabel);
            GUI.Label(new Rect(render.x + 14f, render.y + 35f, render.width - 28f, 20f), Counts(selected),
                styles.small);
            GUILayout.BeginArea(new Rect(rect.x + 12f, render.yMax + 10f, rect.width - 24f, 174f));
            if (_timeline.Draw(styles, length))
            {
                _window.RefreshPreview();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(_timeline.Readout(length), styles.small);
            _preview.isGroundShown = GUILayout.Toggle(_preview.isGroundShown, "Ground", GUILayout.Width(65f));
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10f);
            GUILayout.Label("VISUAL READOUTS", styles.section);
            EditorGUIUtility.labelWidth = 65f;
            _preview.health = EditorGUILayout.Slider("Vitality", _preview.health, 0f, 1f);
            _preview.charge = EditorGUILayout.Slider("Charge", _preview.charge, 0f, 1f);
            _preview.glow = EditorGUILayout.Slider("Glow", _preview.glow, 0f, 1f);
            EditorGUIUtility.labelWidth = 0f;
            GUILayout.EndArea();
        }

        string Title()
        {
            if (!_window.isGrammarMode)
            {
                return "AUTHORED RECIPE";
            }

            if (_window.grammar.isOverrideShown)
            {
                return "AUTHORED GAME OVERRIDE";
            }
            return "LIVE GRAMMAR OUTPUT";
        }

        // Grammar mode draws no selection box
        int SelectedPart()
        {
            if (_window.isGrammarMode)
            {
                return -1;
            }
            return _window.parts.selectedPart;
        }

        static string Counts(CreatureRecipe recipe)
        {
            int parts = 0;
            if (recipe.parts != null)
            {
                parts = recipe.parts.Length;
            }

            int arms = 0;
            if (recipe.arms != null)
            {
                arms = recipe.arms.Length;
            }
            return parts + " parts  /  " + arms + " arms";
        }

        // The view without its selection box, at 1600 by 1000
        void Export()
        {
            CreatureRecipe selected = _window.selected;
            string path = EditorUtility.SaveFilePanel("Export creature preview", "", selected.name + ".png", "png");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            _preview.side = _window.previewSide;
            _preview.selectedPart = -1;
            Texture2D image = _preview.Capture(selected, _timeline.time, 1600, 1000);
            _preview.selectedPart = SelectedPart();
            if (image == null)
            {
                string message = "Choose a creature to capture.";
                if (!string.IsNullOrEmpty(_preview.lastError))
                {
                    message = _preview.lastError;
                }
                EditorUtility.DisplayDialog("Could not export preview", message, "OK");
                return;
            }

            File.WriteAllBytes(path, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            _window.ShowNotification(new GUIContent("Preview exported at 1600 × 1000"));
        }
    }
}
