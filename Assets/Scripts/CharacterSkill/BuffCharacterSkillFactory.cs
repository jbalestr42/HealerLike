using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Custom/Data/CharacterSkill/BuffCharacterSkill")]
public class BuffCharacterSkillFactory : CharacterSkillFactory<BuffCharacterSkill, BuffCharacterSkillData> {}

[Serializable]
public class BuffCharacterSkillData : BaseCharacterSkillData
{
    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<ABuffHandlerFactory>, ABuffHandlerFactory>(buffHandlerFactory)")]
    public List<ABuffHandlerFactory> buffHandlerFactory;
}

public class BuffCharacterSkill : BaseCharacterSkill<BuffCharacterSkillData>
{
    public override void ApplySkillOnTarget(GameObject source, GameObject target)
    {
        foreach (ABuffHandlerFactory buffHandlerFactory in data.buffHandlerFactory)
        {
            target.GetComponent<IBuffable>().AddBuffHandler(buffHandlerFactory, source, target);
        }
    }
}