using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/CharacterSkill/MarkCharacterSkill")]
public class MarkCharacterSkillFactory : CharacterSkillFactory<MarkCharacterSkill, MarkCharacterSkillData> {}

[Serializable]
public class MarkCharacterSkillData : BaseCharacterSkillData
{
}

// Marks the chosen enemy: the units following the mark attack it first, until it dies or another one is marked
public class MarkCharacterSkill : BaseCharacterSkill<MarkCharacterSkillData>
{
    public override void ApplySkillOnTarget(GameObject source, GameObject target)
    {
        Mark(MarkManager.instance, target);
    }

    public static void Mark(MarkManager markManager, GameObject target)
    {
        if (markManager != null && target != null)
        {
            markManager.MarkEntity(target);
        }
    }
}
