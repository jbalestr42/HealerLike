using UnityEngine;

public class DamageDisplayer : MonoBehaviour
{
    [SerializeField] GameObject _damagePopup;

    GameObject _parent;

    void Start()
    {
        _parent = new GameObject("DamageDisplayer");
        _parent.transform.SetParent(transform);

        EntityManager.instance.OnEntitySpawned.AddListener(RegisterEntity);
        EntityManager.instance.OnEntityKilled.AddListener(UnregisterEntity);
    }

    void RegisterEntity(Entity entity)
    {
        entity.health.OnAllConsumerProcessed.AddListener(DisplayDamage);
    }

    void UnregisterEntity(Entity entity)
    {
        entity.health.OnAllConsumerProcessed.RemoveListener(DisplayDamage);
    }

    void DisplayDamage(GameObject owner, ResourceAttribute resource, ResourceModifier resourceModifier, float value)
    {
        GameObject damagePopupGO = Instantiate(_damagePopup, owner.transform.position, Quaternion.identity);
        DamagePopup damagePopup = damagePopupGO.GetComponent<DamagePopup>();
        damagePopupGO.transform.SetParent(_parent.transform);
        damagePopup.Init(resourceModifier.source, value);
    }
}