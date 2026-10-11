using System.Collections.Generic;
using System.Linq;

// Turns the fights measuring the starting teams of the characters into their scores: their damage against the
// dummies and how long they survive the balance team, without then with the skills of the character cast by its
// healer bot, and the damage per second of that team against the same dummies
public static class CharacterScoreCalculator
{
    // The score of every character measured the four ways, by character title, without fingerprint
    public static Dictionary<string, CharacterScore> Compute(IReadOnlyCollection<CombatStats> dpsFights, IReadOnlyCollection<CombatStats> robustnessFights, IReadOnlyCollection<CombatStats> spellDpsFights, IReadOnlyCollection<CombatStats> spellRobustnessFights, float teamDps)
    {
        Dictionary<string, CharacterScore> scores = new Dictionary<string, CharacterScore>();
        foreach (IGrouping<string, CombatStats> character in dpsFights.GroupBy(fight => fight.character))
        {
            List<CombatStats> survived = robustnessFights.Where(fight => fight.character == character.Key).ToList();
            List<CombatStats> spellDealt = spellDpsFights.Where(fight => fight.character == character.Key).ToList();
            List<CombatStats> spellSurvived = spellRobustnessFights.Where(fight => fight.character == character.Key).ToList();
            if (survived.Count == 0 || spellDealt.Count == 0 || spellSurvived.Count == 0)
            {
                continue;
            }

            float dps = character.Average(fight => fight.allyDps);
            float survivalTime = survived.Average(fight => fight.duration);
            float spellDps = spellDealt.Average(fight => fight.allyDps);
            float spellSurvivalTime = spellSurvived.Average(fight => fight.duration);
            scores[character.Key] = new CharacterScore
            {
                dps = dps,
                survivalTime = survivalTime,
                effectiveHealth = WaveScore.GetEffectiveHealth(survivalTime, teamDps),
                threat = WaveScore.GetThreat(dps, survivalTime),
                healingPerSecond = spellSurvived.Average(fight => fight.duration > 0f ? fight.characterHeal / fight.duration : 0f),
                manaPerSecond = spellSurvived.Average(fight => fight.duration > 0f ? fight.manaSpent / fight.duration : 0f),
                spellSurvivalTime = spellSurvivalTime,
                spellDps = spellDps,
                spellEffectiveHealth = WaveScore.GetEffectiveHealth(spellSurvivalTime, teamDps),
                spellThreat = WaveScore.GetThreat(spellDps, spellSurvivalTime),
                timedOut = survived.Exists(fight => fight.timedOut) || spellSurvived.Exists(fight => fight.timedOut),
            };
        }
        return scores;
    }
}
