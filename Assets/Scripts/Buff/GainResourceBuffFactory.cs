using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Buff/GainResourceBuff")]
public class GainResourceBuffFactory : BuffFactory<GainResourceBuff, GainResourceBuffData> { }

[Serializable]
public class GainResourceBuffData
{
    // Resource of the target that gains (e.g. its Shield)
    public ResourceType resourceType = ResourceType.Shield;
    // Amount gained, read on the value owner (e.g. 10% of the max health of the target)
    [SerializeReference]
    public AValue value;
    public ConsumerValueOwner valueOwner = ConsumerValueOwner.Target;
    // Raises the resource up to the amount instead of adding it, nothing when it already has as much (e.g. a
    // shield recharged to 10% of the max health, never piling up)
    public bool upToValue;
}

// Gives some of a resource to its target when applied (e.g. the Shield Wardens recharging the shield of the Iron
// Matron), or tops it up to that amount, through a consumer as any gain, never critical. Nothing on a target without that resource
public class GainResourceBuff : ABuff<GainResourceBuffData>
{
    public override void Instant(GameObject source, GameObject target)
    {
        ResourceAttribute resource = ResourceAttribute.Get(target, data.resourceType);
        GameObject owner = data.valueOwner == ConsumerValueOwner.Target ? target : source;
        if (resource == null || owner == null || data.value == null)
        {
            return;
        }

        float gain = data.value.GetValue(owner);
        if (data.upToValue)
        {
            gain -= resource.Value;
        }
        if (gain <= 0f)
        {
            return;
        }
        ResourceModifier gainModifier = new ResourceModifier { source = source };
        gainModifier.consumers.Add(new RuntimeConsumer(gain, canBeCritical: false));
        resource.AddResourceModifier(gainModifier);
    }

    public override void Add(GameObject source, GameObject target) { }

    public override void Remove(GameObject source, GameObject target) { }
}
