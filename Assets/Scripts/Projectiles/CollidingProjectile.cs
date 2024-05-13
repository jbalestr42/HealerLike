using System;
using System.Collections.Generic;
using UnityEngine;

public class CollidingProjectile : Projectile
{
    void Start()
    {
        OnCollisionEnter.AddListener(ApplyOnHit);
    }
}