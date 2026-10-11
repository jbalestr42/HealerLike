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

// Summons an entity of the caster's side on the free cell of its side closest to it, never outside the board
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

        if (!TryGetSummonPosition(out Vector3 position))
        {
            return false;
        }

        GameObject summon = Spawn(position);
        if (summon == null)
        {
            return false;
        }

        _summons.Add(summon);
        return true;
    }

    public override float cooldownDuration => data.cooldown;

    protected virtual GridManager grid => PlayerBehaviour.instance.grid;
    // The board of the scene, the grid only without one
    protected virtual Board board => EntityManager.instance.board;
    protected virtual Entity.EntityType casterType => GetComponent<Entity>().entityType;

    // The free cell of the caster's side closest to it
    bool TryGetSummonPosition(out Vector3 position)
    {
        Board summonBoard = board;
        if (summonBoard != null)
        {
            return summonBoard.TryGetPlacementPosition(transform.position, BoardLayout.GetCellType(casterType), out position);
        }

        GridCell cell = grid.GetNearestWalkableCell(transform.position);
        // On the caster's plane, like the other entities of its side
        position = cell != null ? new Vector3(cell.center.x, transform.position.y, cell.center.z) : transform.position;
        return cell != null;
    }

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
