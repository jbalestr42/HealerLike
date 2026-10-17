// The consumers taken as they are: no armor, vulnerability nor any other damage reduction, never critical (e.g. the
// mana and the Devotion of the character). The gain multiplier still applies
public class RawResourceConsumerResolver : ResourceConsumerResolver
{
    public RawResourceConsumerResolver(AttributeType? gainMultiplierType = null) : base(gainMultiplierType)
    {
    }

    protected override void InitReductionsAndCriticals(AttributeManager attributeManager)
    {
    }

    protected override float ReduceDamage(float value)
    {
        return value;
    }

    protected override bool TryApplyCritical(ResourceModifier resourceModifier, ref float value)
    {
        return false;
    }
}
