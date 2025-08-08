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
        var gameType = GameManager.instance.gameType as AscensionGameType;
        if (gameType != null)
        {
            return data.value * Math.GetProgressionFactorFromWave(gameType.currentRound);
        }
        return 1f;
    }
}