// An equipment card of the inventory panel: the item, the inventory holding it and where that is
public class ToolkitItemEntry
{
    public AItem item;
    // Null for a starting item: it lives in Character.items, in no inventory handler
    public InventoryHandler owner;
    public string location;
    public bool isInnate;
}
