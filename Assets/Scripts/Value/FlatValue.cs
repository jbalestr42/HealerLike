using System;
using UnityEngine;

[Serializable]
public class FlatValueData
{
    public float value;
}

[CreateAssetMenu(menuName = "Custom/Data/Value/FlatValue")]
public class FlatValue : AValue<FlatValueData>
{
    public override float GetValue(GameObject source)
    {
        return data.value;
    }
}
