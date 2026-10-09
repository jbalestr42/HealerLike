using System;
using UnityEngine;
using Oisif.Inspector;

[CreateAssetMenu(menuName = "Custom/Data/Buff/ApplyBuffOnEventBuff")]
public class ApplyBuffOnEventBuffFactory : BuffFactory<ApplyBuffOnEventBuff, ApplyBuffOnEventBuffData> { }

// A mask: the buff can be given on several events
[Flags]
public enum BuffEventTrigger
{
    None = 0,
    // Every entity of the side, when a battle starts
    BattleStart = 1 << 0,
    // Each entity of the side summoned during a battle
    Summoned = 1 << 1,
}

[Serializable]
public class ApplyBuffOnEventBuffData
{
    public BuffEventTrigger triggers;
    [CreateDataButton]
    public ABuffHandlerFactory buffHandlerFactory;
    // Side of the entities getting the buff
    public Entity.EntityType entityType = Entity.EntityType.Player;
}

// Gives a buff to the entities of a side on a game event, from the one carrying this buff (e.g. an
// item of the character)
public class ApplyBuffOnEventBuff : ABuff<ApplyBuffOnEventBuffData>
{
    GameObject _owner;

    public void ApplyTo(Entity entity)
    {
        if (_owner == null || entity == null || entity.entityType != data.entityType)
        {
            return;
        }

        entity.AddBuffHandler(data.buffHandlerFactory, _owner, entity.gameObject);
    }

    void OnBattleStart()
    {
        foreach (GameObject entity in EntityManager.instance.GetEntities(data.entityType))
        {
            ApplyTo(entity.GetComponent<Entity>());
        }
    }

    public override void Instant(GameObject source, GameObject target) { }

    public override void Add(GameObject source, GameObject target)
    {
        _owner = target;
        if (data.triggers.HasFlag(BuffEventTrigger.BattleStart))
        {
            AscensionGameType.OnBattleStart.AddListener(OnBattleStart);
        }
        if (data.triggers.HasFlag(BuffEventTrigger.Summoned))
        {
            EntityManager.instance.OnEntitySummoned.AddListener(ApplyTo);
        }
    }

    public override void Remove(GameObject source, GameObject target)
    {
        AscensionGameType.OnBattleStart.RemoveListener(OnBattleStart);
        // Also called while the scene is torn down, where the entity manager may already be gone
        EntityManager entityManager = EntityManager.existingInstance;
        if (entityManager != null)
        {
            entityManager.OnEntitySummoned.RemoveListener(ApplyTo);
        }
    }
}
