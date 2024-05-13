using System;
using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/BuffHandler/DurationBuffHandler")]
public class DurationBuffHandlerFactory : BuffHandlerFactory<DurationBuffHandler, DurationBuffHandlerData> {}

[Serializable]
public class DurationBuffHandlerData : BuffHandlerBaseData
{
    public float duration;
}

public class DurationBuffHandler : ABuffHandler<DurationBuffHandlerData>
{
    float _timer = 0f;

    public override bool IsDone()
    {
        return _timer <= 0f;
    }

    public override void Refresh(GameObject source, GameObject target)
    {
        Debug.Log("[DEBUG] Refresh Time " + Time.time + " | " + _timer);
        _timer = data.duration;
    }

    public override void Start(GameObject source, GameObject target)
    {
        Debug.Log("[DEBUG] Start Time " + Time.time);
        _timer = data.duration;
    }

    public override void Update(float deltaTime)
    {
        _timer -= deltaTime;
    }

    public override void Stop(GameObject source, GameObject target)
    {
        Debug.Log("[DEBUG] Stop Time " + Time.time + " | " + _timer);
    }
}