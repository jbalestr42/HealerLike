using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/CharacterSkillValidators/ResourceValidator")]
public class ResourceValidatorFactory : CharacterSkillValidatorFactory<ResourceValidator, ResourceValidatorData> {}

[Serializable]
public class ResourceValidatorData
{
    [CreateDataButton]
    public SimpleConsumerFactory consumer;
}

public class ResourceValidator : ACharacterSkillValidator<ResourceValidatorData>
{
    ResourceAttribute resource;

    public override void Init(UseCharacterSkillButton skillButton, GameObject owner)
    {
        resource = owner.GetComponent<Character>().mana;

        skillButton.hasCost = true;
        skillButton.SetCost(data.consumer.data.value);
    }

    public override bool IsValid(GameObject owner)
    {
        return resource.Value >= data.consumer.data.value;
    }

    public override void OnSkillUsed(GameObject owner)
    {
        ResourceModifier resourceModifier = new ResourceModifier();
        resourceModifier.consumers.Add(data.consumer.GetConsumer(owner, owner));
        resourceModifier.multiplier = 1f;
        resourceModifier.source = owner;
        resource.AddResourceModifier(resourceModifier);
    }
}