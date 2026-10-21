using System;
using System.Globalization;
using System.Text;

// Turns the TextMesh Pro rich text of the legacy panels (EntityInfoFormatter, CharacterInfoPanel, EventView, the
// skill descriptions in the data assets) into markup a UI Toolkit label renders. UI Toolkit's tag set is a superset
// of what those panels emit, so most tags pass through byte for byte; the work is the cases where the two differ or
// where the input is not well formed:
//   - <size=22> and <size=22,5>: a bare number gets "px" (and a comma decimal, which string interpolation produces
//     under a French culture, becomes a dot), so the unit never depends on a default we cannot check
//   - <size=+6>: a relative size passes through (the documentation lists "+1" and "-1" as relative pixel sizes);
//     when the caller gives a baseFontSize it is resolved to an absolute pixel size instead, the fallback if a
//     device shows that relative sizes do not render
//   - <color=green> gets its quotes, <color="#FF0000"> loses them, the two forms the documentation shows
//   - malformed known tags (<size=big>, <color=#12>, <b=1>) and tags UI Toolkit lacks (<page>, <material>, <scale>)
//     are removed with the text inside kept
//   - unclosed tags are closed at the end and stray or misnested closers are repaired, so a style cannot bleed into
//     whatever the caller appends next
//   - anything that merely looks like a tag (<Skeleton>, "HP < 5 and 3 > 1") is content and is left alone
//   - spaces that open a line, including ones behind opening tags ("<color=#9AA3B2>    +2 · Hexer"), become U+00A0,
//     because a label with white-space: normal collapses plain spaces and TextMesh Pro did not. Spaces inside a
//     line are never touched. On by default, PlainLeadingSpaces turns it off.
//   - StripSizes removes every size tag in any form, text kept, for a caller that styles the element itself
// A string with nothing to change comes back as the same instance, without allocating. Tag names are matched
// ignoring case. Not thread safe: it shares two scratch builders, which is fine on the UI thread.
[Flags]
public enum ToolkitRichTextOptions
{
    None = 0,
    // Remove every <size...> and </size>, whatever its form, and keep the text inside
    StripSizes = 1,
    // Leave spaces that open a line as plain spaces
    PlainLeadingSpaces = 2,
}

public static class ToolkitRichText
{
    enum TagResult
    {
        Content,
        Keep,
        Replace,
    }

    const int MaxDepth = 16;

    // Tags that open a scope the normaliser tracks and closes. Order is the id pushed on the open stack.
    static readonly string[] PairedNames = { "b", "i", "u", "s", "color", "size", "mark", "sup", "sub" };
    static readonly int ColorId = 4;
    static readonly int SizeId = 5;
    static readonly int MarkId = 6;

    // Tags UI Toolkit documents that are passed through untouched and untracked
    static readonly string[] PassNames =
    {
        "a", "align", "allcaps", "alpha", "br", "cspace", "font", "font-weight", "gradient", "indent", "line-height",
        "line-indent", "link", "lowercase", "margin", "mspace", "nobr", "noparse", "pos", "rotate", "smallcaps",
        "space", "sprite", "style", "uppercase", "voffset", "width",
    };

    // TextMesh Pro tags missing from the UI Toolkit list: removed, their text kept
    static readonly string[] StripNames = { "page", "material", "scale" };

    static readonly char NoBreakSpace = '\u00A0';

    static readonly StringBuilder _builder = new StringBuilder();
    static readonly StringBuilder _scratch = new StringBuilder();

    // baseFontSize: the font size of the target label in pixels. 0 leaves relative sizes (<size=+6>) as they are.
    public static string Normalise(string text, float baseFontSize, ToolkitRichTextOptions options)
    {
        if (text == null)
        {
            return string.Empty;
        }
        bool strip = (options & ToolkitRichTextOptions.StripSizes) != 0;
        bool spaces = (options & ToolkitRichTextOptions.PlainLeadingSpaces) == 0;
        int i = text.IndexOf('<');
        if (i < 0 && !spaces)
        {
            return text;
        }

        Span<int> open = stackalloc int[MaxDepth];
        int depth = 0;
        StringBuilder builder = null;
        int flushed = 0;
        int scanned = 0;
        bool atLineStart = true;

        while (i >= 0 && i < text.Length)
        {
            int close = text.IndexOf('>', i + 1);
            if (close < 0)
            {
                break;
            }
            // "<<b>": the first bracket is content
            int inner = text.IndexOf('<', i + 1, close - i - 1);
            if (inner >= 0)
            {
                i = inner;
                continue;
            }

            _scratch.Clear();
            ReadOnlySpan<char> body = text.AsSpan(i + 1, close - i - 1);
            TagResult result = TryRewrite(body, baseFontSize, strip, open, ref depth, _scratch);
            if (result != TagResult.Content)
            {
                // A tag does not end the line start, so "<color=#9AA3B2>    x" still has a leading indent
                if (spaces)
                {
                    ScanContent(text, scanned, i, ref atLineStart, ref builder, ref flushed);
                }
                scanned = close + 1;
                if (result == TagResult.Keep)
                {
                    // <br> starts a line, a sprite is content: neither is a plain tag as far as indentation goes
                    if (MemoryExtensions.Equals(body, "br".AsSpan(), StringComparison.OrdinalIgnoreCase))
                    {
                        atLineStart = true;
                    }
                    else if (body.StartsWith("sprite".AsSpan(), StringComparison.OrdinalIgnoreCase))
                    {
                        atLineStart = false;
                    }
                }
                if (result == TagResult.Replace)
                {
                    if (builder == null)
                    {
                        builder = _builder;
                        builder.Clear();
                    }
                    builder.Append(text, flushed, i - flushed).Append(_scratch);
                    flushed = close + 1;
                }
            }
            i = close + 1 < text.Length ? text.IndexOf('<', close + 1) : -1;
        }
        if (spaces)
        {
            ScanContent(text, scanned, text.Length, ref atLineStart, ref builder, ref flushed);
        }

        if (builder == null && depth == 0)
        {
            return text;
        }
        if (builder == null)
        {
            builder = _builder;
            builder.Clear();
        }
        builder.Append(text, flushed, text.Length - flushed);
        for (int k = depth - 1; k >= 0; k--)
        {
            builder.Append("</").Append(PairedNames[open[k]]).Append('>');
        }
        return builder.ToString();
    }

    public static string Normalise(string text, float baseFontSize = 0f)
    {
        return Normalise(text, baseFontSize, ToolkitRichTextOptions.None);
    }

    public static string Normalise(string text, ToolkitRichTextOptions options)
    {
        return Normalise(text, 0f, options);
    }

    // Turns the spaces that open a line into non-breaking spaces. Tags do not count as content, so a space behind
    // an opening tag still opens the line. <br> opens a line and <sprite> counts as text.
    static void ScanContent(string text, int from, int to, ref bool atLineStart, ref StringBuilder builder, ref int flushed)
    {
        for (int k = from; k < to; k++)
        {
            char c = text[k];
            if (c != ' ')
            {
                atLineStart = c == '\n';
                continue;
            }
            if (!atLineStart)
            {
                continue;
            }
            if (builder == null)
            {
                builder = _builder;
                builder.Clear();
            }
            builder.Append(text, flushed, k - flushed).Append(NoBreakSpace);
            flushed = k + 1;
        }
    }

    // Replace: the tag becomes what is in output (nothing means removed). Keep: a real tag that stays as written.
    // Content: something that only looks like a tag, left alone and counted as text.
    static TagResult TryRewrite(ReadOnlySpan<char> body, float baseFontSize, bool strip, Span<int> open, ref int depth, StringBuilder output)
    {
        bool closing = body.Length > 0 && body[0] == '/';
        ReadOnlySpan<char> rest = closing ? body.Slice(1) : body;
        if (rest.Length == 0)
        {
            return TagResult.Content;
        }

        // <#RRGGBB>, the short color form
        if (!closing && rest[0] == '#')
        {
            if (!IsHex(rest.Slice(1)))
            {
                return TagResult.Content;
            }
            return Push(ColorId, open, ref depth) ? TagResult.Keep : TagResult.Replace;
        }

        int equals = rest.IndexOf('=');
        bool hasValue = equals >= 0;
        ReadOnlySpan<char> name = hasValue ? rest.Slice(0, equals) : rest;
        ReadOnlySpan<char> value = hasValue ? rest.Slice(equals + 1) : default;

        int paired = IndexOfName(PairedNames, name);
        if (paired >= 0)
        {
            if (strip && paired == SizeId)
            {
                return TagResult.Replace;
            }
            bool replace = closing
                ? Close(paired, hasValue, open, ref depth, output)
                : Open(paired, hasValue, value, baseFontSize, open, ref depth, output);
            return replace ? TagResult.Replace : TagResult.Keep;
        }
        if (IndexOfName(StripNames, name) >= 0)
        {
            return TagResult.Replace;
        }
        return IndexOfName(PassNames, name) >= 0 ? TagResult.Keep : TagResult.Content;
    }

    static bool Open(int id, bool hasValue, ReadOnlySpan<char> value, float baseFontSize, Span<int> open, ref int depth, StringBuilder output)
    {
        bool valid;
        bool changed = false;
        if (id == ColorId)
        {
            valid = hasValue && TryNormaliseColor("color", value, output, out changed);
        }
        else if (id == MarkId)
        {
            valid = hasValue && TryNormaliseColor("mark", value, output, out changed);
        }
        else if (id == SizeId)
        {
            valid = hasValue && TryNormaliseSize(value, baseFontSize, output, out changed);
        }
        else
        {
            valid = !hasValue;
        }

        if (!valid || depth >= open.Length)
        {
            output.Clear();
            return true;
        }
        open[depth] = id;
        depth++;
        return changed;
    }

    static bool Push(int id, Span<int> open, ref int depth)
    {
        if (depth >= open.Length)
        {
            return false;
        }
        open[depth] = id;
        depth++;
        return true;
    }

    // Closes the nearest open tag of this kind, first closing whatever was opened after it. A closer with no
    // opener is removed.
    static bool Close(int id, bool hasValue, Span<int> open, ref int depth, StringBuilder output)
    {
        int found = -1;
        for (int k = depth - 1; k >= 0; k--)
        {
            if (open[k] == id)
            {
                found = k;
                break;
            }
        }
        if (hasValue || found < 0)
        {
            return true;
        }
        if (found == depth - 1)
        {
            depth--;
            return false;
        }
        for (int k = depth - 1; k > found; k--)
        {
            output.Append("</").Append(PairedNames[open[k]]).Append('>');
        }
        output.Append("</").Append(PairedNames[id]).Append('>');
        depth = found;
        return true;
    }

    // color and mark take "#RGB", "#RGBA", "#RRGGBB", "#RRGGBBAA" or a name; the documentation shows names quoted and
    // hex unquoted, so the value is rewritten to that form.
    static bool TryNormaliseColor(string tag, ReadOnlySpan<char> value, StringBuilder output, out bool changed)
    {
        changed = false;
        bool quoted = value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"';
        ReadOnlySpan<char> inner = quoted ? value.Slice(1, value.Length - 2) : value;
        if (inner.Length == 0)
        {
            return false;
        }

        if (inner[0] == '#')
        {
            if (!IsHex(inner.Slice(1)))
            {
                return false;
            }
            if (quoted)
            {
                changed = true;
                output.Append('<').Append(tag).Append('=').Append(inner).Append('>');
            }
            return true;
        }

        for (int k = 0; k < inner.Length; k++)
        {
            if (!IsAsciiLetter(inner[k]))
            {
                return false;
            }
        }
        if (!quoted)
        {
            changed = true;
            output.Append('<').Append(tag).Append("=\"").Append(inner).Append("\">");
        }
        return true;
    }

    // size takes [+-]number with an optional px, em or %. A bare unsigned number is pixels and gets "px"; a bare
    // signed number is a relative pixel size, kept as it is unless baseFontSize is given.
    static bool TryNormaliseSize(ReadOnlySpan<char> value, float baseFontSize, StringBuilder output, out bool changed)
    {
        changed = false;
        bool quoted = value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"';
        ReadOnlySpan<char> text = quoted ? value.Slice(1, value.Length - 2) : value;

        int index = 0;
        char sign = '\0';
        if (text.Length > 0 && (text[0] == '+' || text[0] == '-'))
        {
            sign = text[0];
            index = 1;
        }
        int numberStart = index;
        float number = 0f;
        float place = 1f;
        bool seenSeparator = false;
        bool hasComma = false;
        int digits = 0;
        while (index < text.Length)
        {
            char c = text[index];
            if (c >= '0' && c <= '9')
            {
                digits++;
                if (seenSeparator)
                {
                    place *= 0.1f;
                    number += (c - '0') * place;
                }
                else
                {
                    number = number * 10f + (c - '0');
                }
            }
            else if ((c == '.' || c == ',') && !seenSeparator)
            {
                seenSeparator = true;
                hasComma = c == ',';
            }
            else
            {
                break;
            }
            index++;
        }
        if (digits == 0 || text[index - 1] == '.' || text[index - 1] == ',')
        {
            return false;
        }

        int numberEnd = index;
        ReadOnlySpan<char> suffix = text.Slice(index);
        bool hasSuffix = suffix.Length > 0;
        if (hasSuffix
            && !MemoryExtensions.Equals(suffix, "px".AsSpan(), StringComparison.OrdinalIgnoreCase)
            && !MemoryExtensions.Equals(suffix, "em".AsSpan(), StringComparison.OrdinalIgnoreCase)
            && !MemoryExtensions.Equals(suffix, "%".AsSpan(), StringComparison.Ordinal))
        {
            return false;
        }

        bool resolve = sign != '\0' && !hasSuffix && baseFontSize > 0f;
        bool addUnit = sign == '\0' && !hasSuffix;
        changed = quoted || hasComma || resolve || addUnit;
        if (!changed)
        {
            return true;
        }

        output.Append("<size=");
        if (resolve)
        {
            float size = baseFontSize + (sign == '-' ? -number : number);
            if (size < 1f)
            {
                size = 1f;
            }
            output.Append(size.ToString("0.##", CultureInfo.InvariantCulture)).Append("px");
        }
        else
        {
            if (sign != '\0')
            {
                output.Append(sign);
            }
            for (int k = numberStart; k < numberEnd; k++)
            {
                output.Append(text[k] == ',' ? '.' : text[k]);
            }
            if (hasSuffix)
            {
                output.Append(suffix);
            }
            else
            {
                output.Append("px");
            }
        }
        output.Append('>');
        return true;
    }

    static int IndexOfName(string[] names, ReadOnlySpan<char> name)
    {
        for (int k = 0; k < names.Length; k++)
        {
            if (MemoryExtensions.Equals(name, names[k].AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                return k;
            }
        }
        return -1;
    }

    static bool IsHex(ReadOnlySpan<char> digits)
    {
        if (digits.Length != 3 && digits.Length != 4 && digits.Length != 6 && digits.Length != 8)
        {
            return false;
        }
        for (int k = 0; k < digits.Length; k++)
        {
            char c = digits[k];
            bool hex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
            if (!hex)
            {
                return false;
            }
        }
        return true;
    }

    static bool IsAsciiLetter(char c)
    {
        return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
    }
}
