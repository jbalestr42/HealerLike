using UnityEngine.UIElements;

// Unitless custom properties consumed by computed placement and custom painting.
public static class ToolkitStyleValues
{
    public static float ReadPositive(ICustomStyle style, string name, float fallback)
    {
        float value = ReadNonNegative(style, name, fallback);
        return value > 0f ? value : fallback;
    }

    public static float ReadNonNegative(ICustomStyle style, string name, float fallback)
    {
        if (
            style.TryGetValue(new CustomStyleProperty<float>(name), out float value)
            && float.IsFinite(value)
            && value >= 0f
        )
        {
            return value;
        }

        return fallback;
    }
}
