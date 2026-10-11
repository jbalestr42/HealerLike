using System.Collections.Generic;
using UnityEngine;

// Plays the character during a simulated fight, following the rules of its profile: every decision interval,
// casts the first usable skill whose conditions are met and which finds a target, like a click would
public class HealerBot
{
    readonly HealerBotProfile _profile;
    readonly Character _character;
    readonly EntityManager _entities;
    // Where a summon is raised, the nearest free cell being used
    readonly Vector3 _frontPosition;
    readonly Dictionary<HealerBotRule, CharacterSkillSlot> _slots = new Dictionary<HealerBotRule, CharacterSkillSlot>();
    readonly Dictionary<string, int> _casts = new Dictionary<string, int>();
    float _nextDecisionTime;

    // Number of casts per skill name
    public IReadOnlyDictionary<string, int> casts => _casts;

    public HealerBot(HealerBotProfile profile, Character character, EntityManager entities, Vector3 frontPosition)
    {
        _profile = profile;
        _character = character;
        _entities = entities;
        _frontPosition = frontPosition;
        foreach (HealerBotRule rule in profile.rules)
        {
            CharacterSkillSlot slot = FindSlot(rule.skill);
            if (slot != null)
            {
                _slots[rule] = slot;
            }
            else
            {
                Debug.LogWarning($"[HealerBot] {profile.name}: {(rule.skill != null ? rule.skill.name : "no skill")} isn't a skill of {character.data.title}");
            }
        }
    }

    // The slot of the character holding the skill: its data is the data of the skill factory
    CharacterSkillSlot FindSlot(ACharacterSkillFactory skill)
    {
        if (skill == null)
        {
            return null;
        }

        CharacterSkillData data = skill.Create().GetData();
        return _character.skillSlots.Find(slot => slot.data == data);
    }

    public void Update(float time)
    {
        if (time < _nextDecisionTime)
        {
            return;
        }
        _nextDecisionTime = HealerBotBrain.GetNextDecisionTime(_nextDecisionTime, time, _profile.decisionInterval);

        List<Entity> allies = GetLiving(Entity.EntityType.Player);
        List<Entity> enemies = GetLiving(Entity.EntityType.Computer);
        Dictionary<GameObject, BotUnit> strikes = GetStrikes(enemies);
        List<BotUnit> allyUnits = allies.ConvertAll(ally => ToBotUnit(ally, strikes));
        List<BotUnit> enemyUnits = enemies.ConvertAll(enemy => ToBotUnit(enemy, strikes));
        float manaPercent = _character.mana.Max > 0f ? _character.mana.Value / _character.mana.Max : 0f;

        foreach (HealerBotRule rule in _profile.rules)
        {
            if (!_slots.TryGetValue(rule, out CharacterSkillSlot slot) || !slot.CanUseSkill() || !HealerBotBrain.AreMet(rule.conditions, allyUnits, manaPercent))
            {
                continue;
            }

            if (TryCast(rule, slot, allies, enemies, allyUnits, enemyUnits))
            {
                _casts[slot.data.name] = _casts.TryGetValue(slot.data.name, out int count) ? count + 1 : 1;
                return;
            }
        }
    }

    bool TryCast(HealerBotRule rule, CharacterSkillSlot slot, List<Entity> allies, List<Entity> enemies, List<BotUnit> allyUnits, List<BotUnit> enemyUnits)
    {
        List<ABuffHandlerFactory> buffs = rule.skipTargetsWithItsBuff ? GetBuffs(slot.data) : null;
        bool isEnemyTarget = rule.target == HealerBotTarget.LowestHealthEnemy || rule.target == HealerBotTarget.HighestHealthEnemy;
        List<Entity> candidates = isEnemyTarget ? enemies : allies;
        int index = HealerBotBrain.PickTarget(rule.target, allyUnits, enemyUnits, i => buffs != null && HasAny(candidates[i], buffs));
        bool needsTarget = rule.target != HealerBotTarget.None && rule.target != HealerBotTarget.FrontCell;
        if (needsTarget && index < 0)
        {
            return false;
        }

        slot.UseSkill();
        AInteraction interaction = InteractionManager.instance.GetInteraction();
        if (interaction is SingleTargetInteraction singleTarget && needsTarget)
        {
            singleTarget.SelectTarget(candidates[index].gameObject);
        }
        else if (interaction is EntityGridInteraction grid)
        {
            grid.SpawnAt(_frontPosition);
        }
        else if (rule.target == HealerBotTarget.FrontCell)
        {
            // Refused before asking for a cell (e.g. too many summons alive)
            return false;
        }

        // A target the skill doesn't take: nothing is cast
        if (InteractionManager.instance.GetInteraction() != null)
        {
            InteractionManager.instance.CancelInteraction();
            return false;
        }
        return true;
    }

    static List<ABuffHandlerFactory> GetBuffs(CharacterSkillData data)
    {
        return data is BuffCharacterSkillData buffData && buffData.buffHandlerFactory != null ? buffData.buffHandlerFactory : new List<ABuffHandlerFactory>();
    }

    static bool HasAny(Entity entity, List<ABuffHandlerFactory> buffs)
    {
        return buffs.Exists(buff => buff != null && entity.buffManager.HasHandler(buff));
    }

    List<Entity> GetLiving(Entity.EntityType side)
    {
        List<Entity> living = new List<Entity>();
        foreach (GameObject unit in _entities.GetEntities(side))
        {
            Entity entity = unit != null ? unit.GetComponent<Entity>() : null;
            if (entity != null && entity.health != null && entity.health.Value > 0f)
            {
                living.Add(entity);
            }
        }
        return living;
    }

    // The units marked by the telegraphed strikes of the enemies, with the first strike on each: its seconds left
    // and the part of it the unit takes
    static Dictionary<GameObject, BotUnit> GetStrikes(List<Entity> enemies)
    {
        Dictionary<GameObject, BotUnit> strikes = new Dictionary<GameObject, BotUnit>();
        foreach (Entity enemy in enemies)
        {
            foreach (MarkedStrikeSkill strike in enemy.GetComponents<MarkedStrikeSkill>())
            {
                if (!strike.isMarking)
                {
                    continue;
                }
                foreach (StrikeMark mark in strike.marks)
                {
                    if (mark.target != null && (!strikes.TryGetValue(mark.target, out BotUnit first) || strike.remainingDelay < first.strikeIn))
                    {
                        strikes[mark.target] = new BotUnit { isMarked = true, strikeIn = strike.remainingDelay, strikeShare = mark.damageMultiplier };
                    }
                }
            }
        }
        return strikes;
    }

    static BotUnit ToBotUnit(Entity entity, Dictionary<GameObject, BotUnit> strikes)
    {
        strikes.TryGetValue(entity.gameObject, out BotUnit strike);
        return new BotUnit
        {
            health = entity.health.Value,
            maxHealth = entity.health.Max,
            isTank = entity.HasTag(TagNames.Tank),
            isMarked = strike.isMarked,
            strikeIn = strike.strikeIn,
            strikeShare = strike.strikeShare,
        };
    }
}
