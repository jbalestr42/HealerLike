using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Buff/DamageAllEnemyOnEnemyDieBuff")]
public class DamageAllEnemyOnEnemyDieBuffFactory : BuffFactory<DamageAllEnemyOnEnemyDieBuff, DamageAllEnemyOnEnemyDieBuffData> { }

[Serializable]
public class DamageAllEnemyOnEnemyDieBuffData
{
    [CreateDataButton]
    public AConsumerFactory damageToAllEnemy;
}

public class DamageAllEnemyOnEnemyDieBuff : ABuff<DamageAllEnemyOnEnemyDieBuffData>, IStackableBuff
{
    int _stacks = 1;

    void OnEnemyDie(Enemy enemy, bool hasReachedEnd)
    {
        if (!hasReachedEnd)
        {
            foreach (GameObject enemyGo in EntityManager.instance.enemies)
            {
                if (enemyGo != enemy.gameObject)
                {
                    ResourceModifier resourceModifier = new ResourceModifier();
                    resourceModifier.consumers.Add(data.damageToAllEnemy.GetConsumer(enemy.gameObject, enemy.gameObject));
                    resourceModifier.multiplier = _stacks;
                    resourceModifier.source = enemy.gameObject;

                    enemyGo.GetComponent<Entity>().health.AddResourceModifier(resourceModifier);
                }
            }
        }
    }

    public override void Add(GameObject source, GameObject target)
    {
        EntityManager.instance.OnEnemyKilled.AddListener(OnEnemyDie);
    }

    public override void Remove(GameObject source, GameObject target)
    {
        EntityManager.instance.OnEnemyKilled.RemoveListener(OnEnemyDie);
    }

    public void Stack(GameObject source, GameObject target)
    {
        _stacks++;
    }

    public void Unstack(GameObject source, GameObject target)
    {
        _stacks--;
    }
}