using System.Collections.Generic;
using System.Linq;

// Turns the fights measuring the reward items one by one into their scores: the damage of the balance team against
// the dummies with the item on one of its units, and how long the dummies survive the balance team with the item
// on one of them, each fight compared with the fight without item of the same seed
public static class ItemScoreCalculator
{
    // The score of every item measured both ways, by item name, without fingerprint. baseline: the same measures
    // without item, its gains at 0
    public static Dictionary<string, ItemScore> Compute(IReadOnlyCollection<CombatStats> dpsFights, IReadOnlyCollection<CombatStats> robustnessFights, out ItemScore baseline)
    {
        List<CombatStats> dpsBaseline = dpsFights.Where(IsWithoutItem).ToList();
        List<CombatStats> robustnessBaseline = robustnessFights.Where(IsWithoutItem).ToList();
        float teamDps = dpsBaseline.Count > 0 ? dpsBaseline.Average(fight => fight.allyDps) : 0f;
        float baselineSurvival = robustnessBaseline.Count > 0 ? robustnessBaseline.Average(fight => fight.duration) : 0f;
        Dictionary<int, float> dpsBySeed = BySeed(dpsBaseline, fight => fight.allyDps);
        Dictionary<int, float> survivalBySeed = BySeed(robustnessBaseline, fight => fight.duration);
        float baselineEffectiveHealth = WaveScore.GetEffectiveHealth(baselineSurvival, teamDps);
        baseline = new ItemScore
        {
            dps = teamDps,
            survivalTime = baselineSurvival,
            effectiveHealth = baselineEffectiveHealth,
            power = WaveScore.GetThreat(teamDps, baselineSurvival),
            timedOut = robustnessBaseline.Exists(fight => fight.timedOut),
        };

        Dictionary<string, ItemScore> scores = new Dictionary<string, ItemScore>();
        foreach (IGrouping<string, CombatStats> item in dpsFights.Where(fight => !IsWithoutItem(fight)).GroupBy(fight => fight.item))
        {
            List<CombatStats> survived = robustnessFights.Where(fight => fight.item == item.Key).ToList();
            if (survived.Count == 0)
            {
                continue;
            }

            float dpsGain = item.Average(fight => fight.allyDps - Get(dpsBySeed, fight.seed, teamDps));
            float survivalTime = survived.Average(fight => fight.duration);
            float survivalGain = survived.Average(fight => fight.duration - Get(survivalBySeed, fight.seed, baselineSurvival));
            ItemScore score = new ItemScore
            {
                dps = item.Average(fight => fight.allyDps),
                dpsGain = dpsGain,
                dpsGainShare = teamDps > 0f ? dpsGain / teamDps : 0f,
                bestHolder = "",
                survivalTime = survivalTime,
                effectiveHealth = WaveScore.GetEffectiveHealth(survivalTime, teamDps),
                effectiveHealthGain = WaveScore.GetEffectiveHealth(survivalGain, teamDps),
                effectiveHealthGainShare = baselineSurvival > 0f ? survivalGain / baselineSurvival : 0f,
                timedOut = survived.Exists(fight => fight.timedOut),
            };
            // As the threat of a wave: the damage dealt before dying, with the item both ways
            score.power = WaveScore.GetThreat(score.dps, survivalTime);
            score.powerGain = score.power - baseline.power;
            score.powerGainShare = baseline.power > 0f ? score.powerGain / baseline.power : 0f;

            // The unit of the balance team gaining the most from it, the first one listed on a tie
            foreach (IGrouping<string, CombatStats> holder in item.Where(fight => !string.IsNullOrEmpty(fight.itemHolder)).GroupBy(fight => fight.itemHolder))
            {
                float holderGain = holder.Average(fight => fight.allyDps - Get(dpsBySeed, fight.seed, teamDps));
                if (string.IsNullOrEmpty(score.bestHolder) || holderGain > score.bestHolderDpsGain)
                {
                    score.bestHolder = holder.Key;
                    score.bestHolderDpsGain = holderGain;
                }
            }
            scores[item.Key] = score;
        }
        return scores;
    }

    static bool IsWithoutItem(CombatStats fight)
    {
        return string.IsNullOrEmpty(fight.item);
    }

    // The mean value of the fights of each seed
    static Dictionary<int, float> BySeed(IEnumerable<CombatStats> fights, System.Func<CombatStats, float> value)
    {
        return fights.GroupBy(fight => fight.seed).ToDictionary(seed => seed.Key, seed => seed.Average(value));
    }

    // The value of the seed, or the mean of every seed when that seed wasn't played without item
    static float Get(Dictionary<int, float> bySeed, int seed, float mean)
    {
        return bySeed.TryGetValue(seed, out float value) ? value : mean;
    }
}
