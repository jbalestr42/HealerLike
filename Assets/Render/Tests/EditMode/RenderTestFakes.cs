using System.Collections.Generic;
using HealerLike.Render.Zones;
using UnityEngine;

namespace HealerLike.Render
{

// Counts every call a spell sink gets and keeps the last arguments
public class RecordingSpellSink : ISpellVisualSink
{
    public int impactCount;
    public int statusCount;
    public int removeCount;
    public int pulseCount;
    public GameObject lastSource;
    public GameObject lastTarget;
    public ResourceKind lastResource;
    public float lastAmount;
    public bool lastCritical;
    public ZoneKind lastZoneKind;

    public void ShowImpact(GameObject source, GameObject target, ResourceKind resource, float preClampAmount,
        bool isCritical)
    {
        impactCount++;
        lastSource = source;
        lastTarget = target;
        lastResource = resource;
        lastAmount = preClampAmount;
        lastCritical = isCritical;
    }

    public void SetStatus(GameObject source, GameObject target, ABuffHandlerFactory factory, int stacks,
        float elapsedSeconds, float durationSeconds)
    {
        statusCount++;
        lastSource = source;
        lastTarget = target;
    }

    public void RemoveStatus(GameObject source, GameObject target, ABuffHandlerFactory factory)
    {
        removeCount++;
    }

    public void PulseArea(Vector3 center, float radius, ZoneKind kind, float strength)
    {
        pulseCount++;
        lastZoneKind = kind;
    }
}

// Records every health outcome it is sent; like the sinks that draw heals, only a positive change counts as one
public class RecordingHealthSink : IHealthVisualSink
{
    public readonly List<(GameObject target, float value, bool critical)> calls =
        new List<(GameObject, float, bool)>();
    public int healCount;

    public void OnHealthResolved(GameObject target, float value, bool critical)
    {
        calls.Add((target, value, critical));
        if (value > 0f)
        {
            healCount++;
        }
    }
}

}
