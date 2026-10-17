using System;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Custom/Data/CharacterSkill/BalanceLifeCharacterSkill")]
public class BalanceLifeCharacterSkillFactory : CharacterSkillFactory<BalanceLifeCharacterSkill, BalanceLifeCharacterSkillData> {}

[Serializable]
public class BalanceLifeCharacterSkillData : CharacterSkillData
{
    public Entity.EntityType entityType = Entity.EntityType.Player;
    public BalanceLifeMode mode = BalanceLifeMode.Relative;
}

// Shares the health of the team: every unit ends up at the average health percent of the team
public class BalanceLifeCharacterSkill : ACharacterSkill<BalanceLifeCharacterSkillData>
{
    public override void Use(GameObject source, UnityAction<bool> onSkillComplete)
    {
        HealthBalance.Balance(EntityManager.instance.GetEntities(data.entityType), data.mode);
        onSkillComplete(true);
    }
}
