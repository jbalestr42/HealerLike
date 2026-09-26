using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    public class StageRestObservation : IDisposable
    {
        readonly StageMapSession _session;
        readonly List<ResourceAttribute> _health = new List<ResourceAttribute>();
        public StageRestObservation(StageMapSession session)
        {
            _session = session;
            foreach (GameObject ally in session.manager.entityManager.GetEntities(Entity.EntityType.Player))
            {
                ResourceAttribute health = ally.GetComponent<Entity>().health;
                session.output.Check(health != null && health.OnAllConsumerProcessed != null,
                    "Every rest observer has an initialized health resource");
                _health.Add(health);
            }

            foreach (ResourceAttribute health in _health)
            {
                health.OnAllConsumerProcessed.AddListener(OnConsumer);
            }
        }

        void OnConsumer(GameObject owner, ResourceModifier modifier, float value, bool critical)
        {
            if (value > 0f)
            {
                _session.manifest.restHealingEvents++;
            }
        }

        public void Dispose()
        {
            foreach (ResourceAttribute health in _health)
            {
                if (health != null)
                {
                    health.OnAllConsumerProcessed.RemoveListener(OnConsumer);
                }
            }

            _health.Clear();
        }
    }
}
