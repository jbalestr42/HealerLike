using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AttributeModifier
{
    public virtual void Init(GameObject source, GameObject target) {}
    public abstract float ApplyModifier();
}

public abstract class AttributeModifier<DataType> : AttributeModifier
{
    public DataType data;
}