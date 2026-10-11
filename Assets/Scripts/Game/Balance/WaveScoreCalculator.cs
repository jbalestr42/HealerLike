using System.Collections.Generic;
using System.Linq;

// Turns the fights measuring the waves into their scores: their damage against the dummies, how long they survive
// the balance team, and the damage per second of that team against the same dummies
public static class WaveScoreCalculator
{
    // Mean damage per second of the balance team against the dummies, 0 without fight
    public static float GetTeamDps(IReadOnlyCollection<CombatStats> teamFights)
    {
        return teamFights.Count > 0 ? teamFights.Average(fight => fight.allyDps) : 0f;
    }

    // The score of every wave measured both ways, by wave name, without fingerprint
    public static Dictionary<string, WaveScore> Compute(IReadOnlyCollection<CombatStats> dpsFights, IReadOnlyCollection<CombatStats> robustnessFights, float teamDps)
    {
        Dictionary<string, WaveScore> scores = new Dictionary<string, WaveScore>();
        foreach (IGrouping<string, CombatStats> wave in dpsFights.GroupBy(fight => fight.wave))
        {
            List<CombatStats> survived = robustnessFights.Where(fight => fight.wave == wave.Key).ToList();
            if (survived.Count == 0)
            {
                continue;
            }

            float dps = wave.Average(fight => fight.enemyDps);
            float survivalTime = survived.Average(fight => fight.duration);
            scores[wave.Key] = new WaveScore
            {
                dps = dps,
                peakDps = wave.Average(fight => fight.enemyPeakDps),
                survivalTime = survivalTime,
                effectiveHealth = WaveScore.GetEffectiveHealth(survivalTime, teamDps),
                threat = WaveScore.GetThreat(dps, survivalTime),
                timedOut = survived.Exists(fight => fight.timedOut),
            };
        }
        return scores;
    }
}
