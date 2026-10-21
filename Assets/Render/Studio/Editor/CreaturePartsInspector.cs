using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Studio.Editor
{
    // The creature studio's right column in parts mode: the recipe's parts as a list, the selected part's fields,
    // the rig, the sockets and the checks. Structural edits go through CreaturePartActions.
    public class CreaturePartsInspector
    {
        static readonly Color rowHighlight = new Color(0.22f, 0.18f, 0.32f);

        readonly CreaturePartActions _actions = new CreaturePartActions();
        CreatureStudioWindow _window;
        Vector2 _scroll;
        Vector2 _partsScroll;
        int _selectedPart;
        int _selectedArm;
        Primitive _newPrimitive = Primitive.Sphere;
        string[] _warnings = new string[0];
        bool _isCheckDirty = true;

        public CreaturePartActions actions { get { return _actions; } }

        public int selectedPart { get { return _selectedPart; } set { _selectedPart = value; } }

        public int selectedArm { get { return _selectedArm; } set { _selectedArm = value; } }

        public Primitive newPrimitive { get { return _newPrimitive; } }

        public void Init(CreatureStudioWindow window)
        {
            _window = window;
            _actions.Init(window, this);
        }

        // The root part selected and the scroll back at the top, for a newly selected recipe
        public void Reset()
        {
            _selectedPart = 0;
            _scroll = Vector2.zero;
        }

        public void InvalidateChecks()
        {
            _isCheckDirty = true;
        }

        public void Draw(Rect rect)
        {
            using (StudioLabelWidthScope width = new StudioLabelWidthScope(112f))
            {
                StudioStyles styles = _window.styles;
                CreatureRecipe selected = _window.selected;
                EditorGUI.DrawRect(rect, StudioStyles.Panel);
                GUILayout.BeginArea(new Rect(rect.x + 12f, rect.y + 12f, rect.width - 24f, rect.height - 24f));
                GUILayout.Label("CREATOR", styles.section);
                if (selected == null)
                {
                    GUILayout.Label("Choose a creature to begin.");
                    GUILayout.EndArea();
                    return;
                }

                GUILayout.Space(6f);
                string state = "Local draft · edits support Undo";
                if (AssetDatabase.Contains(selected))
                {
                    state = "Saved recipe · edits support Undo";
                }

                GUILayout.Label(state, styles.small);
                _scroll = EditorGUILayout.BeginScrollView(_scroll);
                _window.serialized.Update();
                DrawIdentity(styles, selected);
                DrawAssembly(styles);
                DrawRig(styles, selected);
                DrawChecks(styles, selected);
                GUILayout.Space(16f);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Duplicate creature"))
                {
                    _actions.DuplicateRecipe();
                    EndMutation();
                }

                using (new EditorGUI.DisabledScope(!AssetDatabase.Contains(selected)))
                {
                    if (GUILayout.Button("Locate asset"))
                    {
                        EditorGUIUtility.PingObject(selected);
                    }
                }

                EditorGUILayout.EndHorizontal();
                GUILayout.Label("Ctrl / Cmd + Z to undo. Changes are visual recipe data, ready for the existing "
                    + "renderer.",
                    styles.small);
                EditorGUILayout.EndScrollView();
                GUILayout.EndArea();
            }
        }

        void DrawIdentity(StudioStyles styles, CreatureRecipe selected)
        {
            EditorGUI.BeginChangeCheck();
            styles.Section("IDENTITY");
            Field("m_Name", "Name", false);
            if (EditorGUI.EndChangeCheck())
            {
                _actions.ApplyEdits();
            }

            // A preview setting: it neither dirties nor announces the recipe
            EditorGUI.BeginChangeCheck();
            _window.manualSurface = (LookSide)EditorGUILayout.EnumPopup("Preview surface", _window.manualSurface);
            if (EditorGUI.EndChangeCheck())
            {
                _window.drafts.RememberSurface(selected, _window.manualSurface);
                _window.Repaint();
            }
        }

        void DrawAssembly(StudioStyles styles)
        {
            styles.Section("PART ASSEMBLY");
            SerializedProperty parts = _window.serialized.FindProperty("parts");
            _selectedPart = Mathf.Clamp(_selectedPart, 0, Mathf.Max(0, parts.arraySize - 1));
            _partsScroll = EditorGUILayout.BeginScrollView(_partsScroll, GUILayout.Height(145f));
            for (int i = 0; i < parts.arraySize; i++)
            {
                string id = parts.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue;
                if (string.IsNullOrWhiteSpace(id))
                {
                    id = "Unnamed part";
                }

                Rect row = GUILayoutUtility.GetRect(10f, 26f, GUILayout.ExpandWidth(true));
                if (i == _selectedPart)
                {
                    EditorGUI.DrawRect(row, rowHighlight);
                }

                if (GUI.Button(row, i + "  " + id, styles.card))
                {
                    _selectedPart = i;
                    GUI.FocusControl(null);
                    _window.Repaint();
                }
            }

            EditorGUILayout.EndScrollView();
            _newPrimitive = (Primitive)EditorGUILayout.EnumPopup("New primitive", _newPrimitive);
            DrawPartButtons(parts.arraySize);
            if (parts.arraySize > 0)
            {
                styles.Section("SELECTED PART  ·  " + _selectedPart);
                if (CreaturePartFields.Draw(parts, _selectedPart))
                {
                    _actions.ApplyEdits();
                }
                GUILayout.Label("Parent indexes reference the list above. Part 0 is the root; removal includes "
                    + "descendants and attached arms.", styles.small);
            }
        }

        void DrawPartButtons(int count)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add child"))
            {
                _actions.AddPart();
                EndMutation();
            }

            using (new EditorGUI.DisabledScope(count == 0))
            {
                if (GUILayout.Button("Duplicate"))
                {
                    _actions.DuplicatePart();
                    EndMutation();
                }
            }

            using (new EditorGUI.DisabledScope(count == 0 || _selectedPart == 0))
            {
                if (GUILayout.Button("Remove"))
                {
                    _actions.RemovePart();
                    EndMutation();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawRig(StudioStyles styles, CreatureRecipe selected)
        {
            styles.Section("RIG & MOTION");
            EditorGUI.BeginChangeCheck();
            Field("roots", "Roots / feet", true);
            Field("arms", "Arms", true);
            if (EditorGUI.EndChangeCheck())
            {
                _actions.ApplyEdits();
            }

            if (GUILayout.Button("Add arm to selected part"))
            {
                _actions.AddArm();
                EndMutation();
            }

            if (selected.arms != null && selected.arms.Length > 0)
            {
                DrawArmTools(selected);
            }

            GUILayout.Label("Arm tools keep joints and source sockets consistent. Rebuild rest pose after changing "
                + "segment count or length.", styles.small);
            EditorGUI.BeginChangeCheck();
            Field("idle", "Idle motion", true);
            styles.Section("SOCKETS & MATERIAL");
            Field("sourceLocal", "Source sockets", true);
            Field("neckLocal", "Neck socket", false);
            Field("wiltColour", "Wilt colour", false);
            Field("stoneOchre", "Stone ochre", false);
            if (EditorGUI.EndChangeCheck())
            {
                _actions.ApplyEdits();
            }
        }

        void DrawArmTools(CreatureRecipe selected)
        {
            _selectedArm = Mathf.Clamp(_selectedArm, 0, selected.arms.Length - 1);
            string[] labels = new string[selected.arms.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i] = "Arm " + i + " · part " + selected.arms[i].bodyPart;
            }

            _selectedArm = EditorGUILayout.Popup("Arm tools", _selectedArm, labels);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Rebuild rest pose"))
            {
                _actions.RebuildArm();
                EndMutation();
            }

            if (GUILayout.Button("Remove arm"))
            {
                _actions.RemoveArm();
                EndMutation();
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawChecks(StudioStyles styles, CreatureRecipe selected)
        {
            if (_isCheckDirty)
            {
                _warnings = CreatureStudioAuthoring.Validate(selected);
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

        void Field(string name, string label, bool isExpanded)
        {
            SerializedProperty property = _window.serialized.FindProperty(name);
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, new GUIContent(label), isExpanded);
            }
        }

        // A structural edit changes the layout under the cursor, so the frame ends there
        static void EndMutation()
        {
            GUIUtility.ExitGUI();
        }
    }
}
