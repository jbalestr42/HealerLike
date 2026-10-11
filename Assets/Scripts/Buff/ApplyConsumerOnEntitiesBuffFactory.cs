using System;
using System.Collections.Generic;
using UnityEngine;
using Oisif.Inspector;

[CreateAssetMenu(menuName = "Custom/Data/Buff/ApplyConsumerOnEntitiesBuff")]
public class ApplyConsumerOnEntitiesBuffFactory : BuffFactory<ApplyConsumerOnEntitiesBuff, ApplyConsumerOnEntitiesBuffData> { }

[Serializable]
public class ApplyConsumerOnEntitiesBuffData
{
    [CreateDataButton]
    public AConsumerFactory consumerFactory;
    public Entity.EntityType entityType = Entity.EntityType.Player;
}

// Applies a consumer (a heal, damage, ...) to every entity of a type, on each period of its handler:
// the holder is the source, so its attributes (e.g. the Heal Power of the character) scale the value
public class ApplyConsumerOnEntitiesBuff : ABuff<ApplyConsumerOnEntitiesBuffData>, IStackableBuff
{
    int _stacks = 1;

    public override void Instant(GameObject source, GameObject target)
    {
        EntityManager entityManager = EntityManager.existingInstance;
        if (entityManager != null)
        {
            ApplyTo(entityManager.GetEntities(data.entityType), target);
        }
    }

    public void ApplyTo(List<GameObject> entities, GameObject holder)
    {
        foreach (GameObject entity in entities)
        {
            entity.GetComponent<Entity>().health.AddResourceModifier(ResourceModifier.Create(data.consumerFactory, holder, entity, _stacks));
        }
    }

    public override void Add(GameObject source, GameObject target) { }

    public override void Remove(GameObject source, GameObject target) { }

    #region IStackableBuff

    public void Stack(GameObject source, GameObject target)
    {
        _stacks++;
    }

    public void Unstack(GameObject source, GameObject target)
    {
        _stacks--;
    }

    #endregion
}
