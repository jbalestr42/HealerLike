using UnityEngine;

// Gives a new copy of the item to every clicked entity until the interaction is cancelled
public class GiveItemInteraction : AInteraction
{
    AItemFactory _itemFactory;

    public GiveItemInteraction(AItemFactory itemFactory)
    {
        _itemFactory = itemFactory;
    }

    public override int GetLayerMask()
    {
        return 1 << Layers.Entity;
    }

    public override bool IsValidTarget(GameObject target)
    {
        return target.GetComponentInParent<Entity>() != null;
    }

    public override void OnMouseClick(RaycastHit hit)
    {
        InventoryHandler inventoryHandler = hit.transform.GetComponentInParent<Entity>().inventoryHandler;
        inventoryHandler.AddItem(_itemFactory.GetItem(), inventoryHandler.GetFirstFreeIndex());
    }
}
