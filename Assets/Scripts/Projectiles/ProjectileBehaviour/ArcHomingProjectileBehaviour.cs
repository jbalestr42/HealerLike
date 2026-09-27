using System;
using UnityEngine;

[Serializable]
public class ArcHomingProjectileBehaviourData
{
    // Horizontal distance traveled per second
    public float speed = 5f;
    public float angleMin = 5f;
    public float angleMax = 25f;
}

// Lob: a parabola from the launch point that lands on the target point, following it if it moves
public class ArcHomingProjectileBehaviour : AProjectileBehaviour<ArcHomingProjectileBehaviourData>
{
    Vector3 _start;
    float _launchAngle;
    float _traveledDistance = 0f;
    GameObject _currentTarget;

    public override void Init(GameObject source)
    {
        _launchAngle = UnityEngine.Random.Range(data.angleMin, data.angleMax);
        Launch();
        projectile.OnUpdate.AddListener(OnUpdate);
    }

    void OnUpdate()
    {
        Advance(Time.deltaTime);
    }

    public void Advance(float deltaTime)
    {
        if (!projectile.target)
        {
            return;
        }

        // A new target (the previous one died) starts a new arc from here
        if (projectile.target != _currentTarget)
        {
            Launch();
        }

        Vector3 end = projectile.targetPoint.transform.position;
        float distance = GetHorizontalDistance(_start, end);
        _traveledDistance += data.speed * deltaTime;
        float ratio = distance > 0f ? Mathf.Clamp01(_traveledDistance / distance) : 1f;

        Vector3 position = GetArcPosition(_start, end, ratio, GetApexHeight(distance, _launchAngle));
        if (position != transform.position)
        {
            transform.rotation = Quaternion.LookRotation(position - transform.position);
        }
        transform.position = position;
    }

    void Launch()
    {
        _start = transform.position;
        _traveledDistance = 0f;
        _currentTarget = projectile.target;
    }

    public static float GetHorizontalDistance(Vector3 from, Vector3 to)
    {
        return Vector2.Distance(new Vector2(from.x, from.z), new Vector2(to.x, to.z));
    }

    // Height of the top of the arc above the start-end line, for a launch at this angle
    public static float GetApexHeight(float distance, float launchAngle)
    {
        return distance * Mathf.Tan(launchAngle * Mathf.Deg2Rad) / 4f;
    }

    // ratio goes from 0 (start) to 1 (end)
    public static Vector3 GetArcPosition(Vector3 start, Vector3 end, float ratio, float apexHeight)
    {
        return Vector3.Lerp(start, end, ratio) + Vector3.up * (4f * apexHeight * ratio * (1f - ratio));
    }
}
