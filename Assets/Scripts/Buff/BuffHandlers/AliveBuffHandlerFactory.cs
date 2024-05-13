using System;
using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/BuffHandler/AliveBuffHandler")]
public class AliveBuffHandlerFactory : BuffHandlerFactory<AliveBuffHandler, AliveBuffHandlerData> {}

[Serializable]
public class AliveBuffHandlerData : BuffHandlerBaseData
{
}

public class AliveBuffHandler : ABuffHandler<AliveBuffHandlerData>
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