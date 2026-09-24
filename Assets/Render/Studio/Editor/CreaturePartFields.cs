using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Studio.Editor
{
    // The selected part's fields in the parts inspector
    public static class CreaturePartFields
    {
        static readonly string[] partFields =
        {
            "id", "parent", "primitive", "localPosition", "localEuler", "dimensions", "colour", "glow", "role",
            "variant"
        };

        // The part's fields, the parent as a popup of the parts before it; the root's parent stays -1
        public static bool Draw(SerializedProperty parts, int selectedPart)
        {
            SerializedProperty part = parts.GetArrayElementAtIndex(selectedPart);
            EditorGUI.BeginChangeCheck();
            foreach (string name in partFields)
            {
                if (name != "parent")
                {
                    EditorGUILayout.PropertyField(part.FindPropertyRelative(name));
                    continue;
                }

                SerializedProperty parent = part.FindPropertyRelative(name);
                if (selectedPart == 0)
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.TextField("Parent", "Root (−1)");
                    }

                    if (parent.intValue != -1)
                    {
                        parent.intValue = -1;
                        GUI.changed = true;
                    }
                    continue;
                }

                string[] parents = new string[selectedPart];
                for (int i = 0; i < selectedPart; i++)
                {
                    parents[i] = i + "  " + parts.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue;
                }
                parent.intValue = EditorGUILayout.Popup("Parent", parent.intValue, parents);
            }

            return EditorGUI.EndChangeCheck();
        }
    }
}
