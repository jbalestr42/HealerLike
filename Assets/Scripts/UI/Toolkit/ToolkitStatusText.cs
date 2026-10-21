using System.Collections.Generic;

// The {status:X} keywords of the descriptions shown by UI Toolkit: the title of the status bold, colored and linked,
// as the legacy panels show it (see StatusKeywords), and for the panels with room for it, what each cited status does.
// UI Toolkit labels render the <link>, <b> and <color> tags of the keyword as they are
public static class ToolkitStatusText
{
    const string KeywordStart = "{status:";

    public static string Format(string text)
    {
        return Format(text, StatusKeywords.statuses);
    }

    // Every {status:X} of the text replaced by its keyword; a text citing none comes back as it is
    public static string Format(string text, IReadOnlyList<StatusData> statuses)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }
        if (!text.Contains(KeywordStart))
        {
            return text;
        }
        return StatusKeywords.Format(text, statuses);
    }

    public static string WithExplanation(string text)
    {
        return WithExplanation(text, StatusKeywords.statuses);
    }

    // The formatted text, then the title and description of each status it cites, apart from it
    public static string WithExplanation(string text, IReadOnlyList<StatusData> statuses)
    {
        string formatted = Format(text, statuses);
        string explanation = StatusKeywords.GetExplanation(formatted, statuses);
        return explanation.Length == 0 ? formatted : formatted + "\n\n" + explanation;
    }
}
