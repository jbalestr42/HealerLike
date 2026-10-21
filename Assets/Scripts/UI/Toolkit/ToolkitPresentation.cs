using UnityEngine;

// Text and values shown by the Toolkit cards and bars
public static class ToolkitPresentation
{
    public static string Resource(float value, float maximum)
    {
        return $"{value:0} / {maximum:0}";
    }

    public static float Percentage(float value, float maximum)
    {
        if (maximum <= 0f)
        {
            return 0f;
        }

        return Mathf.Clamp(value / maximum * 100f, 0f, 100f);
    }

    public static string SkillStatus(bool showCost, string cost, bool showCooldown, string cooldown)
    {
        string resource = "Free";
        if (showCost)
        {
            resource = $"{cost} mana";
        }

        string timing = "Ready";
        if (showCooldown && !string.IsNullOrEmpty(cooldown))
        {
            timing = cooldown;
        }

        return $"{resource} · {timing}";
    }
}
