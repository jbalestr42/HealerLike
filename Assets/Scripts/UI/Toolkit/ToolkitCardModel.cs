using UnityEngine.Events;

// Presentation only data: the layout and the theme never need to know the gameplay types
public class ToolkitCardModel
{
    public string key;
    public object iconSource;
    public string title;
    public string description;
    public string status;
    public bool isEnabled = true;
    public bool canDrag;
    public System.Func<bool> canBeginDrag;
    public ToolkitSpellState spell;
    public float healthFraction = -1f;
    public System.Action<Entity> deployed;

    // What the card acts on, read back by its activate callback
    public object source;
    public UnityAction<ToolkitCardModel> activate;
}
