using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/CharacterSkill/EntityBuffCharacterSkill")]
public class EntityBuffCharacterSkillFactory : CharacterSkillFactory<EntityBuffCharacterSkill, EntityBuffCharacterSkillData> {}

[Serializable]
public class EntityBuffCharacterSkillData : BaseCharacterSkillData
{
    [CreateDataButton]
    public ABuffHandlerFactory buffHandlerFactory;
    public Entity.EntityType entityType;
}

public class EntityBuffCharacterSkill : ACharacterSkill<EntityBuffCharacterSkillData>
{
    public override void Use()
    {
        foreach (GameObject entity in EntityManager.instance.GetEntities(data.entityType))
        {
            entity.GetComponent<IBuffable>().AddBuffHandler(data.buffHandlerFactory, entity, entity);
        }
    }
}