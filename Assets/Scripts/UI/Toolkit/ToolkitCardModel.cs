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

    // What the card acts on, read back by its activate callback
    public object source;
    public UnityAction<ToolkitCardModel> activate;
}
