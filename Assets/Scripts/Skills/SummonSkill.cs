using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SummonSkillData : SkillDataBase
{
    public EntityData entity;
    public float cooldown = 6f;
    // Summons of the caster alive at the same time, it waits for one to die to summon again
    [Min(1)]
    public int maxAlive = 2;
}

// Summons an entity of the caster's side on the free cell closest to it, never outside the grid
public class SummonSkill : ACooldownSkill<SummonSkillData>
{
    readonly List<GameObject> _summons = new List<GameObject>();

    public int aliveCount
    {
        get
        {
            // Dead summons are destroyed
            _summons.RemoveAll(summon => summon == null);
            return _summons.Count;
        }
    }

    public override bool Execute(GameObject source)
    {
        if (aliveCount >= data.maxAlive)
        {
            return false;
        }

        GridCell cell = grid.GetNearestWalkableCell(transform.position);
        if (cell == null)
        {
            return false;
        }

        // On the caster's plane, like the other entities of its side
        GameObject summon = Spawn(new Vector3(cell.center.x, transform.position.y, cell.center.z));
        if (summon == null)
        {
            return false;
        }

        _summons.Add(summon);
        return true;
    }

    public override float cooldownDuration => data.cooldown;

    protected virtual GridManager grid => PlayerBehaviour.instance.grid;

    protected virtual GameObject Spawn(Vector3 position)
    {
        GameObject summon = EntityManager.instance.SpawnEntity(data.entity, position, GetComponent<Entity>().entityType);
        if (summon != null)
        {
            // Entities are disabled at spawn until the battle starts, this one joins the running battle
            EntityManager.instance.AddSummon(summon.GetComponent<Entity>());
            summon.GetComponent<Entity>().Enable(true);
        }
        return summon;
    }
}
