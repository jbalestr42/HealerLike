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
		if (projectile.target)
        {
            Vector3 direction = projectile.targetPoint.transform.position - transform.position;
            transform.position += direction.normalized * data.speed * Time.deltaTime;
            transform.LookAt(projectile.targetPoint.transform);
        }
	}
}