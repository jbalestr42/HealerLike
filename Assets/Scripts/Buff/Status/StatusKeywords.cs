using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

// {status:Vulnerable} in a description: the title of the status, bold and colored (gold for a buff, purple for a debuff),
// wrapped in an invisible link naming it, so the box next to the text can explain every status it cites (see
// StatusKeywordBox). The statuses are found by their title among the ones of the game data
public static class StatusKeywords
{
    public const string PositiveColor = "#FFD25A";
    public const string NegativeColor = "#C88CFF";

    static readonly Regex KeywordRegex = new Regex(@"\{status:([^}]*)\}");
    static readonly Regex LinkRegex = new Regex("<link=\"status:([^\"]*)\">");

    static IReadOnlyList<StatusData> _statuses = new List<StatusData>();
    // The statuses of the game (GameData.statuses), registered when the game data is loaded
    public static IReadOnlyList<StatusData> statuses
    {
        get
        {
#if UNITY_EDITOR
            // Outside of the game (e.g. a description previewed in the inspector, a data test): every status of the project
            if (_statuses.Count == 0)
            {
                _statuses = LoadProjectStatuses();
            }
#endif
            return _statuses;
        }
    }

    public static void Register(IReadOnlyList<StatusData> statuses)
    {
        _statuses = statuses ?? new List<StatusData>();
    }

#if UNITY_EDITOR
    static List<StatusData> LoadProjectStatuses()
    {
        List<StatusData> projectStatuses = new List<StatusData>();
        foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:StatusData"))
        {
            projectStatuses.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<StatusData>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid)));
        }
        return projectStatuses;
    }
#endif

    // The status with this title, null when there is none
    public static StatusData Find(string title, IReadOnlyList<StatusData> statuses)
    {
        if (statuses == null)
        {
            return null;
        }
        foreach (StatusData status in statuses)
        {
            if (status != null && status.title == title)
            {
                return status;
            }
        }
        return null;
    }

    public static string Format(string text)
    {
        return Format(text, statuses);
    }

    // Every {status:X} of the text replaced by its keyword, an unknown status by its bare name
    public static string Format(string text, IReadOnlyList<StatusData> statuses)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "";
        }
        return KeywordRegex.Replace(text, match => FormatStatus(match.Groups[1].Value, statuses));
    }

    public static string FormatStatus(string title)
    {
        return FormatStatus(title, statuses);
    }

    public static string FormatStatus(string title, IReadOnlyList<StatusData> statuses)
    {
        StatusData status = Find(title, statuses);
        if (status == null)
        {
            Debug.LogError($"[StatusKeywords] Unknown status '{title}' in {{status:{title}}}");
            return title;
        }
        return $"<link=\"status:{status.title}\"><b><color={GetColor(status)}>{status.title}</color></b></link>";
    }

    public static string GetColor(StatusData status)
    {
        return status.isPositive ? PositiveColor : NegativeColor;
    }

    public static List<StatusData> GetCitedStatuses(string formattedText)
    {
        return GetCitedStatuses(formattedText, statuses);
    }

    // The statuses a formatted text cites, each once, in the order they first appear
    public static List<StatusData> GetCitedStatuses(string formattedText, IReadOnlyList<StatusData> statuses)
    {
        List<StatusData> cited = new List<StatusData>();
        if (string.IsNullOrEmpty(formattedText))
        {
            return cited;
        }
        foreach (Match match in LinkRegex.Matches(formattedText))
        {
            StatusData status = Find(match.Groups[1].Value, statuses);
            if (status != null && !cited.Contains(status))
            {
                cited.Add(status);
            }
        }
        return cited;
    }

    public static string GetExplanation(string formattedText)
    {
        return GetExplanation(formattedText, statuses);
    }

    // Each status the text cites, its title then its description, apart from each other. Empty when it cites none
    public static string GetExplanation(string formattedText, IReadOnlyList<StatusData> statuses)
    {
        List<string> explanations = new List<string>();
        foreach (StatusData status in GetCitedStatuses(formattedText, statuses))
        {
            string title = $"<b><color={GetColor(status)}>{status.title}</color></b>";
            explanations.Add(string.IsNullOrEmpty(status.description) ? title : $"{title}\n{status.description}");
        }
        return string.Join("\n\n", explanations);
    }
}
