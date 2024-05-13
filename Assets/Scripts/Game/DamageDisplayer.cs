using UnityEngine;

public class DamageDisplayer : MonoBehaviour
{
    [SerializeField] GameObject _damagePopup;

    void Start()
    {
        EntityManager.instance.OnEnemySpawned.AddListener(RegisterEnemy);
        EntityManager.instance.OnEnemyKilled.AddListener(UnregisterEnemy);
    }

    void RegisterEnemy(Enemy enemy)
    {
        enemy.health.OnAllConsumerProcessed.AddListener(DisplayDamage);
    }

    void UnregisterEnemy(Enemy enemy, bool hasReachedEnd)
    {
        enemy.health.OnAllConsumerProcessed.RemoveListener(DisplayDamage);
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