using UnityEngine;

// How far a simulation is: the part of the fights done, and the time left guessed from the time the done ones took
public static class SimulationProgress
{
    // Part of the fights done, from 0 to 1
    public static float GetRatio(int done, int total)
    {
        return total > 0 ? Mathf.Clamp01((float)done / total) : 1f;
    }

    // Seconds left at the average pace of the fights done so far, negative before the first one is done
    public static float EstimateRemaining(float elapsed, int done, int total)
    {
        if (done <= 0)
        {
            return -1f;
        }
        return elapsed / done * Mathf.Max(0, total - done);
    }

    // A short duration: 45s, 12m 05s, 1h 03m
    public static string FormatDuration(float seconds)
    {
        int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
        int hours = total / 3600;
        int minutes = total % 3600 / 60;
        int secs = total % 60;
        if (hours > 0)
        {
            return $"{hours}h {minutes:00}m";
        }
        if (minutes > 0)
        {
            return $"{minutes}m {secs:00}s";
        }
        return $"{secs}s";
    }

    // The short text of the editor's status bar: 3% 120/3744
    public static string DescribeShort(int done, int total)
    {
        return $"{GetRatio(done, total) * 100f:0}% {done}/{total}";
    }

    // One line for the progress bar: fights, percent, time spent and time left
    public static string Describe(int done, int total, float elapsed)
    {
        float remaining = EstimateRemaining(elapsed, done, total);
        string left = remaining < 0f ? "time left unknown yet" : $"~{FormatDuration(remaining)} left";
        return $"{done}/{total} fights ({GetRatio(done, total) * 100f:0}%), {FormatDuration(elapsed)} spent, {left}";
    }
}
