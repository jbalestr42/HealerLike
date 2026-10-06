using System.Collections.Generic;
using UnityEngine;

// Weights edited as shares of 100%: setting one share rescales the others, keeping their proportions, so
// the shares always add up to 100% (e.g. the room types of the map generation test scene)
public static class WeightShares
{
    // 0 when every weight is 0
    public static float GetShare(IReadOnlyList<float> weights, int index)
    {
        float total = 0f;
        foreach (float weight in weights)
        {
            total += Mathf.Max(0f, weight);
        }
        return total > 0f ? Mathf.Max(0f, weights[index]) / total : 0f;
    }

    // The weights become the shares themselves (adding up to 1); the others split what's left in proportion
    // to their weights, or evenly when they all were 0
    public static void SetShare(IList<float> weights, int index, float share)
    {
        if (weights.Count == 1)
        {
            weights[0] = 1f;
            return;
        }

        share = Mathf.Clamp01(share);
        float othersTotal = 0f;
        for (int i = 0; i < weights.Count; i++)
        {
            if (i != index)
            {
                othersTotal += Mathf.Max(0f, weights[i]);
            }
        }

        for (int i = 0; i < weights.Count; i++)
        {
            if (i == index)
            {
                continue;
            }

            weights[i] = othersTotal > 0f ? Mathf.Max(0f, weights[i]) / othersTotal * (1f - share) : (1f - share) / (weights.Count - 1);
        }
        weights[index] = share;
    }
}
