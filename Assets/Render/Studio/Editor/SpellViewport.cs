using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Studio.Editor
{
    // The spell studio's middle column: the effect preview, its timeline, the target creature and the PNG export
    public class SpellViewport
    {
        static readonly string[] surfaceNames = { "Auto", "Plant", "Stone" };

        SpellStudioWindow _window;
        SpellStudioPreview _preview;

        public SpellStudioPreview preview { get { return _preview; } }

        public void Init(SpellStudioWindow window)
        {
            _window = window;
            _preview = new SpellStudioPreview();
            _preview.Init();
            _preview.target.recipe = window.targetCreature;
            ApplyTargetSurface();
        }

        public void Dispose()
        {
            if (_preview != null)
            {
                _preview.Dispose();
                _preview = null;
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
                _window.timeline.Toggle(_window.duration);
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
            SpellStudioPreset selected = _window.selected;
            EditorGUI.DrawRect(rect, StudioStyles.Panel);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 10f, 180f, 20f), "LIVE PREVIEW", styles.section);
            if (selected == null)
            {
                return;
            }

            Rect render = new Rect(rect.x + 1f, rect.y + 38f, rect.width - 2f, rect.height - 155f);
            _preview.Draw(render, selected, _window.timeline.time);
            EffectChannels channels = selected.resolvedChannels;
            string resolution = selected.resolvedElement + " / " + channels.family + " / " + channels.tempo;
            GUI.Label(new Rect(render.x + 14f, render.y + 12f, render.width - 28f, 24f),
                SpellStudioDrafts.Label(selected),
                EditorStyles.boldLabel);
            GUI.Label(new Rect(render.x + 14f, render.y + 34f, render.width - 28f, 20f), resolution, styles.small);
            if (GUI.Button(new Rect(rect.xMax - 180f, rect.y + 7f, 87f, 23f), "Export PNG"))
            {
                Export();
            }

            if (GUI.Button(new Rect(rect.xMax - 87f, rect.y + 7f, 75f, 23f), "Reset view"))
            {
                _preview.ResetCamera();
                _window.Repaint();
            }

            GUILayout.BeginArea(new Rect(rect.x + 12f, render.yMax + 10f, rect.width - 24f, 102f));
            if (_window.timeline.Draw(styles, _window.duration))
            {
                _window.RefreshPreview();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(_window.timeline.Readout(_window.duration), styles.small);
            GUILayout.FlexibleSpace();
            _preview.isGroundShown = GUILayout.Toggle(_preview.isGroundShown, "Ground");
            _preview.isReferenceShown = GUILayout.Toggle(_preview.isReferenceShown, "Target");
            EditorGUILayout.EndHorizontal();
            DrawTarget();
            GUILayout.EndArea();
        }

        // The creature the spell plays on and the surface its palette reads
        void DrawTarget()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            GUIContent label = new GUIContent("Creature", "Reference recipe; None uses the built-in healer.");
            CreatureRecipe creature = (CreatureRecipe)EditorGUILayout.ObjectField(label, _window.targetCreature,
                typeof(CreatureRecipe), false);
            if (EditorGUI.EndChangeCheck())
            {
                _window.targetCreature = creature;
                _preview.target.recipe = creature;
                _preview.Refresh();
                _window.Repaint();
            }

            EditorGUI.BeginChangeCheck();
            int surface = EditorGUILayout.Popup(_window.targetSurface, surfaceNames, GUILayout.Width(65f));
            if (EditorGUI.EndChangeCheck())
            {
                _window.targetSurface = surface;
                ApplyTargetSurface();
                _preview.Refresh();
                _window.Repaint();
            }

            using (new EditorGUI.DisabledScope(_window.targetCreature == null))
            {
                if (GUILayout.Button("Edit", GUILayout.Width(42f)))
                {
                    AssetDatabase.OpenAsset(_window.targetCreature);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        // 0 reads the side from the creature, 1 is plant, 2 is stone
        void ApplyTargetSurface()
        {
            if (_window.targetSurface == 0)
            {
                _preview.target.isAutomatic = true;
            }
            else if (_window.targetSurface == 2)
            {
                _preview.target.side = LookSide.Stone;
            }
            else
            {
                _preview.target.side = LookSide.Plant;
            }
        }

        void Export()
        {
            SpellStudioPreset selected = _window.selected;
            string label = SpellStudioDrafts.Label(selected);
            string path = EditorUtility.SaveFilePanel("Export spell preview", "", label + ".png", "png");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            Texture2D image = _preview.Capture(selected, _window.timeline.time, 1600, 1000);
            if (image == null)
            {
                EditorUtility.DisplayDialog("Could not export preview", _preview.error, "OK");
                return;
            }

            StudioCaptureOutput.Write(image, path);
            _window.ShowNotification(new GUIContent("Preview exported at 1600 × 1000"));
        }
    }
}
