using UnityEngine;

// One icon of a kit row, a skill, a unit or an item: what the icon stands for and what its popover reads
public class ToolkitKitEntry
{
    public object iconSource;
    public string title;
    // The popover's text, already normalised for UI Toolkit rich text
    public string body;
    // A unit keeps its name beside its icon, a skill is its icon alone
    public bool showLabel;
    // Shown beside the icon instead of the name when set (e.g. the count of an item counting toward an effect)
    public string badge;
    // The outline of an item that stands out (its ItemStyle), none for a regular one
    public Color? outline;

    // The model the popover shows, through the same OnInspectRequested route a creature in the field uses
    public ToolkitCardModel ToModel()
    {
        return new ToolkitCardModel
        {
            key = title,
            iconSource = iconSource,
            title = title,
            description = body,
            source = this
        };
    }
}
