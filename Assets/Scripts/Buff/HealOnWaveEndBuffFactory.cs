using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Buff/HealOnWaveEndBuff")]
public class HealOnWaveEndBuffFactory : BuffFactory<HealOnWaveEndBuff, HealOnWaveEndBuffData> {}

[Serializable]
public class HealOnWaveEndBuffData
{
    public int value;
}

public class HealOnWaveEndBuff : ABuff<HealOnWaveEndBuffData>, IStackableBuff
{
    int _stacks = 1;

    void OnWaveEnd()
    {
        PlayerBehaviour.instance.life += data.value * _stacks;
    }

    public override void Add(GameObject source, GameObject target)
    {
        WaveGameType.OnWaveEnd.AddListener(OnWaveEnd);
    }

    public override void Remove(GameObject source, GameObject target)
    {
        WaveGameType.OnWaveEnd.RemoveListener(OnWaveEnd);
    }

    public void Stack(GameObject source, GameObject target)
    {
        _stacks++;
    }

    public void Unstack(GameObject source, GameObject target)
    {
        _stacks--;
    }
}