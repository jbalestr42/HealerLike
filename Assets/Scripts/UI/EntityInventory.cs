using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class EntityInventory : MonoBehaviour
{
    [SerializeField] GameObject _inventoryConainer;

    [SerializeField] GameObject _inventoryEntity;

    List<SelectEntityButton> _entityButtons = new List<SelectEntityButton>();

	public void Init(List<EntityData> entitiesData)
    {
        _entityButtons.Clear();
		foreach (var data in entitiesData)
        {
            AddEntity(data);
        }

        EntityManager.instance.OnEntitySpawned.AddListener(UpdateUsableEntities);
	}
    
    public void AddEntity(EntityData data)
    {
        var entityButton = Instantiate(_inventoryEntity);
        entityButton.GetComponent<SelectEntityButton>().data = data;
        entityButton.GetComponentInChildren<UnityEngine.UI.Text>().text = data.title + "\n" + data.price;
        entityButton.transform.SetParent(_inventoryConainer.transform);
        _entityButtons.Add(entityButton.GetComponent<SelectEntityButton>());
    }

    public void UpdateUsableEntities(Entity entity)
    {
        var entityButton = _entityButtons.Where(x => x.data == entity.data).FirstOrDefault();
        if (entityButton != null)
        {
            _entityButtons.Remove(entityButton);
            GameObject.Destroy(entityButton.gameObject);
        }
    }

    public void Show(bool show)
    {
        _inventoryConainer.SetActive(show);
    }
}
