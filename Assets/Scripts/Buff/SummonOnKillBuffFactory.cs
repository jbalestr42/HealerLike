using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Buff/SummonOnKillBuff")]
public class SummonOnKillBuffFactory : BuffFactory<SummonOnKillBuff, SummonOnKillBuffData> { }

[Serializable]
public class SummonOnKillBuffData
{
    // Summoned on the side of the holder, where each enemy it kills died
    public EntityData entity;
}

// Each enemy the holder kills rises as a summon of its side, gone at the end of the battle (e.g. a
// skeleton)
public class SummonOnKillBuff : ABuff<SummonOnKillBuffData>
{
    Entity _owner;

    public override void Instant(GameObject source, GameObject target) { }

    public override void Add(GameObject source, GameObject target)
    {
        _owner = target.GetComponent<Entity>();
        _owner.OnKill.AddListener(OnKill);
    }

    public override void Remove(GameObject source, GameObject target)
    {
        if (_owner != null)
        {
            _owner.OnKill.RemoveListener(OnKill);
        }
    }

    void OnKill(Entity killed)
    {
        if (killed == null || killed.entityType == _owner.entityType || data.entity == null)
        {
            return;
        }

        Spawn(killed.transform.position);
    }

    protected virtual GameObject Spawn(Vector3 position)
    {
        GameObject summon = EntityManager.instance.SpawnEntity(data.entity, position, _owner.entityType);
        if (summon != null)
        {
            // Entities are disabled at spawn until the battle starts, this one joins the running battle
            EntityManager.instance.AddSummon(summon.GetComponent<Entity>());
            summon.GetComponent<Entity>().Enable(true);
        }
        return summon;
    }
}
