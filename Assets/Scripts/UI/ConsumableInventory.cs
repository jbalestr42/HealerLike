using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConsumableInventory : MonoBehaviour
{
    [SerializeField] GameObject _inventoryConainer;
    [SerializeField] GameObject _inventoryConsumable;

    List<UseConsumableButton> _consumableButtons = new List<UseConsumableButton>();

	void Awake()
    {
        var consumables = DataManager.instance.consumables;
		foreach (AConsumableFactory data in consumables)
        {
            var consumableButton = Instantiate(_inventoryConsumable);
            consumableButton.GetComponent<UseConsumableButton>().data = data;
            consumableButton.GetComponentInChildren<UnityEngine.UI.Text>().text = data.title;
            consumableButton.transform.SetParent(_inventoryConainer.transform);
            _consumableButtons.Add(consumableButton.GetComponent<UseConsumableButton>());
        }
	}
}
