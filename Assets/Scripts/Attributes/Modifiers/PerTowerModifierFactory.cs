using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Modifier/PerTowerModifier")]
public class PerTowerModifierFactory : BuffFactory<AttributeModifierBuff<PerTowerModifier, PerTowerModifierData>, PerTowerModifierData> { }

[Serializable]
public class PerTowerModifierData : BaseData
{
    public float multiplierPerTowerModifier;
}

public class PerTowerModifier : AttributeModifier<PerTowerModifierData>
{
    public override float ApplyModifier()
    {
        return data.multiplierPerTowerModifier * EntityManager.instance.towers.Count;
    }
}
