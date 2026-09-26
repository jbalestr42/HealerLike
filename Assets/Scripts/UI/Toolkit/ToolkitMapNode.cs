using System;
using UnityEngine.UIElements;

public class ToolkitMapNode : IDisposable
{
    readonly Action<MapNode> _select;
    ToolkitMapGlyph _glyph;
    Label _state;
    public MapNode node { get; private set; }
    public Button button { get; private set; }

    public ToolkitMapNode(VisualTreeAsset template, MapNode node, Action<MapNode> select)
    {
        _select = select;
        this.node = node;
        if (
            !ToolkitTemplates.TryClone(template, "map-node", out Button root)
            || !ToolkitTemplates.Require(root, "map-label", out Label label)
            || !ToolkitTemplates.Require(root, "map-state", out _state)
            || !ToolkitTemplates.Require(root, "map-glyph", out VisualElement glyph)
        )
        {
            return;
        }

        button = root;
        button.name = $"map-node-{node.floor}-{node.column}";
        button.userData = node;
        button.AddToClassList("room-" + node.type.ToString().ToLowerInvariant());
        button.tooltip =
            $"{MapView.GetNodeLabel(node.type)} · Room {node.floor + 1}\n"
            + ToolkitRunMapPresentation.Description(node.type);
        label.text = MapView.GetNodeLabel(node.type);
        _glyph = new ToolkitMapGlyph(glyph, node.type);
        button.clicked += Activate;
    }

    public void Display(MapNodeState state, bool canSelect)
    {
        button.EnableInClassList("is-available", state == MapNodeState.Available);
        button.EnableInClassList("is-current", state == MapNodeState.Current);
        button.EnableInClassList("is-visited", state == MapNodeState.Visited);
        button.EnableInClassList("is-locked", state == MapNodeState.Locked);
        button.SetEnabled(canSelect && state == MapNodeState.Available);
        _state.text = ToolkitRunMapPresentation.StateLabel(state);
    }

    void Activate()
    {
        if (node != null && _select != null)
        {
            _select.Invoke(node);
        }
    }

    public void Dispose()
    {
        if (button != null)
        {
            button.clicked -= Activate;
            if (ReferenceEquals(button.userData, node))
            {
                button.userData = null;
            }

            button.RemoveFromHierarchy();
        }

        if (_glyph != null)
        {
            _glyph.Dispose();
        }

        node = null;
    }
}
