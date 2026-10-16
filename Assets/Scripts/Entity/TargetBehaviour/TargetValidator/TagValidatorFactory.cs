using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/TargetValidator/TagValidator")]
public class TagValidatorFactory : TargetValidatorFactory<TagValidator, TagValidatorData> {}

[Serializable]
public class TagValidatorData
{
    public GameplayTag tag;
    // false: only targets without the tag
    public bool mustHave = true;
}

// Targets by tag, from the entity data or added in the fight (e.g. the Shield Wardens only recharging the Iron Matron)
public class TagValidator : ATargetValidator<TagValidatorData>
{
    public override bool IsValid(GameObject source, GameObject target)
    {
        Entity entity = target != null ? target.GetComponent<Entity>() : null;
        bool hasTag = entity != null && entity.HasTag(data.tag);
        return hasTag == data.mustHave;
    }
}
