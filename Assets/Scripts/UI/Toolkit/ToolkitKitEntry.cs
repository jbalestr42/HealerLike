// One icon of a class card's kit, a skill or a unit: what the icon stands for and what its popover reads
public class ToolkitKitEntry
{
    public object iconSource;
    public string title;
    // The popover's text, already normalised for UI Toolkit rich text
    public string body;
    // A unit keeps its name beside its icon, a skill is its icon alone
    public bool showLabel;

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
