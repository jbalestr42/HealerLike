using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Custom/Data/CharacterSkill/EntityBuffCharacterSkill")]
public class EntityBuffCharacterSkillFactory : CharacterSkillFactory<EntityBuffCharacterSkill, EntityBuffCharacterSkillData> {}

[Serializable]
public class EntityBuffCharacterSkillData
{
    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<ABuffHandlerFactory>, ABuffHandlerFactory>(buffHandlerFactory)")]
    public List<ABuffHandlerFactory> buffHandlerFactory;
    public Entity.EntityType entityType;
}

public class EntityBuffCharacterSkill : ACharacterSkill<EntityBuffCharacterSkillData>
{
    public override void Use(GameObject source, UnityAction<bool> onSkillComplete)
    {
        foreach (GameObject entity in EntityManager.instance.GetEntities(data.entityType))
        {
            foreach (ABuffHandlerFactory buffHandlerFactory in data.buffHandlerFactory)
            {
                entity.GetComponent<IBuffable>().AddBuffHandler(buffHandlerFactory, source, entity);
            }
        }
        onSkillComplete(true);
    }
}