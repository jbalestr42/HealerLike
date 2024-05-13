using UnityEngine;

public static class Math
{
    public static float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    public static float RemapClamped(float aValue, float aIn1, float aIn2, float aOut1, float aOut2)
    {
        float t = (aValue - aIn1) / (aIn2 - aIn1);
        t = Mathf.Clamp01(t);
        return aOut1 + (aOut2 - aOut1) * t;
    }

    public static float GetProgressionFactorFromWave(int currentWave)
    {
        return currentWave * currentWave * 0.15f;
    }
}