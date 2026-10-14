using System;
using System.Collections.Generic;
using UnityEngine;
using Oisif.Inspector;

[Serializable]
public class PurifySkillData : SkillDataBase
{
    // The handlers with this tag (or one of its descendants) are the debuffs it removes
    public GameplayTag debuffTag;
    // One of them, picked at random, given to a random ally on each use
    [CreateDataButton]
    public List<ABuffHandlerFactory> buffHandlerFactories = new List<ABuffHandlerFactory>();
    // Also given to the ally getting the buff, none: nothing more (e.g. Blessed)
    public StatusData status;
    [Min(0.1f)]
    public float rate = 5f;
}

// Purifier: on each use, removes one debuff from a random debuffed ally, then gives a random buff of its list to a
// random ally other than itself, along with its status (e.g. Blessed). That ally is drawn ahead of the use, for its line to be shown before (see SkillTargetLines)
public class PurifySkill : ACooldownSkill<PurifySkillData>, ISkillTargetPreview
{
    // The ally the next buff goes to, drawn again once it is given or the ally dies
    Entity _upcomingAlly;

    public override float cooldownDuration => data.rate;

    public override bool Execute(GameObject source)
    {
        List<Entity> allies = GetLivingAllies();
        if (allies.Count == 0)
        {
            return false;
        }

        RemoveOneDebuff(allies);
        GiveRandomBuff(allies);
        return true;
    }

    // A random debuff of a random debuffed ally, false without any
    public bool RemoveOneDebuff(List<Entity> allies)
    {
        List<(BuffManager, BuffManager.BuffHandlerData)> debuffs = new List<(BuffManager, BuffManager.BuffHandlerData)>();
        foreach (Entity ally in allies)
        {
            BuffManager buffManager = ally.GetComponent<BuffManager>();
            if (buffManager == null || data.debuffTag == null)
            {
                continue;
            }

            foreach (BuffManager.BuffHandlerData handler in buffManager.GetActiveHandlers())
            {
                if (handler.buffHandlerFactory != null && handler.buffHandlerFactory.HasTag(data.debuffTag))
                {
                    debuffs.Add((buffManager, handler));
                }
            }
        }
        if (debuffs.Count == 0)
        {
            return false;
        }

        (BuffManager owner, BuffManager.BuffHandlerData debuff) = debuffs[UnityEngine.Random.Range(0, debuffs.Count)];
        owner.RemoveBuff(handler => handler == debuff);
        return true;
    }

    // A random buff of the list on a random ally other than the purifier, its source; null without buff or such ally
    public ABuffHandlerFactory GiveRandomBuff(List<Entity> allies)
    {
        List<ABuffHandlerFactory> buffs = data.buffHandlerFactories.FindAll(buff => buff != null);
        if (buffs.Count == 0 || allies.Count == 0)
        {
            return null;
        }

        ABuffHandlerFactory buff = buffs[UnityEngine.Random.Range(0, buffs.Count)];
        Entity ally = GetUpcomingAlly(allies);
        _upcomingAlly = null;
        BuffManager buffManager = ally != null ? ally.GetComponent<BuffManager>() : null;
        if (buffManager == null)
        {
            return null;
        }

        buffManager.AddHandler(buff, gameObject, ally.gameObject);
        ally.ApplyStatus(data.status, gameObject);
        return buff;
    }

    // The ally drawn for the next buff while it is one of the allies, else a new one drawn at random, never the purifier
    Entity GetUpcomingAlly(List<Entity> allies)
    {
        if (_upcomingAlly == null || !allies.Contains(_upcomingAlly))
        {
            List<Entity> others = allies.FindAll(ally => ally.gameObject != gameObject);
            _upcomingAlly = others.Count > 0 ? others[UnityEngine.Random.Range(0, others.Count)] : null;
        }
        return _upcomingAlly;
    }

    List<Entity> GetLivingAllies()
    {
        List<Entity> living = new List<Entity>();
        foreach (GameObject ally in GetAllies())
        {
            Entity entity = ally != null ? ally.GetComponent<Entity>() : null;
            if (entity != null && entity.health != null && entity.health.Value > 0f)
            {
                living.Add(entity);
            }
        }
        return living;
    }

    protected virtual List<GameObject> GetAllies()
    {
        Entity self = GetComponent<Entity>();
        EntityManager entityManager = EntityManager.existingInstance;
        return entityManager != null && self != null ? entityManager.GetEntities(self.entityType) : new List<GameObject>();
    }

    #region ISkillTargetPreview

    // The ally of the next buff, null without buff to give or living ally
    public GameObject GetUpcomingTarget()
    {
        if (!data.buffHandlerFactories.Exists(buff => buff != null))
        {
            return null;
        }
        Entity ally = GetUpcomingAlly(GetLivingAllies());
        return ally != null ? ally.gameObject : null;
    }

    #endregion
}
