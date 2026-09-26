using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/CharacterSkillValidators/ResourceValidator")]
public class ResourceValidatorFactory : CharacterSkillValidatorFactory<ResourceValidator, ResourceValidatorData> {}

[Serializable]
public class ResourceValidatorData
{
    [CreateDataButton]
    public ConsumerFactory consumer;
}

public class ResourceValidator : ACharacterSkillValidator<ResourceValidatorData>
{
    ResourceAttribute resource;

    public override void Init(UseCharacterSkillButton skillButton, GameObject owner)
    {
        resource = owner.GetComponent<Character>().mana;

        skillButton.hasCost = true;
        skillButton.SetCost(data.consumer.data.value.GetValue(owner));
    }

    public override bool IsValid(GameObject owner)
    {
        return resource.Value >= data.consumer.data.value.GetValue(owner);
    }

    public override void OnSkillUsed(GameObject owner)
    {
        resource.AddResourceModifier(ResourceModifier.Create(data.consumer, owner, owner));
    }
}