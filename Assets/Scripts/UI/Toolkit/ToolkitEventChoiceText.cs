using System;
using System.Text.RegularExpressions;

// Reads what Julien's EventView bakes into a choice button: the button object is named "Choice <label>", and its
// text is "<b>label</b>", then for a described choice a newline and "<size=75%>description</size>".
// Pure string work, so the format dependency sits in one place and the tests can round trip his own formatter.
public static class ToolkitEventChoiceText
{
    public static readonly string NamePrefix = "Choice ";

    const string SizeOpen = "<size=75%>";
    const string SizeClose = "</size>";

    static readonly Regex _anyTag = new Regex("<[^>]*>");

    // The choice's label: the part of the object name after the prefix, else the first line of the text
    public static string Label(string objectName, string text)
    {
        if (!string.IsNullOrEmpty(objectName)
            && objectName.StartsWith(NamePrefix, StringComparison.Ordinal)
            && objectName.Length > NamePrefix.Length)
        {
            return objectName.Substring(NamePrefix.Length);
        }

        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        int newline = text.IndexOf('\n');
        string first = newline >= 0 ? text.Substring(0, newline) : text;
        return _anyTag.Replace(first, string.Empty).Trim();
    }

    // The description under the label, plain of his 75% wrapper and of tags the Toolkit label cannot draw
    public static string Description(string label, string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        string body = text;
        string boldLine = "<b>" + label + "</b>";
        if (body.StartsWith(boldLine, StringComparison.Ordinal))
        {
            body = body.Substring(boldLine.Length);
        }
        else
        {
            // His format moved: drop a first line that is the label once stripped, keep everything otherwise
            int newline = body.IndexOf('\n');
            string first = newline >= 0 ? body.Substring(0, newline) : body;
            if (_anyTag.Replace(first, string.Empty).Trim() == label)
            {
                body = newline >= 0 ? body.Substring(newline + 1) : string.Empty;
            }
        }

        body = body.TrimStart('\r', '\n');
        if (body.StartsWith(SizeOpen, StringComparison.Ordinal) && body.EndsWith(SizeClose, StringComparison.Ordinal))
        {
            body = body.Substring(SizeOpen.Length, body.Length - SizeOpen.Length - SizeClose.Length);
        }

        return Normalise(body).Trim();
    }

    // The one seam to the shared rich text normaliser. Sizes are dropped: the card and the screen labels take their
    // size from the theme, and his relative <size=+6> heading would otherwise outgrow the card it sits in.
    public static string Normalise(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return ToolkitRichText.Normalise(text, ToolkitRichTextOptions.StripSizes);
    }
}
