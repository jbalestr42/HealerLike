using System;
using UnityEngine;
using Oisif.Inspector;

[CreateAssetMenu(menuName = "Custom/Data/Buff/ManaOnKillBuff")]
public class ManaOnKillBuffFactory : BuffFactory<ManaOnKillBuff, ManaOnKillBuffData> { }

[Serializable]
public class ManaOnKillBuffData
{
    // Applied to the mana of the character carrying the buff
    [CreateDataButton]
    public AConsumerFactory consumerFactory;
    // Side of the entities whose death gives mana
    public Entity.EntityType killedType = Entity.EntityType.Computer;
}

// Restores mana to the character each time an entity of the killed side dies
public class ManaOnKillBuff : ABuff<ManaOnKillBuffData>, IStackableBuff
{
    int _stacks = 1;
    Character _owner;

    public void OnEntityKilled(Entity dead)
    {
        if (_owner == null || dead == null || dead.entityType != data.killedType)
        {
            return;
        }

        _owner.mana.AddResourceModifier(ResourceModifier.Create(data.consumerFactory, _owner.gameObject, _owner.gameObject, _stacks));
    }

    public override void Instant(GameObject source, GameObject target) { }

    public override void Add(GameObject source, GameObject target)
    {
        _owner = target.GetComponent<Character>();
        EntityManager.instance.OnEntityKilled.AddListener(OnEntityKilled);
    }

    public override void Remove(GameObject source, GameObject target)
    {
        // Also called while the scene is torn down, where the entity manager may already be gone
        EntityManager entityManager = EntityManager.existingInstance;
        if (entityManager != null)
        {
            entityManager.OnEntityKilled.RemoveListener(OnEntityKilled);
        }
    }

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
