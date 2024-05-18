using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Custom/Data/CharacterSkill/SingleTargetBuffCharacterSkill")]
public class SingleTargetBuffCharacterSkillFactory : CharacterSkillFactory<SingleTargetBuffCharacterSkill, SingleTargetBuffCharacterSkillData> {}

[Serializable]
public class SingleTargetBuffCharacterSkillData
{
    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<ABuffHandlerFactory>, ABuffHandlerFactory>(buffHandlerFactory)")]
    public List<ABuffHandlerFactory> buffHandlerFactory;
    public Entity.EntityType entityType;
}

public class SingleTargetBuffCharacterSkill : ACharacterSkill<SingleTargetBuffCharacterSkillData>
{
    public override void Use(GameObject source, UnityAction<bool> onSkillComplete)
    {
        UnityAction<GameObject> onTargetSelected = (GameObject target) => 
        {
            foreach (ABuffHandlerFactory buffHandlerFactory in data.buffHandlerFactory)
            {
                target.GetComponent<IBuffable>().AddBuffHandler(buffHandlerFactory, source, target);
            }
        };

        InteractionManager.instance.SetInteraction(new SingleTargetInteraction(onTargetSelected, onSkillComplete));
    }
}