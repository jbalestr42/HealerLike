using System;
using UnityEngine;
using Oisif.Inspector;

[CreateAssetMenu(menuName = "Custom/Data/CharacterSkillValidators/ResourceValidator")]
public class ResourceValidatorFactory : CharacterSkillValidatorFactory<ResourceValidator, ResourceValidatorData> {}

[Serializable]
public class ResourceValidatorData
{
    [CreateDataButton]
    public ConsumerFactory consumer;
}

// The skill costs the value of the consumer in mana, with the SkillCostMultiplier of the owner applied
public class ResourceValidator : ACharacterSkillValidator<ResourceValidatorData>
{
    ResourceAttribute resource;
    GameObject _owner;

    public override void Init(UseCharacterSkillButton skillButton, GameObject owner)
    {
        resource = owner.GetComponent<Character>().mana;
        _owner = owner;

        skillButton.hasCost = true;
        skillButton.SetCost(GetCost(owner));
    }

    // The multiplier can change during the run (e.g. an item lowering the costs)
    public override void Update(UseCharacterSkillButton skillButton)
    {
        skillButton.SetCost(GetCost(_owner));
    }

    public override bool IsValid(GameObject owner)
    {
        return resource.Value >= GetCost(owner);
    }

    public override void OnSkillUsed(GameObject owner)
    {
        resource.AddResourceModifier(ResourceModifier.Create(data.consumer, owner, owner, GetCostMultiplier(owner)));
    }

    public float GetCost(GameObject owner)
    {
        return data.consumer.data.value.GetValue(owner) * GetCostMultiplier(owner);
    }

    // The SkillCostMultiplier of the owner, 1 without one, never below 0: a skill can be free, never give mana
    public static float GetCostMultiplier(GameObject owner)
    {
        AttributeManager attributes = owner != null ? owner.GetComponent<AttributeManager>() : null;
        if (attributes == null || !attributes.Has(AttributeType.SkillCostMultiplier))
        {
            return 1f;
        }
        return Mathf.Max(0f, attributes.Get(AttributeType.SkillCostMultiplier).Value);
    }
}