using System.Collections.Generic;
using System.Linq;

// Turns the fights measuring the starting teams of the characters into their scores: their damage against the
// dummies, how long they survive the balance team without and with the skills of the character, and the damage
// per second of that team against the same dummies
public static class CharacterScoreCalculator
{
    // The score of every character measured the three ways, by character title, without fingerprint
    public static Dictionary<string, CharacterScore> Compute(IReadOnlyCollection<CombatStats> dpsFights, IReadOnlyCollection<CombatStats> robustnessFights, IReadOnlyCollection<CombatStats> healedFights, float teamDps)
    {
        Dictionary<string, CharacterScore> scores = new Dictionary<string, CharacterScore>();
        foreach (IGrouping<string, CombatStats> character in dpsFights.GroupBy(fight => fight.character))
        {
            List<CombatStats> survived = robustnessFights.Where(fight => fight.character == character.Key).ToList();
            List<CombatStats> healed = healedFights.Where(fight => fight.character == character.Key).ToList();
            if (survived.Count == 0 || healed.Count == 0)
            {
                continue;
            }

            float survivalTime = survived.Average(fight => fight.duration);
            float healedSurvivalTime = healed.Average(fight => fight.duration);
            float dps = character.Average(fight => fight.allyDps);
            scores[character.Key] = new CharacterScore
            {
                dps = dps,
                threat = WaveScore.GetThreat(dps, survivalTime),
                healedThreat = WaveScore.GetThreat(dps, healedSurvivalTime),
                survivalTime = survivalTime,
                effectiveHealth = WaveScore.GetEffectiveHealth(survivalTime, teamDps),
                healedSurvivalTime = healedSurvivalTime,
                healedEffectiveHealth = WaveScore.GetEffectiveHealth(healedSurvivalTime, teamDps),
                healingPerSecond = healed.Average(fight => fight.duration > 0f ? fight.characterHeal / fight.duration : 0f),
                manaPerSecond = healed.Average(fight => fight.duration > 0f ? fight.manaSpent / fight.duration : 0f),
                timedOut = survived.Exists(fight => fight.timedOut) || healed.Exists(fight => fight.timedOut),
            };
        }
        return scores;
    }
}
