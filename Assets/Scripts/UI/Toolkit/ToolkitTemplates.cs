using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Templates own their hierarchy. A broken contract is reported before callbacks are attached.
public static class ToolkitTemplates
{
    public static bool TryClone<ElementType>(VisualTreeAsset template, string name, out ElementType root)
        where ElementType : VisualElement
    {
        root = null;
        if (template == null)
        {
            Debug.LogError("[ToolkitTemplates] Missing template for '" + name + "'.");
            return false;
        }

        TemplateContainer tree = template.CloneTree();
        if (!Require(tree, name, out root))
        {
            return false;
        }

        List<StyleSheet> localStyles = new List<StyleSheet>();
        for (int i = 0; i < root.styleSheets.count; i++)
        {
            localStyles.Add(root.styleSheets[i]);
        }

        root.styleSheets.Clear();
        for (int i = 0; i < tree.styleSheets.count; i++)
        {
            root.styleSheets.Add(tree.styleSheets[i]);
        }

        foreach (StyleSheet style in localStyles)
        {
            root.styleSheets.Add(style);
        }

        root.RemoveFromHierarchy();
        PreparePicking(root);
        return true;
    }

    public static bool Require<ElementType>(VisualElement root, string name, out ElementType element)
        where ElementType : VisualElement
    {
        element = root != null ? root.Q<ElementType>(name) : null;
        if (element != null)
        {
            return true;
        }

        Debug.LogError("[ToolkitTemplates] Required " + typeof(ElementType).Name + " '" + name + "' is missing.");
        return false;
    }

    public static void PreparePicking(VisualElement root)
    {
        root.Query<TemplateContainer>().ForEach(PrepareContainer);
    }

    static void PrepareContainer(TemplateContainer container)
    {
        // Semantic surfaces consume empty-space taps; structural wrappers leave the battlefield open.
        bool surface = container.ClassListContains("surface") || container.ClassListContains("modal-layer");
        container.pickingMode = surface ? PickingMode.Position : PickingMode.Ignore;
    }
}
