using UnityEngine.Events;

// Presentation only data: the layout and the theme never need to know the gameplay types
public class ToolkitCardModel
{
    public string key;
    public object iconSource;
    public string title;
    public string description;
    // A lighter second block under the description, hidden when empty (the class screen's kit)
    public string details;
    public string status;
    public bool isEnabled = true;
    // An item with the Cursed tag: the card is outlined, as PlayerItemIcon outlines its icon
    public bool isCursed;
    public bool canDrag;
    public System.Func<bool> canBeginDrag;
    public ToolkitSpellState spell;
    public float healthFraction = -1f;
    public System.Action<Entity> deployed;

    // What the card acts on, read back by its activate callback
    public object source;
    public UnityAction<ToolkitCardModel> activate;
}
