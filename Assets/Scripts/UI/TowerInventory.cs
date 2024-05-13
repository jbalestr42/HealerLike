using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerInventory : MonoBehaviour
{
    [SerializeField] GameObject _inventoryConainer;

    [SerializeField] GameObject _inventoryTower;

    [SerializeField] TowerRange _towerRange;

    List<SelectTowerButton> _towerButtons;

	void Awake()
    {
        _towerButtons = new List<SelectTowerButton>();
        var towersData = DataManager.instance.towers;
		foreach (var data in towersData)
        {
            var towerButton = Instantiate(_inventoryTower);
            towerButton.GetComponent<SelectTowerButton>().data = data;
            towerButton.GetComponent<SelectTowerButton>().range = _towerRange;
            towerButton.GetComponentInChildren<UnityEngine.UI.Text>().text = data.title + "\n" + data.price;
            towerButton.transform.SetParent(_inventoryConainer.transform);
            _towerButtons.Add(towerButton.GetComponent<SelectTowerButton>());
        }

        PlayerBehaviour.instance.OnGoldChanged.AddListener(UpdateAffordableTowers);
	}

    public void UpdateAffordableTowers(int gold)
    {
        for (int i = 0; i < _towerButtons.Count; i++)
        {
            _towerButtons[i].GetComponent<UnityEngine.UI.Button>().interactable = _towerButtons[i].data.price <= gold;
        }
    }
}
