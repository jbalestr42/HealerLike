using System.Collections.Generic;
using UnityEngine;

public class ResourceModifier
{
    public List<AConsumer> consumers = new List<AConsumer>();
    public float multiplier = 1f;
    public GameObject source;
}