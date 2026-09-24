using UnityEditor;
using UnityEngine;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Studio.Editor
{
    // The spell studio's right column: the selected preset's fields, its resolution and its checks, with undo
    public class SpellInspectorPane
    {
        SpellStudioWindow _window;
        Vector2 _scroll;
        string[] _warnings = new string[0];
        bool _isCheckDirty = true;

        public void Init(SpellStudioWindow window)
        {
            _window = window;
        }

        // Runs the checks again on the next draw and puts the scroll back at the top
        public void Reset()
        {
            _scroll = Vector2.zero;
            _isCheckDirty = true;
        }

        public void InvalidateChecks()
        {
            _isCheckDirty = true;
        }

        public void Draw(Rect rect)
        {
            StudioStyles styles = _window.styles;
            SpellStudioPreset selected = _window.selected;
            EditorGUI.DrawRect(rect, StudioStyles.Panel);
            GUILayout.BeginArea(new Rect(rect.x + 12f, rect.y + 12f, rect.width - 24f, rect.height - 24f));
            GUILayout.Label("CREATOR", styles.section);
            if (selected == null)
            {
                GUILayout.Label("Choose a spell to begin.");
                GUILayout.EndArea();
                return;
            }

            GUILayout.Space(6f);
            string state = "Unsaved draft · edits support Undo";
            if (AssetDatabase.Contains(selected))
            {
                state = "Saved preset · edits support Undo";
            }

            GUILayout.Label(state, styles.small);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            _window.serialized.Update();
            EditorGUIUtility.labelWidth = 112f;
            DrawFields(styles, selected);
            DrawShape(styles, selected);
            DrawChecks(styles, selected);
            GUILayout.Space(16f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Duplicate"))
            {
                EditorGUIUtility.labelWidth = 0f;
                _window.Duplicate();
                GUIUtility.ExitGUI();
            }

            using (new EditorGUI.DisabledScope(!AssetDatabase.Contains(selected)))
            {
                if (GUILayout.Button("Locate asset"))
                {
                    EditorGUIUtility.PingObject(selected);
                }
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Label("Ctrl / Cmd + Z to undo. Use Save as to create a reusable asset.", styles.small);
            EditorGUIUtility.labelWidth = 0f;
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // What the preview shows about the projectile the preset looks up
        public static string ProjectileReadout(SpellStudioPreset preset)
        {
            DeliveryStyle style = preset.ResolveDelivery();
            string path = "";
            if (style == DeliveryStyle.ChainSync)
            {
                path = " · preserves contact path";
            }
            return "Delivery: " + style + path + "\nLookup only; this viewport previews the effect element.";
        }

        void DrawFields(StudioStyles styles, SpellStudioPreset selected)
        {
            EditorGUI.BeginChangeCheck();
            styles.Section("IDENTITY");
            Field("displayName", "Name");
            Field("description", "Notes");
            styles.Section("GRAMMAR & PRESETS");
            SpellGrammarFields.Draw(_window.serialized, selected, styles);
            styles.Section("PREVIEW TIMING");
            bool isOnce = selected.resolvedChannels.tempo == EffectTempo.Once;
            using (new EditorGUI.DisabledScope(isOnce))
            {
                Field("durationSeconds", "Preview length");
            }

            if (isOnce)
            {
                GUILayout.Label("One-shot length follows the resolved entry’s motion cycle.", styles.small);
            }

            styles.Section("CAST CONTEXT");
            Field("stacks", "Stacks");
            Field("charges", "Charges");
            Field("amount", "Amount");
            Field("critical", "Critical");
            Field("side", "Side");
            Field("scale", "Scale");
            styles.Section("COLOUR");
            Field("overrideColour", "Custom colour");
            if (_window.serialized.FindProperty("overrideColour").boolValue)
            {
                Field("colour", "Colour");
            }

            styles.Section("SHAPE & MOTION");
            Field("overrideEntry", "Custom entry");
            bool isChanged = EditorGUI.EndChangeCheck();
            if (_window.serialized.ApplyModifiedProperties() || isChanged)
            {
                _window.RefreshPreview();
                _window.timeline.time = Mathf.Min(_window.timeline.time, _window.duration);
            }
        }

        // The preset's own copy of the shape, and the way back to the shared vocabulary
        void DrawShape(StudioStyles styles, SpellStudioPreset selected)
        {
            if (GUILayout.Button("Copy vocabulary shape into preset"))
            {
                Undo.RecordObject(selected, "Copy spell vocabulary entry");
                if (selected.CaptureEntry())
                {
                    EditorUtility.SetDirty(selected);
                    _window.serialized.Update();
                    _window.RefreshPreview();
                }
                else
                {
                    _window.ShowNotification(new GUIContent("Choose a vocabulary containing this element"));
                }
            }

            if (selected.overrideEntry)
            {
                EditorGUI.BeginChangeCheck();
                Field("entry", "Entry", true);
                if (EditorGUI.EndChangeCheck())
                {
                    _window.serialized.ApplyModifiedProperties();
                    _window.RefreshPreview();
                }
            }
            else
            {
                GUILayout.Label("Copy the source shape to edit parts, motion, socket and counts without changing "
                    + "the shared vocabulary.", styles.small);
            }

            using (new EditorGUI.DisabledScope(selected.vocabulary == null))
            {
                if (GUILayout.Button("Apply shape to vocabulary…") && ConfirmPublish(selected))
                {
                    SpellStudioPublishing.PublishEntry(selected);
                    _window.RefreshPreview();
                }
            }
        }

        void DrawChecks(StudioStyles styles, SpellStudioPreset selected)
        {
            if (_isCheckDirty)
            {
                _warnings = SpellPresetValidator.Validate(selected);
                _isCheckDirty = false;
            }

            if (_warnings.Length == 0)
            {
                return;
            }

            styles.Section("CHECKS");
            foreach (string warning in _warnings)
            {
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }
        }

        static bool ConfirmPublish(SpellStudioPreset selected)
        {
            string message = "Replace " + selected.resolvedElement + " in " + selected.vocabulary.name + "?\n\n"
                + "This changes the shared shape, motion, socket and count used by game effects. Colour and cast "
                + "context remain in this preset. You can undo this change.";
            return EditorUtility.DisplayDialog("Update shared vocabulary?", message, "Apply shape", "Cancel");
        }

        void Field(string name, string label)
        {
            Field(name, label, false);
        }

        void Field(string name, string label, bool isExpanded)
        {
            SerializedProperty property = _window.serialized.FindProperty(name);
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, new GUIContent(label), isExpanded);
            }
        }
    }
}
