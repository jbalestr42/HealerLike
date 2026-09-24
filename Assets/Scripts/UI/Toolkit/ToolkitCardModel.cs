using System;

namespace HealerLike.UI.Toolkit
{
    /// <summary>Presentation-only data. Layout and theme never need to know gameplay types.</summary>
    public sealed class ToolkitCardModel
    {
        public string Key;
        public object IconSource;
        public string Title;
        public string Description;
        public string Status;
        public bool Enabled = true;
        public Action Activate;
    }

    public static class ToolkitPresentation
    {
        public static string Resource(float value, float maximum) => $"{value:0} / {maximum:0}";
        public static float Percentage(float value, float maximum) => maximum > 0f
            ? UnityEngine.Mathf.Clamp(value / maximum * 100f, 0f, 100f) : 0f;
        public static string SkillStatus(float cost, float cooldown, bool hasCost)
        {
            string mana = hasCost ? $"{cost:0} mana" : "Free";
            return cooldown > 0f ? $"{mana} · {cooldown:0.0}s" : $"{mana} · Ready";
        }
    }
}
