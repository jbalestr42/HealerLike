using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

// What the simulated fights say about balance: how hard each wave is on each floor, where it fits (every bot
// wins, spending about the mana targeted for its room type), and how each character copes
public class BalanceReport
{
    // Parts of the max mana a fight should take, by room type (40% of it comes back before each fight)
    public struct ManaTargets
    {
        public float combatMin;
        public float combatMax;
        // Elites and bosses
        public float eliteMin;
        public float eliteMax;
    }

    // A normal fight 30% to 50% of the mana, an elite or a boss 60% to all of it
    public static readonly ManaTargets DefaultManaTargets = new ManaTargets { combatMin = 0.3f, combatMax = 0.5f, eliteMin = 0.6f, eliteMax = 1f };
    // Set from the Difficulty tab of the Balance Report
    public static ManaTargets manaTargets = DefaultManaTargets;

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

    // Range of mana spent that fits the room type
    public static (float min, float max) GetManaTargetRange(string roomType)
    {
        bool isElite = roomType == MapNodeType.Elite.ToString() || roomType == MapNodeType.Boss.ToString();
        return isElite ? (manaTargets.eliteMin, manaTargets.eliteMax) : (manaTargets.combatMin, manaTargets.combatMax);
    }

    // Middle of the range of the room type
    public static float GetManaTarget(string roomType)
    {
        (float min, float max) = GetManaTargetRange(roomType);
        return (min + max) / 2f;
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

    // How far the mana spent is out of the range of the room type: 0 within it, negative when the fight is too easy
    public static float GetManaGap(Cell cell, string roomType)
    {
        (float min, float max) = GetManaTargetRange(roomType);
        if (cell.manaSpent < min)
        {
            return cell.manaSpent - min;
        }
        if (cell.manaSpent > max)
        {
            return cell.manaSpent - max;
        }
        return 0f;
    }

    // Every bot won, spending the mana targeted
    public static bool Fits(Cell cell, string roomType)
    {
        return cell.allWon && GetManaGap(cell, roomType) == 0f;
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
