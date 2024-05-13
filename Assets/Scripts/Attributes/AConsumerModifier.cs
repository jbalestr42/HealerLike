using UnityEngine;

public abstract class AConsumerModifier
{
    public abstract void Init(GameObject source, GameObject target); 
    public abstract float ApplyController(AConsumer consumer, float value);
}

public abstract class AConsumerModifier<DataType> : AConsumerModifier
{
    public DataType data;
}