using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Buff/ManaOnOverhealBuff")]
public class ManaOnOverhealBuffFactory : BuffFactory<ManaOnOverhealBuff, ManaOnOverhealBuffData> { }

[Serializable]
public class ManaOnOverhealBuffData
{
    // Part of the heal above the max health restored as mana to the character carrying the buff
    [Min(0)]
    public float ratio = 0.1f;
    // Side of the entities whose overheal gives mana
    public Entity.EntityType healedType = Entity.EntityType.Player;
}

// Restores mana to the character each time an entity of the healed side is healed above its max health
public class ManaOnOverhealBuff : ABuff<ManaOnOverhealBuffData>
{
    Character _owner;
    List<Entity> _healedEntities = new List<Entity>();

    public override void Instant(GameObject source, GameObject target) { }

    public override void Add(GameObject source, GameObject target)
    {
        _owner = target.GetComponent<Character>();
        EntityManager entityManager = EntityManager.instance;
        foreach (GameObject entity in entityManager.GetEntities(data.healedType))
        {
            Listen(entity.GetComponent<Entity>());
        }
        entityManager.OnEntitySpawned.AddListener(Listen);
    }

    public override void Remove(GameObject source, GameObject target)
    {
        // Also called while the scene is torn down, where the entity manager may already be gone
        EntityManager entityManager = EntityManager.existingInstance;
        if (entityManager != null)
        {
            entityManager.OnEntitySpawned.RemoveListener(Listen);
        }
        StopListening();
    }

    // Listens to the heals of the entity when it's of the healed side
    public void Listen(Entity entity)
    {
        // The dead entities are destroyed along with their listeners
        _healedEntities.RemoveAll(healed => healed == null);
        if (entity == null || entity.entityType != data.healedType || _healedEntities.Contains(entity))
        {
            return;
        }

        entity.OnHealReceived.AddListener(OnHealReceived);
        _healedEntities.Add(entity);
    }

    public void StopListening()
    {
        foreach (Entity entity in _healedEntities)
        {
            if (entity != null)
            {
                entity.OnHealReceived.RemoveListener(OnHealReceived);
            }
        }
        _healedEntities.Clear();
    }

    void OnHealReceived(GameObject source, ConsumerResult heal)
    {
        float mana = heal.overflow * data.ratio;
        if (_owner == null || mana <= 0f)
        {
            return;
        }

        ResourceModifier manaModifier = new ResourceModifier { source = _owner.gameObject };
        manaModifier.consumers.Add(new RuntimeConsumer(mana));
        _owner.mana.AddResourceModifier(manaModifier);
    }
}
