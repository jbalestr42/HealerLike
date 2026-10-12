using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Buff/ShareHealOnRelativeCellBuff")]
public class ShareHealOnRelativeCellBuffFactory : BuffFactory<ShareHealOnRelativeCellBuff, ShareHealOnRelativeCellBuffData> { }

[Serializable]
public class ShareHealOnRelativeCellBuffData
{
    // Part of each heal received by the holder also healed on the allies on the cells of the pattern
    [Min(0)]
    public float ratio = 0.2f;
    public RelativeCellPatternType pattern = RelativeCellPatternType.Adjacent;
    [Min(1)]
    public int range = 1;
    // Smaller shared heals are dropped, so two holders next to each other stop sharing back and forth
    [Min(0)]
    public float minimumSharedHeal = 1f;
}

public class ShareHealOnRelativeCellBuff : ABuff<ShareHealOnRelativeCellBuffData>
{
    Entity _owner;

    public override void Instant(GameObject source, GameObject target) { }

    public override void Add(GameObject source, GameObject target)
    {
        _owner = target.GetComponent<Entity>();
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
        float sharedHeal = heal.value * data.ratio;
        if (sharedHeal <= 0f || sharedHeal < data.minimumSharedHeal)
        {
            return;
        }

        foreach (Entity entity in RelativeCellPattern.FindEntities(_owner, GetAllies(), data.pattern, data.range, GetCellSize()))
        {
            // A heal: ignores the armor and the invincibility
            ResourceModifier healModifier = new ResourceModifier { source = _owner.gameObject };
            healModifier.consumers.Add(new RuntimeConsumer(sharedHeal));
            entity.health.AddResourceModifier(healModifier);
        }
    }

    protected virtual List<GameObject> GetAllies()
    {
        EntityManager entityManager = EntityManager.existingInstance;
        return entityManager != null ? entityManager.GetEntities(_owner.entityType) : new List<GameObject>();
    }

    protected virtual float GetCellSize()
    {
        return Board.currentCellSize;
    }
}
