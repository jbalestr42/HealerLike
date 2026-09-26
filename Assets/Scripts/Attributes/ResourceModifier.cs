using System.Collections.Generic;
using UnityEngine;

public class ResourceModifier
{
    public List<AConsumer> consumers = new List<AConsumer>();
    public float multiplier = 1f;
    public GameObject source;

    // Modifier holding the single consumer given by the factory, sent from source to target
    public static ResourceModifier Create(AConsumerFactory consumerFactory, GameObject source, GameObject target, float multiplier = 1f)
    {
        ResourceModifier resourceModifier = new ResourceModifier();
        resourceModifier.consumers.Add(consumerFactory.GetConsumer(source, target));
        resourceModifier.multiplier = multiplier;
        resourceModifier.source = source;
        return resourceModifier;
    }
}
