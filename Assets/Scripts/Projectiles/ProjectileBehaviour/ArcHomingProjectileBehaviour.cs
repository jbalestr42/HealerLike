using System;
using UnityEngine;

[Serializable]
public class ArcHomingProjectileBehaviourData
{
    public float speed = 1f;
    public float angleMin = 5f;
    public float angleMax = 25f;
}

public class ArcHomingProjectileBehaviour : AProjectileBehaviour<ArcHomingProjectileBehaviourData>
{
    public override void Init(GameObject source)
    {
        projectile.OnUpdate.AddListener(OnUpdate);
    }

    Quaternion _initialRotation;
    float _launchAngle;
    Rigidbody _rigidbody;

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _initialRotation = transform.rotation;
        _launchAngle = UnityEngine.Random.Range(data.angleMin, data.angleMax);
    }

    void OnUpdate()
    {
        if (projectile.target)
        {
            Vector3 targetLocation = projectile.targetPoint.transform.position;

            transform.LookAt(targetLocation);

            Vector3 projectileXZPos = new Vector3(transform.position.x, targetLocation.y, transform.position.z);

            // Horizontal distance between projectile and target
            float R = Vector3.Distance(projectileXZPos, targetLocation);

            // Gravity
            float G = Physics.gravity.y;
            float tanAlpha = Mathf.Tan(_launchAngle * Mathf.Deg2Rad); // Can be cached

            // Vertical distance between projectile and target
            float H = targetLocation.y - transform.position.y;

            // Horizontal velocity
            float Vz = Mathf.Sqrt(G * R * R / (data.speed * (H - R * tanAlpha)));
            
            // Vertical velocity
            float Vy = tanAlpha * Vz;

            Vector3 localVelocity = new Vector3(0f, Vy, Vz);
            Vector3 globalVelocity = transform.TransformDirection(localVelocity);

            _rigidbody.velocity = globalVelocity;
            transform.rotation = Quaternion.LookRotation(_rigidbody.velocity) * _initialRotation;
        }
	}
}