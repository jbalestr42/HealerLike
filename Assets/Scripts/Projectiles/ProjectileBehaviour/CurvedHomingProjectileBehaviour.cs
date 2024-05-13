using System;
using UnityEngine;
using static UnityEngine.ParticleSystem;

[Serializable]
public class CurvedHomingProjectileBehaviourData
{
    public float speed = 10f;
    public float additionnalSpeedOverDistance = 10f;
}

public class CurvedHomingProjectileBehaviour : AProjectileBehaviour<CurvedHomingProjectileBehaviourData>
{
    [SerializeField] MinMaxCurve _minMaxHeightCurve;
    Vector3 _launchVector;
    Vector3 _sourcePosition;

    public override void Init(GameObject source)
    {
        projectile.OnUpdate.AddListener(OnUpdate);
        _launchVector = new Vector3(0f, 1f, UnityEngine.Random.Range(-0.5f, 0.5f));
        _sourcePosition = projectile.source.transform.position;
    }

    void OnUpdate()
    {
		if (projectile.target)
        {
            float normalizedDistance = GetNormalizedDistance(projectile.targetPoint.transform.position, transform.position, projectile.targetPoint.transform.position, _sourcePosition);

            Vector3 direction = (projectile.targetPoint.transform.position - transform.position).normalized;
            Vector3 curveDirection = _minMaxHeightCurve.Evaluate(1f - normalizedDistance) * (Vector3.up + Vector3.Cross(Vector3.up, transform.forward) * _launchVector.z);
            Vector3 deltaPosition = (direction + curveDirection) * (data.speed + data.additionnalSpeedOverDistance * normalizedDistance) * Time.deltaTime;
            transform.position += deltaPosition;
            transform.rotation = Quaternion.LookRotation(deltaPosition);
        }
	}

    // Compute the normalized distance [0, 1] where 'start' is 0, 'end' is 1 and 'value' is a point between start and end
    float GetNormalizedDistance(Vector3 from1, Vector3 to1, Vector3 from2, Vector3 to2)
    {
        return OptimizedHorizontalDistance(from1, to1) / OptimizedHorizontalDistance(from2, to2);
    }

    float OptimizedHorizontalDistance(Vector3 v1, Vector3 v2)
    {
        return (v2.x - v1.x) * (v2.x - v1.x) + (v2.z - v1.z) * (v2.z - v1.z);
    }
}