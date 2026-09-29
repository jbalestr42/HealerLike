using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Buff/DamageEnemyOnHealBuff")]
public class DamageEnemyOnHealBuffFactory : BuffFactory<DamageEnemyOnHealBuff, DamageEnemyOnHealBuffData> { }

[Serializable]
public class DamageEnemyOnHealBuffData
{
    // Part of each heal received by the holder dealt as damage to one of its enemies
    [MinValue(0)]
    public float ratio = 0.25f;
    // How the enemy is picked, from the position of the holder
    public TargetBehaviourType targetType = TargetBehaviourType.Nearest;
}

public class DamageEnemyOnHealBuff : ABuff<DamageEnemyOnHealBuffData>
{
    Entity _owner;
    ATargetBehaviour _targetBehaviour;

    public override void Instant(GameObject source, GameObject target) { }

    public override void Add(GameObject source, GameObject target)
    {
        _owner = target.GetComponent<Entity>();
        _targetBehaviour = ATargetBehaviour.Create(data.targetType);
        _owner.OnHealReceived.AddListener(OnHealReceived);
    }

    public override void Remove(GameObject source, GameObject target)
    {
        if (_owner != null)
        {
            _owner.OnHealReceived.RemoveListener(OnHealReceived);
        }
    }

    void OnHealReceived(GameObject source, ConsumerResult heal)
    {
        float damage = heal.value * data.ratio;
        if (damage <= 0f)
        {
            return;
        }

        Entity enemy = PickEnemy();
        if (enemy != null)
        {
            // Regular damage: reduced by the armor, prevented by an invincibility
            ResourceModifier damageModifier = new ResourceModifier { source = _owner.gameObject };
            damageModifier.consumers.Add(new RuntimeConsumer(-damage, false, false));
            enemy.health.AddResourceModifier(damageModifier);
        }
    }

    // The first living enemy in the order of the target behaviour, null without any
    Entity PickEnemy()
    {
        List<GameObject> enemies = GetEnemies().FindAll(enemy =>
        {
            Entity entity = enemy != null ? enemy.GetComponent<Entity>() : null;
            return entity != null && entity.health != null && entity.health.Value > 0f;
        });
        if (enemies.Count == 0 || _targetBehaviour == null)
        {
            return null;
        }

        _targetBehaviour.ApplyBehaviour(enemies, _owner.transform.position, float.MaxValue);
        return enemies[0].GetComponent<Entity>();
    }

    protected virtual List<GameObject> GetEnemies()
    {
        EntityManager entityManager = EntityManager.existingInstance;
        return entityManager != null ? entityManager.GetEntities(_owner.GetTargetType()) : new List<GameObject>();
    }
}
