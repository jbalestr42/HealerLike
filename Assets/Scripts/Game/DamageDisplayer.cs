using UnityEngine;

public class DamageDisplayer : MonoBehaviour
{
    [SerializeField] GameObject _damagePopup;

    void Start()
    {
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
        //Debug.Log("[DEBUG] All damage received " + value);
        if (value != 0f)
        {
            GameObject damagePopupGO = Instantiate(_damagePopup, owner.transform.position, Quaternion.identity);
            DamagePopup damagePopup = damagePopupGO.GetComponent<DamagePopup>();
            damagePopup.Init(resourceModifier.source, value);
        }
    }
}