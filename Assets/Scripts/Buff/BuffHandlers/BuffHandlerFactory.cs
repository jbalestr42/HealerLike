using System;
using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/BuffHandler/BuffHandler")]
public class BuffHandlerFactory : BuffHandlerFactory<BuffHandler, BuffHandlerData> {}

[Serializable]
public class BuffHandlerData : BuffHandlerBaseData
{
}

public class BuffHandler : ABuffHandler<BuffHandlerData>
{
    public override bool IsDone()
    {
        return false;
    }

    public override void Refresh(GameObject source, GameObject target)
    {
    }

    public override void Start(GameObject source, GameObject target)
    {
    }

    public override void Update(float deltaTime)
    {
    }

    public override void Stop(GameObject source, GameObject target)
    {
    }
}