using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// The healer's resources and the level of the run in the HUD, each one explained in the popover on hover or tap: the
// mana (what comes back after a fight and a rest), the Devotion (hidden for a character building none) and the
// ascension level with its maluses (hidden at level 0)
public sealed class ToolkitHudStatus : IDisposable
{
    public const string ManaName = "mana-value";
    public const string DevotionName = "devotion-value";
    public const string AscensionName = "ascension-label";
    public const string LitClass = "is-lit";
    public const string DevotionDescription = "Some of your spells give Devotion, others spend it all for a stronger effect."
        + "\nBack to 0 at the start of each fight.";

    ToolkitGameContext _context;
    ToolkitGameView _view;
    readonly List<ToolkitInfoTrigger> _triggers = new List<ToolkitInfoTrigger>();

    public void Init(ToolkitGameContext context, ToolkitGameView view)
    {
        Dispose();
        _context = context;
        _view = view;
        AddTrigger(ManaName, GetManaModel);
        AddTrigger(DevotionName, GetDevotionModel);
        AddTrigger(AscensionName, GetAscensionModel);
    }

    public void Dispose()
    {
        foreach (ToolkitInfoTrigger trigger in _triggers)
        {
            trigger.Dispose();
        }

        _triggers.Clear();
        _context = null;
        _view = null;
    }

    public void Refresh()
    {
        if (_context == null)
        {
            return;
        }

        ResourceAttribute devotion = GetDevotion();
        int devotionMax = devotion != null ? Mathf.RoundToInt(devotion.Max) : 0;
        _view.Show(DevotionName, devotionMax > 0);
        if (devotionMax > 0)
        {
            int value = Mathf.RoundToInt(devotion.Value);
            _view.SetText(DevotionName, GetDevotionText(value, devotionMax));
            _view.root.Q(DevotionName)?.EnableInClassList(LitClass, value > 0);
        }

        int level = GetAscensionLevel();
        _view.Show(AscensionName, level > 0);
        if (level > 0)
        {
            _view.SetText(AscensionName, GetAscensionText(level, _view.isTouchLayout));
        }
    }

    // The count over its max, "✦" standing for the Devotion
    public static string GetDevotionText(int value, int max)
    {
        return $"✦ {value}/{max}";
    }

    // Short on a phone, where the header shares its row with the buttons
    public static string GetAscensionText(int level, bool isShort)
    {
        return isShort ? $"A{level}" : $"Ascension {level}";
    }

    // The level of the run as the settings clamp it, 0 outside a run
    int GetAscensionLevel()
    {
        AscensionSettings settings = _context.data != null ? _context.data.ascension : null;
        RunState run = _context.ascension != null ? _context.ascension.run : null;
        return settings != null && run != null ? settings.ClampLevel(run.ascensionLevel) : 0;
    }

    Character GetCharacter()
    {
        return _context != null && _context.player != null ? _context.player.character : null;
    }

    ResourceAttribute GetDevotion()
    {
        Character character = GetCharacter();
        return character != null ? character.devotion : null;
    }

    ToolkitCardModel GetManaModel()
    {
        Character character = GetCharacter();
        ResourceAttribute mana = character != null ? character.mana : null;
        if (mana == null)
        {
            return null;
        }

        // What comes back between the fights only matters in a run
        string body = _context.ascension != null ? WithoutHeading(AscensionGameType.GetManaTooltip(mana), "Mana") : "";
        return Model(mana, $"Mana {Mathf.RoundToInt(mana.Value)} / {Mathf.RoundToInt(mana.Max)}", body);
    }

    ToolkitCardModel GetDevotionModel()
    {
        ResourceAttribute devotion = GetDevotion();
        if (devotion == null || Mathf.RoundToInt(devotion.Max) <= 0)
        {
            return null;
        }

        return Model(devotion, $"Devotion {Mathf.RoundToInt(devotion.Value)} / {Mathf.RoundToInt(devotion.Max)}",
            DevotionDescription);
    }

    ToolkitCardModel GetAscensionModel()
    {
        int level = GetAscensionLevel();
        if (level <= 0)
        {
            return null;
        }

        AscensionSettings settings = _context.data.ascension;
        return Model(settings, $"Ascension {level}", settings.GetMalusText(level));
    }

    static ToolkitCardModel Model(object source, string title, string body)
    {
        return new ToolkitCardModel
        {
            key = title,
            iconSource = source,
            title = title,
            description = ToolkitRichText.Normalise(ToolkitStatusText.Format(body)),
            source = source
        };
    }

    // The tooltip without its bold first line naming it, the popover's title does
    public static string WithoutHeading(string tooltip, string name)
    {
        string head = "<b>" + name + "</b>";
        return tooltip.StartsWith(head, StringComparison.Ordinal) ? tooltip.Substring(head.Length).TrimStart('\n') : tooltip;
    }

    void AddTrigger(string name, Func<ToolkitCardModel> getModel)
    {
        VisualElement element = _view.root.Q(name);
        if (element != null)
        {
            _triggers.Add(new ToolkitInfoTrigger(_view, element, getModel));
        }
    }
}
