using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityInventory : MonoBehaviour
{
    [SerializeField] GameObject _inventoryConainer;

    [SerializeField] GameObject _inventoryEntity;

    List<SelectEntityButton> _entityButtons;

	void Awake()
    {
        _entityButtons = new List<SelectEntityButton>();
        var entitiesData = DataManager.instance.entities;
		foreach (var data in entitiesData)
        {
            var entityButton = Instantiate(_inventoryEntity);
            entityButton.GetComponent<SelectEntityButton>().data = data;
            entityButton.GetComponentInChildren<UnityEngine.UI.Text>().text = data.title + "\n" + data.price;
            entityButton.transform.SetParent(_inventoryConainer.transform);
            _entityButtons.Add(entityButton.GetComponent<SelectEntityButton>());
        }

        PlayerBehaviour.instance.OnGoldChanged.AddListener(UpdateAffordableTowers);
	}

    public void UpdateAffordableTowers(int gold)
    {
        for (int i = 0; i < _entityButtons.Count; i++)
        {
            _entityButtons[i].GetComponent<UnityEngine.UI.Button>().interactable = _entityButtons[i].data.price <= gold;
        }
    }
}
