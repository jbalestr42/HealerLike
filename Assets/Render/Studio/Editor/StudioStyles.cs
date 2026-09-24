using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Studio.Editor
{
    // The studio windows' dark theme: its colours, four text styles and the library card, built on the first OnGUI
    public class StudioStyles
    {
        public static readonly Color Background = new Color(0.055f, 0.065f, 0.083f);
        public static readonly Color Panel = new Color(0.085f, 0.098f, 0.12f);
        public static readonly Color Divider = new Color(0.17f, 0.2f, 0.24f);

        Color _accent;
        Color _highlight;
        GUIStyle _title;
        GUIStyle _section;
        GUIStyle _small;
        GUIStyle _card;

        public Color accent { get { return _accent; } }

        public GUIStyle title { get { return _title; } }

        public GUIStyle section { get { return _section; } }

        public GUIStyle small { get { return _small; } }

        public GUIStyle card { get { return _card; } }

        public bool isReady { get { return _title != null; } }

        // accent colours section titles and the selected card's edge, highlight fills the selected card
        public void Init(Color accent, Color highlight, Color smallText, Color cardText)
        {
            _accent = accent;
            _highlight = highlight;
            _title = new GUIStyle(EditorStyles.boldLabel);
            _title.fontSize = 22;
            _title.normal.textColor = Color.white;
            _section = new GUIStyle(EditorStyles.boldLabel);
            _section.fontSize = 10;
            _section.normal.textColor = accent;
            _small = new GUIStyle(EditorStyles.label);
            _small.fontSize = 10;
            _small.wordWrap = true;
            _small.normal.textColor = smallText;
            _card = new GUIStyle(EditorStyles.label);
            _card.padding = new RectOffset(12, 8, 6, 6);
            _card.fontSize = 12;
            _card.normal.textColor = cardText;
        }

        // One library row, a diamond for a saved asset and a dot for a draft; true when clicked
        public bool DrawCard(string label, bool isSelected, bool isSaved)
        {
            Rect rect = GUILayoutUtility.GetRect(10f, 33f, GUILayout.ExpandWidth(true));
            if (isSelected)
            {
                EditorGUI.DrawRect(rect, _highlight);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), _accent);
            }

            string marker = "·  ";
            if (isSaved)
            {
                marker = "◇  ";
            }
            return GUI.Button(rect, marker + label, _card);
        }

        public void Section(string label)
        {
            GUILayout.Space(16f);
            GUILayout.Label(label, _section);
            GUILayout.Space(5f);
        }

        // Fills the window and draws the header rule
        public static void DrawBackground(Rect position)
        {
            EditorGUI.DrawRect(new Rect(0f, 0f, position.width, position.height), Background);
            EditorGUI.DrawRect(new Rect(10f, 69f, position.width - 20f, 1f), Divider);
        }
    }
}
