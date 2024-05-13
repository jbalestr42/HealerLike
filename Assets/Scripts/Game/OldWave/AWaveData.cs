using System.Collections.Generic;
using UnityEngine;

public abstract class AWaveData : ScriptableObject
{
    public int minimumWaveApparition;
    public abstract int count { get; }
    public abstract AWaveStep GetStep(int index);
}

public abstract class AWaveData<Data> : AWaveData where Data : AWaveStep
{
    public List<Data> steps;
    public override int count => steps.Count;

    public override AWaveStep GetStep(int index)
    {
        return steps[index];
    }
}