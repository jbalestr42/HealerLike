using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Oisif.Inspector;

[CreateAssetMenu(menuName = "Custom/Data/CharacterSkill/BuffCharacterSkill")]
public class BuffCharacterSkillFactory : CharacterSkillFactory<BuffCharacterSkill, BuffCharacterSkillData> {}

[Serializable]
public class BuffCharacterSkillData : BaseCharacterSkillData
{
    [CreateDataButton]
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