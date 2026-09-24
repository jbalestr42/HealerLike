using System;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // EntityManager raises OnProjectileSpawned before the caller sets an area's source and radius; Start runs once
    // they are set, in the same frame as the area's own Start, before its visual shows
    public class AreaPulseOnStart : MonoBehaviour
    {
        Action _pulse;

        public void Init(Action pulse)
        {
            _pulse = pulse;
        }

        void Start()
        {
            _pulse?.Invoke();
        }
    }
}
