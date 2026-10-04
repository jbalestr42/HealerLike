using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

// What the simulated fights say about balance: how hard each wave is on each floor, where it fits (every bot
// wins, spending about the mana targeted for its room type), and how each character copes
public class BalanceReport
{
    // Part of the max mana a fight should take: a normal fight about 60%, an elite or a boss all of it
    public const float CombatManaTarget = 0.6f;
    public const float EliteManaTarget = 1f;
    public const float ManaTolerance = 0.15f;

    // Fights of a wave on a floor (or of a character), averaged
    public class Cell
    {
        public int fights;
        public int wins;
        public int timeouts;
        // Parts of the max mana spent and of the max health of the allies lost
        public float manaSpent;
        public float healthLost;
        public float deaths;
        public float duration;

        public float winRate => fights > 0 ? (float)wins / fights : 0f;
        public bool allWon => fights > 0 && wins == fights;

        public static Cell Average(IReadOnlyCollection<CombatStats> fights)
        {
            Cell cell = new Cell { fights = fights.Count };
            if (fights.Count == 0)
            {
                return cell;
            }

            foreach (CombatStats fight in fights)
            {
                cell.wins += fight.won ? 1 : 0;
                cell.timeouts += fight.timedOut ? 1 : 0;
                cell.manaSpent += fight.manaMax > 0f ? fight.manaSpent / fight.manaMax : 0f;
                cell.healthLost += fight.allyHealthMax > 0f ? Mathf.Max(0f, fight.allyHealthStart - fight.allyHealthEnd) / fight.allyHealthMax : 0f;
                cell.deaths += fight.deadAllies.Count;
                cell.duration += fight.duration;
            }
            cell.manaSpent /= fights.Count;
            cell.healthLost /= fights.Count;
            cell.deaths /= fights.Count;
            cell.duration /= fights.Count;
            return cell;
        }
    }

    readonly List<CombatStats> _fights;

    public IReadOnlyList<CombatStats> fights => _fights;
    // In the order of their first fight
    public IReadOnlyList<string> waves { get; }
    public IReadOnlyList<string> bots { get; }
    public IReadOnlyList<int> floors { get; }

    public BalanceReport(IEnumerable<CombatStats> fights)
    {
        _fights = fights.Where(fight => fight != null).ToList();
        waves = _fights.Select(fight => fight.wave).Distinct().ToList();
        bots = _fights.Select(fight => fight.bot).Distinct().ToList();
        floors = _fights.Select(fight => fight.floor).Distinct().OrderBy(floor => floor).ToList();
    }

    // One fight per line of a combat log, blank lines skipped
    public static List<CombatStats> Parse(IEnumerable<string> lines)
    {
        List<CombatStats> fights = new List<CombatStats>();
        foreach (string line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                fights.Add(JsonUtility.FromJson<CombatStats>(line));
            }
        }
        return fights;
    }

    public static float GetManaTarget(string roomType)
    {
        return roomType == MapNodeType.Elite.ToString() || roomType == MapNodeType.Boss.ToString() ? EliteManaTarget : CombatManaTarget;
    }

    // Room type the wave was played in
    public string GetRoomType(string wave)
    {
        CombatStats fight = _fights.Find(stats => stats.wave == wave);
        return fight != null ? fight.roomType : null;
    }

    // Every fight of the wave on the floor, of one bot or of all of them (null)
    public Cell GetCell(string wave, int floor, string bot = null)
    {
        return Cell.Average(_fights.FindAll(fight => fight.wave == wave && fight.floor == floor && (bot == null || fight.bot == bot)));
    }

    // Every fight of the bot in a room type, on one floor or on all of them (null)
    public Cell GetBotCell(string bot, string roomType, int? floor = null)
    {
        return Cell.Average(_fights.FindAll(fight => fight.bot == bot && fight.roomType == roomType && (floor == null || fight.floor == floor)));
    }

    // How far the mana spent is from the target of the room type, negative when the fight is too easy
    public static float GetManaGap(Cell cell, string roomType)
    {
        return cell.manaSpent - GetManaTarget(roomType);
    }

    // Every bot won, spending about the mana targeted
    public static bool Fits(Cell cell, string roomType)
    {
        return cell.allWon && Mathf.Abs(GetManaGap(cell, roomType)) <= ManaTolerance;
    }

    // The floors where the wave fits, for every bot together
    public List<int> GetRecommendedFloors(string wave)
    {
        string roomType = GetRoomType(wave);
        return floors.Where(floor =>
        {
            Cell cell = GetCell(wave, floor);
            return cell.fights > 0 && Fits(cell, roomType);
        }).ToList();
    }

    // "3-5, 7": the floors as ranges, "-" without any
    public static string FormatFloors(IReadOnlyList<int> floors)
    {
        if (floors.Count == 0)
        {
            return "-";
        }

        List<int> sorted = floors.OrderBy(floor => floor).ToList();
        StringBuilder text = new StringBuilder();
        int start = sorted[0];
        for (int i = 1; i <= sorted.Count; i++)
        {
            if (i < sorted.Count && sorted[i] == sorted[i - 1] + 1)
            {
                continue;
            }

            int end = sorted[i - 1];
            if (text.Length > 0)
            {
                text.Append(", ");
            }
            text.Append(start == end ? start.ToString() : $"{start}-{end}");
            if (i < sorted.Count)
            {
                start = sorted[i];
            }
        }
        return text.ToString();
    }
}
