// Finds the buff counting toward the effect of an item (ICounterBuff) among the buffs its holder got from it
public static class ItemCounter
{
    // Null when the item has none, or its buffs are not applied yet (the buff manager applies them on its update)
    public static ICounterBuff Find(AItem item, BuffManager holderBuffManager)
    {
        Item buffItem = item as Item;
        if (buffItem == null || buffItem.data == null || buffItem.data.buffs == null || holderBuffManager == null)
        {
            return null;
        }
        foreach (ABuffHandlerFactory buffHandlerFactory in buffItem.data.buffs)
        {
            if (buffHandlerFactory == null || buffHandlerFactory.buffFactoryList == null)
            {
                continue;
            }
            foreach (ABuffFactory buffFactory in buffHandlerFactory.buffFactoryList)
            {
                if (buffFactory != null && holderBuffManager.GetFirstBuff(buffFactory) is ICounterBuff counterBuff)
                {
                    return counterBuff;
                }
            }
        }
        return null;
    }

    // The current count alone, e.g. "4": four spells used, the next one is free
    public static string GetText(ICounterBuff counterBuff)
    {
        return counterBuff.counter.ToString();
    }
}
