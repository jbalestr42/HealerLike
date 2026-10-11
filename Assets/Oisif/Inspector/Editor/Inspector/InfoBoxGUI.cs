using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Oisif.Editor
{
    // The box of an [InfoBox]: its text drawn as rich text, the way the game shows it (colors, bold...)
    public static class InfoBoxGUI
    {
        // A quoted tag value, e.g. <color="red">, which TextMeshPro reads but not the rich text of the editor
        static readonly Regex QuotedTagValue = new Regex(@"<(\w+)=""([^""<>]*)"">");
        static GUIStyle _style;

        static GUIStyle style => _style ?? (_style = new GUIStyle(EditorStyles.helpBox) { richText = true, wordWrap = true, fontSize = EditorStyles.label.fontSize, padding = new RectOffset(6, 6, 4, 4) });

        public static void Draw(string text)
        {
            GUILayout.Label(ToEditorRichText(text), style);
        }

        // The text with its tag values unquoted, <color="red"> becoming <color=red>
        public static string ToEditorRichText(string text)
        {
            return QuotedTagValue.Replace(text, "<$1=$2>");
        }
    }
}
