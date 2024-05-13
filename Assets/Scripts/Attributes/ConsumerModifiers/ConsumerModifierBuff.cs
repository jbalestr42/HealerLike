using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConsumerModifierBuff<ConsumerModifierType, ConsumerModifierDataType> : ABuff<ConsumerModifierDataType>
                                where ConsumerModifierType : AConsumerModifier<ConsumerModifierDataType>, new()
{
    ConsumerModifierType _controller;

    public override void Add(GameObject source, GameObject target)
    {
        _controller = new ConsumerModifierType() { data = data };
        _controller.Init(source, target);
        target.GetComponent<Entity>().health.AddConsumerModifier(_controller);
    }

    public override void Remove(GameObject source, GameObject target)
    {
        target.GetComponent<Entity>().health.RemoveConsumerModifier(_controller);
    }
}