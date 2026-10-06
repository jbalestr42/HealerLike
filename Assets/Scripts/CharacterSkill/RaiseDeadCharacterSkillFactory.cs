using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Custom/Data/CharacterSkill/RaiseDeadCharacterSkill")]
public class RaiseDeadCharacterSkillFactory : CharacterSkillFactory<RaiseDeadCharacterSkill, RaiseDeadCharacterSkillData> {}

[Serializable]
public class RaiseDeadCharacterSkillData : CharacterSkillData
{
    public EntityData entity;
    // Summons alive at the same time, the skill can't be used again until one dies
    [MinValue(1)]
    public int maxAlive = 2;
    // Stats added to the summon per Heal Power of the character
    public float healthPerHealPower = 3f;
    public float damagePerHealPower = 0.1f;
}

// Summons an ally on the clicked cell, stronger with the Heal Power of the character. The summon
// only lasts for the current battle
public class RaiseDeadCharacterSkill : ACharacterSkill<RaiseDeadCharacterSkillData>
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

    public override void Use(GameObject source, UnityAction<bool> onSkillComplete)
    {
        if (aliveCount >= data.maxAlive)
        {
            onSkillComplete(false);
            return;
        }

        Character character = source.GetComponent<Character>();
        InteractionManager.instance.SetInteraction(new EntityGridInteraction(data.entity, Entity.EntityType.Player, false, summon =>
        {
            AttributeManager attributes = character.attributeManager;
            float healPower = attributes.Has(AttributeType.HealPower) ? attributes.Get(AttributeType.HealPower).Value : 0f;
            // The character is only enabled during a battle
            Raise(summon, healPower, data, character.buffManager.isEnabled);
            EntityManager.instance.AddSummon(summon);
            AddSummon(summon.gameObject);
            onSkillComplete(true);
        }));
    }

    public void AddSummon(GameObject summon)
    {
        _summons.Add(summon);
    }

    public static void Raise(Entity summon, float healPower, RaiseDeadCharacterSkillData data, bool isBattleRunning)
    {
        Empower(summon.attributeManager, healPower, data);
        // Entities are disabled at spawn until the battle starts, a summon joins the running battle
        summon.Enable(isBattleRunning);
    }

    public static void Empower(AttributeManager attributes, float healPower, RaiseDeadCharacterSkillData data)
    {
        AddToAttribute(attributes, AttributeType.HealthMax, healPower * data.healthPerHealPower);
        AddToAttribute(attributes, AttributeType.Damage, healPower * data.damagePerHealPower);
    }

    static void AddToAttribute(AttributeManager attributes, AttributeType type, float value)
    {
        Attribute attribute = attributes.GetOrAdd(type);
        attribute.BaseValue += value;
        attribute.Update();
    }
}
