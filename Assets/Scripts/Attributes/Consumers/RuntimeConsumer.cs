// A consumer whose value is only known at runtime (e.g. a heal equal to the damage just dealt).
// Heals are positive, damage negative
public class RuntimeConsumer : AConsumer
{
    readonly float _value;
    readonly bool _ignoreDamageReduction;
    readonly bool _ignoreConsumerPrevention;
    readonly bool _canBeCritical;

    public RuntimeConsumer(float value, bool ignoreDamageReduction = true, bool ignoreConsumerPrevention = true, bool canBeCritical = true)
    {
        _value = value;
        _ignoreDamageReduction = ignoreDamageReduction;
        _ignoreConsumerPrevention = ignoreConsumerPrevention;
        _canBeCritical = canBeCritical;
    }

    public override float GetValue() => _value;
    public override bool ignoreDamageReduction => _ignoreDamageReduction;
    public override bool ignoreConsumerPrevention => _ignoreConsumerPrevention;
    public override bool canBeCritical => _canBeCritical;
}
