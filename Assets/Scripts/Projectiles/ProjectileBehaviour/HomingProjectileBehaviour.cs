using System;
using UnityEngine;

[Serializable]
public class HomingProjectileBehaviourData
{
    public float speed = 10f;
}

public class HomingProjectileBehaviour : AProjectileBehaviour<HomingProjectileBehaviourData>
{
    public override void Init(GameObject source)
    {
        projectile.OnUpdate.AddListener(OnUpdate);
    }

    void OnUpdate()
    {
        Advance(Time.deltaTime);
    }

    // Never goes past the target: a long frame (low frame rate, sped up game) lands on it instead of jumping over it
    public void Advance(float deltaTime)
    {
		if (projectile.target)
        {
            transform.position = Vector3.MoveTowards(transform.position, projectile.targetPoint.transform.position, data.speed * deltaTime);
            transform.LookAt(projectile.targetPoint.transform);
        }
	}
}