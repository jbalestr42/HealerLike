using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/CharacterSkill/ShieldCharacterSkill")]
public class ShieldCharacterSkillFactory : CharacterSkillFactory<ShieldCharacterSkill, ShieldCharacterSkillData> {}

[Serializable]
public class ShieldCharacterSkillData : BuffCharacterSkillData
{
    // Spent all at once, each buff given once per point spent
    public ResourceType spentResource = ResourceType.Devotion;
}

// Shield of the Cleric: spends all the Devotion of the character, its single target getting one stack of each buff per
// Devotion spent (e.g. +15% armor each), none without Devotion
public class ShieldCharacterSkill : BaseCharacterSkill<ShieldCharacterSkillData>
{
    public override void ApplySkillOnTarget(GameObject source, GameObject target)
    {
        ResourceAttribute resource = ResourceAttribute.Get(source, data.spentResource);
        int stacks = resource != null ? Mathf.RoundToInt(resource.SpendAll(source)) : 0;
        foreach (ABuffHandlerFactory buffHandlerFactory in data.buffHandlerFactory)
        {
            for (int i = 0; i < stacks; i++)
            {
                target.GetComponent<IBuffable>().AddBuffHandler(buffHandlerFactory, source, target);
            }
        }
    }
}
