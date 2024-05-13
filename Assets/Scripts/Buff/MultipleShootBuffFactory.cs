using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Buff/MultipleShootBuff")]
public class MultipleShootBuffFactory : BuffFactory<MultipleShootBuff, MultipleShootBuffData> {}

[Serializable]
public class MultipleShootBuffData
{
    public int value;
}

public class MultipleShootBuff : ABuff<MultipleShootBuffData>, IStackableBuff
{
    public override void Add(GameObject source, GameObject target)
    {
        target.GetComponent<ITargetProvider>().targetCount += data.value;
    }

    public override void Remove(GameObject source, GameObject target)
    {
        target.GetComponent<ITargetProvider>().targetCount -= data.value;
    }

    public void Stack(GameObject source, GameObject target)
    {
        target.GetComponent<ITargetProvider>().targetCount += data.value;
    }

    public void Unstack(GameObject source, GameObject target)
    {
        target.GetComponent<ITargetProvider>().targetCount -= data.value;
    }
}