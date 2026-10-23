using UnityEngine;

// The stash of the player, in slots
public class PlayerInventory : MonoBehaviour
{
    [SerializeField] SlotInventory _inventory;
    public SlotInventory inventory { get { return _inventory; } set { _inventory = value; } }

    void Awake()
    {
        _inventory.inventoryHandler = PlayerBehaviour.instance.stash;
        _inventory.Init();
    }

    public bool IsInventoryVisible()
    {
        return _inventory.gameObject.activeInHierarchy;
    }

    public void ShowInventory()
    {
        _inventory.gameObject.SetActive(true);
    }

    public void HideInventory()
    {
        _inventory.gameObject.SetActive(false);
    }
}
