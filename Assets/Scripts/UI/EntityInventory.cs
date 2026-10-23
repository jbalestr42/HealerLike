using System.Collections.Generic;
using UnityEngine;

// A button per unit of the roster, to place it on the board
public class EntityInventory : MonoBehaviour
{
    [SerializeField] GameObject _inventoryConainer;

    [SerializeField] GameObject _inventoryEntity;

    List<SelectEntityButton> _entityButtons = new List<SelectEntityButton>();
    UnitRoster _roster;

    public void Bind(UnitRoster roster)
    {
        _roster = roster;
        _roster.OnChanged.AddListener(Refresh);
        Refresh();
    }

    void OnDestroy()
    {
        if (_roster != null)
        {
            _roster.OnChanged.RemoveListener(Refresh);
        }
    }

    void Refresh()
    {
        foreach (SelectEntityButton button in _entityButtons)
        {
            Destroy(button.gameObject);
        }
        _entityButtons.Clear();

        foreach (EntityData data in _roster.units)
        {
            GameObject entityButton = Instantiate(_inventoryEntity);
            entityButton.GetComponent<SelectEntityButton>().data = data;
            entityButton.GetComponentInChildren<TMPro.TMP_Text>().text = data.title;
            entityButton.transform.SetParent(_inventoryConainer.transform);
            _entityButtons.Add(entityButton.GetComponent<SelectEntityButton>());
        }
    }

    public void Show(bool show)
    {
        _inventoryConainer.SetActive(show);
    }
}
