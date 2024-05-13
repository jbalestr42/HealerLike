using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Buff/InvincibilityBuff")]
public class InvincibilityBuffFactory : BuffFactory<InvincibilityBuff, InvincibilityBuffData> {}

[Serializable]
public class InvincibilityBuffData
{
}

public class InvincibilityBuff : ABuff<InvincibilityBuffData>
{
    public override void Add(GameObject source, GameObject target)
    {
        target.GetComponent<Entity>().health.preventConsumers = true;
    }

    public override void Remove(GameObject source, GameObject target)
    {
        target.GetComponent<Entity>().health.preventConsumers = false;
    }
}