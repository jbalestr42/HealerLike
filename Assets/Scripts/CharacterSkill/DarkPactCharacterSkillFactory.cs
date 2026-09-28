using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Custom/Data/CharacterSkill/DarkPactCharacterSkill")]
public class DarkPactCharacterSkillFactory : CharacterSkillFactory<DarkPactCharacterSkill, DarkPactCharacterSkillData> {}

[Serializable]
public class DarkPactCharacterSkillData : CharacterSkillData
{
    // Part of their max health every ally sacrifices
    public float healthPercent = 0.15f;
    // Mana gained per health sacrificed
    public float manaPerHealth = 0.5f;
    public Entity.EntityType entityType = Entity.EntityType.Player;
}

// Every ally sacrifices part of its health, turned into mana for the character
public class DarkPactCharacterSkill : ACharacterSkill<DarkPactCharacterSkillData>
{
    public override void Use(GameObject source, UnityAction<bool> onSkillComplete)
    {
        List<ResourceAttribute> healths = new List<ResourceAttribute>();
        foreach (GameObject entity in EntityManager.instance.GetEntities(data.entityType))
        {
            healths.Add(entity.GetComponent<Entity>().health);
        }
        Pact(healths, source.GetComponent<Character>().mana, data.healthPercent, data.manaPerHealth);
        onSkillComplete(true);
    }

    // The health is taken directly (a sacrifice ignores armor and invincibility) and never kills:
    // every ally keeps at least 1 HP. Returns the health sacrificed
    public static float Pact(List<ResourceAttribute> healths, ResourceAttribute mana, float healthPercent, float manaPerHealth)
    {
        float sacrificed = 0f;
        foreach (ResourceAttribute health in healths)
        {
            if (health == null || health.Value <= 0f)
            {
                continue;
            }
            float loss = Mathf.Clamp(health.Max * healthPercent, 0f, Mathf.Max(0f, health.Value - 1f));
            health.SetValue(health.Value - loss);
            sacrificed += loss;
        }
        mana.SetValue(mana.Value + sacrificed * manaPerHealth);
        return sacrificed;
    }
}
