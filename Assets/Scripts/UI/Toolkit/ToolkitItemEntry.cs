// An equipment card of the inventory panel: the item, the inventory holding it and where that is
public class ToolkitItemEntry
{
    public AItem item;
    // Null for a starting item: it lives in Character.items, in no inventory handler
    public InventoryHandler owner;
    public string location;
    public bool isInnate;

    // The description written in the data of the item, empty for an item without data (e.g. a custom item)
    public static string GetDescription(AItem item)
    {
        IGameDataSource source = item as IGameDataSource;
        BaseItemData data = source != null ? source.sourceData as BaseItemData : null;
        return data != null && data.description != null ? data.description : string.Empty;
    }
}
