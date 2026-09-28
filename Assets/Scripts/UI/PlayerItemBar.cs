using System.Collections.Generic;
using UnityEngine;

// Displays an icon for each item of the character: the ones it starts with, then the ones won
public class PlayerItemBar : MonoBehaviour
{
    [SerializeField] PlayerItemIcon _iconPrefab;

    Character _character;
    readonly List<PlayerItemIcon> _icons = new List<PlayerItemIcon>();
    public List<PlayerItemIcon> icons => _icons;

    void Start()
    {
        PlayerBehaviour.instance.OnCharacterInit.AddListener(Show);
    }

    void OnDestroy()
    {
        Unbind();
    }

    public void Show(Character character)
    {
        Unbind();
        Clear();

        _character = character;
        foreach (AItem item in character.items)
        {
            AddIcon(item);
        }
        foreach (InventoryItemData itemData in character.inventoryHandler.items)
        {
            AddIcon(itemData.item);
        }

        character.inventoryHandler.OnItemAdded.AddListener(OnItemAdded);
        character.inventoryHandler.OnItemRemoved.AddListener(OnItemRemoved);
    }

    void OnItemAdded(InventoryItemData itemData, bool isNewItem)
    {
        AddIcon(itemData.item);
    }

    void OnItemRemoved(InventoryItemData itemData)
    {
        PlayerItemIcon icon = _icons.Find(i => i.item == itemData.item);
        if (icon != null)
        {
            _icons.Remove(icon);
            DestroyIcon(icon);
        }
    }

    void AddIcon(AItem item)
    {
        PlayerItemIcon icon = Instantiate(_iconPrefab, transform);
        icon.Init(item);
        _icons.Add(icon);
    }

    void Clear()
    {
        foreach (PlayerItemIcon icon in _icons)
        {
            DestroyIcon(icon);
        }
        _icons.Clear();
    }

    static void DestroyIcon(PlayerItemIcon icon)
    {
        if (Application.isPlaying)
        {
            Destroy(icon.gameObject);
        }
        else
        {
            DestroyImmediate(icon.gameObject);
        }
    }

    void Unbind()
    {
        if (_character != null)
        {
            _character.inventoryHandler.OnItemAdded.RemoveListener(OnItemAdded);
            _character.inventoryHandler.OnItemRemoved.RemoveListener(OnItemRemoved);
        }
        _character = null;
    }
}
