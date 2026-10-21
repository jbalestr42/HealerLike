using UnityEngine;
using UnityEngine.UIElements;

// Runtime and the design preview resolve exactly the same theme asset.
public static class ToolkitTheme
{
    public static ThemeStyleSheet Resolve(ThemeStyleSheet selected)
    {
        return selected != null ? selected : Resources.Load<ThemeStyleSheet>("UI/Toolkit/RuntimeTheme");
    }

    public static bool Apply(VisualElement root, ThemeStyleSheet selected)
    {
        ThemeStyleSheet theme = Resolve(selected);
        if (theme == null)
        {
            Debug.LogError("[ToolkitTheme] Requires Resources/UI/Toolkit/RuntimeTheme.tss or an assigned theme.");
            return false;
        }

        for (int i = root.styleSheets.count - 1; i >= 0; i--)
        {
            StyleSheet previous = root.styleSheets[i];
            if (previous is ThemeStyleSheet)
            {
                root.styleSheets.Remove(previous);
            }
        }

        root.AddToClassList("toolkit-theme");
        root.styleSheets.Add(theme);
        return true;
    }

    public static PanelSettings CreatePanelSettings(PanelSettings source, ThemeStyleSheet selected)
    {
        PanelSettings settings =
            source != null ? Object.Instantiate(source) : ScriptableObject.CreateInstance<PanelSettings>();
        settings.name = "Toolkit runtime panel";
        settings.themeStyleSheet = Resolve(selected);
        settings.scaleMode = PanelScaleMode.ConstantPixelSize;
        return settings;
    }
}
