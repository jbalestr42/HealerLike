using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Custom/Data/Buff/ShareCharacterHealBuff")]
public class ShareCharacterHealBuffFactory : BuffFactory<ShareCharacterHealBuff, ShareCharacterHealBuffData> { }

[Serializable]
public class ShareCharacterHealBuffData
{
    // Part of each heal of the character on an ally also healed on the most wounded ally around the holder
    [Min(0)]
    public float ratio = 0.4f;
    public RelativeCellPatternType pattern = RelativeCellPatternType.Adjacent;
    [Min(1)]
    public int range = 1;
}

// Beacon: each heal the character casts on an ally is copied, in part, on the most wounded ally around the
// holder, the healed ally left out
public class ShareCharacterHealBuff : ABuff<ShareCharacterHealBuffData>
{
    Entity _owner;
    // The listener of each ally, to remove it
    readonly Dictionary<Entity, UnityAction<GameObject, ConsumerResult>> _listened = new Dictionary<Entity, UnityAction<GameObject, ConsumerResult>>();

    public override void Instant(GameObject source, GameObject target) { }

    public override void Add(GameObject source, GameObject target)
    {
        _owner = target.GetComponent<Entity>();
        foreach (GameObject ally in GetAllies())
        {
            Listen(ally != null ? ally.GetComponent<Entity>() : null);
        }

        EntityManager entityManager = EntityManager.existingInstance;
        if (entityManager != null)
        {
            entityManager.OnEntitySpawned.AddListener(Listen);
        }
    }

    public override void Remove(GameObject source, GameObject target)
    {
        // Also called while the scene is torn down, where the entity manager may already be gone
        EntityManager entityManager = EntityManager.existingInstance;
        if (entityManager != null)
        {
            entityManager.OnEntitySpawned.RemoveListener(Listen);
        }

        foreach (KeyValuePair<Entity, UnityAction<GameObject, ConsumerResult>> listened in _listened)
        {
            if (listened.Key != null)
            {
                listened.Key.OnHealReceived.RemoveListener(listened.Value);
            }
        }
        _listened.Clear();
    }

    // Listens to the heals of the entity when it's an ally of the holder
    public void Listen(Entity entity)
    {
        if (entity == null || _owner == null || entity.entityType != _owner.entityType || _listened.ContainsKey(entity))
        {
            return;
        }

        UnityAction<GameObject, ConsumerResult> listener = (source, heal) => OnHealReceived(entity, source, heal);
        entity.OnHealReceived.AddListener(listener);
        _listened[entity] = listener;
    }

    void OnHealReceived(Entity healed, GameObject source, ConsumerResult heal)
    {
        // Only the heals of the character: the copies come from the holder, so they are never copied again
        if (_owner == null || source == null || source.GetComponent<Character>() == null)
        {
            return;
        }

        List<GameObject> around = new List<GameObject>();
        foreach (Entity entity in RelativeCellPattern.FindEntities(_owner, GetAllies(), data.pattern, data.range, GetCellSize()))
        {
            if (entity != healed)
            {
                around.Add(entity.gameObject);
            }
        }
        LifeSteal.HealMostWounded(_owner.gameObject, around, heal.value * data.ratio);
    }

    protected virtual List<GameObject> GetAllies()
    {
        EntityManager entityManager = EntityManager.existingInstance;
        return entityManager != null && _owner != null ? entityManager.GetEntities(_owner.entityType) : new List<GameObject>();
    }

    protected virtual float GetCellSize()
    {
        PlayerBehaviour player = PlayerBehaviour.existingInstance;
        return player != null && player.grid != null ? player.grid.size : 1f;
    }
}
