using System;
using UnityEngine;
using Oisif.Inspector;

[CreateAssetMenu(menuName = "Custom/Data/CharacterSkillValidators/ResourceValidator")]
public class ResourceValidatorFactory : CharacterSkillValidatorFactory<ResourceValidator, ResourceValidatorData> {}

[Serializable]
public class ResourceValidatorData
{
    // The resource of the character the skill costs, or gives with a negative cost (e.g. each Heal gives 1 Devotion)
    public ResourceType resourceType = ResourceType.Mana;
    [CreateDataButton]
    public ConsumerFactory consumer;
    // The SkillCostMultiplier of the owner lowers or raises the cost (e.g. a mana cost, not a Devotion gain)
    public bool useCostMultiplier = true;
}

// The skill costs the value of the consumer in a resource of the character, a negative cost giving it instead. The
// first validator of a skill with a positive cost is the cost the skill shows
public class ResourceValidator : ACharacterSkillValidator<ResourceValidatorData>
{
    ResourceAttribute resource;
    GameObject _owner;
    // The skill shows this cost
    bool _showsCost;

    public override void Init(CharacterSkillState state, GameObject owner)
    {
        resource = ResourceAttribute.Get(owner, data.resourceType);
        _owner = owner;

        // A gain isn't a cost, and a skill shows a single one
        if (GetCost(owner) > 0f && !state.hasCost)
        {
            _showsCost = true;
            state.hasCost = true;
            state.cost = GetCost(owner);
        }
    }

    // The multiplier can change during the run (e.g. an item lowering the costs)
    public override void Update(CharacterSkillState state)
    {
        if (_showsCost)
        {
            state.cost = GetCost(_owner);
        }
    }

    // A character without the resource can't pay for it, a gain never blocks the skill
    public override bool IsValid(GameObject owner)
    {
        float cost = GetCost(owner);
        return cost <= 0f || (resource != null && resource.Value >= cost);
    }

    // A negative cost: the skill gives the resource
    public override bool IsGain(GameObject owner)
    {
        return GetCost(owner) < 0f;
    }

    public override void OnSkillUsed(GameObject owner)
    {
        if (resource != null)
        {
            resource.AddResourceModifier(ResourceModifier.Create(data.consumer, owner, owner, GetCostMultiplier(data, owner)));
        }
    }

    public float GetCost(GameObject owner)
    {
        return GetCost(data, owner);
    }

    // The cost of the data, with the SkillCostMultiplier of the owner applied when it uses it (the base cost without
    // one)
    public static float GetCost(ResourceValidatorData data, GameObject owner)
    {
        return data.consumer.data.value.GetValue(owner) * GetCostMultiplier(data, owner);
    }

    static float GetCostMultiplier(ResourceValidatorData data, GameObject owner)
    {
        return data.useCostMultiplier ? GetCostMultiplier(owner) : 1f;
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