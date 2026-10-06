using UnityEngine;

// Panel of the game shown on the selected entity: everything about it (health, stats, targeting, skills,
// passives, items, effects) in the detailed info panel, and its inventory to equip items on it
public class PanelEntity : APanel
{
    [SerializeField] SlotInventory _inventory;
    [SerializeField] EntityInfoPanel _info;

    public override void OnShowUI(GameObject selectedObject)
    {
        Entity entity = selectedObject.GetComponent<Entity>();
        _info.OnShowUI(selectedObject);
        _inventory.inventoryHandler = entity.inventoryHandler;
        _inventory.RefreshInventory();
    }

    public override void UpdateUI(GameObject selectedObject)
    {
        if (selectedObject != null)
        {
            _info.UpdateUI(selectedObject);
        }
    }

    public override void OnHideUI(GameObject selectedObject)
    {
        _info.OnHideUI(selectedObject);
    }
}
