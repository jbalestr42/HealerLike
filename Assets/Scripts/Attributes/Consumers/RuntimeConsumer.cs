// A consumer whose value is only known at runtime (e.g. a heal equal to the damage just dealt).
// Heals are positive, damage negative
public class RuntimeConsumer : AConsumer<ConsumerBaseData>
{
    readonly float _value;

    public RuntimeConsumer(float value, bool ignoreDamageReduction = true, bool ignoreConsumerPrevention = true, bool canBeCritical = true)
    {
        _value = value;
        data = new ConsumerBaseData
        {
            ignoreDamageReduction = ignoreDamageReduction,
            ignoreConsumerPrevention = ignoreConsumerPrevention,
            canBeCritical = canBeCritical,
        };
    }

    public override float GetValue() => _value;
}
