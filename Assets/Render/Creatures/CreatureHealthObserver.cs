using System;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public class CreatureHealthObserver : IDisposable
    {
        ResourceAttribute _health;
        CreatureRig _rig;

        public void Init(ResourceAttribute health, CreatureRig rig)
        {
            if (_health == health && _rig == rig)
            {
                return;
            }

            Dispose();
            _health = health;
            _rig = rig;
            if (_health != null)
            {
                _health.OnAllConsumerProcessed.AddListener(OnHealthProcessed);
            }
        }

        public void Dispose()
        {
            if (_health != null)
            {
                _health.OnAllConsumerProcessed.RemoveListener(OnHealthProcessed);
            }

            _health = null;
            _rig = null;
        }

        void OnHealthProcessed(GameObject owner, ResourceModifier modifier, ConsumerResult result)
        {
            float value = result.value;
            bool critical = result.isCritical;
            if (_rig != null && float.IsFinite(value) && value < 0f)
            {
                _rig.Hit();
            }
        }
    }
}
