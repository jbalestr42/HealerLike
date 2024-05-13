using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Modifier/CurrentWaveModifier")]
public class CurrentWaveModifierFactory : BuffFactory<AttributeModifierBuff<CurrentWaveModifier, CurrentWaveModifierData>, CurrentWaveModifierData> { }

[Serializable]
public class CurrentWaveModifierData : BaseData
{
    public float value;
}

public class CurrentWaveModifier : AttributeModifier<CurrentWaveModifierData>
{
    public override float ApplyModifier()
    {
        return data.value * Math.GetProgressionFactorFromWave(GameManager.instance.gameType.currentWave);
    }
}