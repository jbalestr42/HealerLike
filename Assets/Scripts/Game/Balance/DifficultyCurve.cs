using System;
using UnityEngine;

// Threat a wave should have on each floor (WaveScore.threat): an exponential difficulty, the threat of floor 0
// multiplied by the same growth from one floor to the next
[Serializable]
public class DifficultyCurve
{
    public float floorZeroThreat = 500f;
    // 0.15 for +15% of threat per floor
    public float growthPerFloor = 0.15f;

    public DifficultyCurve() { }

    public DifficultyCurve(float floorZeroThreat, float growthPerFloor)
    {
        this.floorZeroThreat = floorZeroThreat;
        this.growthPerFloor = growthPerFloor;
    }

    public float GetThreat(float floor)
    {
        return floorZeroThreat * Mathf.Pow(1f + growthPerFloor, floor);
    }

    // The floor, with its fraction, where the curve reaches the threat: negative below the threat of floor 0.
    // Without growth, the curve stays at the threat of floor 0: every threat is on floor 0
    public float GetFloor(float threat)
    {
        if (growthPerFloor <= 0f || threat <= 0f || floorZeroThreat <= 0f)
        {
            return 0f;
        }
        return Mathf.Log(threat / floorZeroThreat) / Mathf.Log(1f + growthPerFloor);
    }
}
