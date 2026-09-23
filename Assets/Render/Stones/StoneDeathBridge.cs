using UnityEngine;
using UnityEngine.Serialization;

namespace HealerLike.Render.Stones
{
    public class StoneDeathBridge : MonoBehaviour
    {
        [FormerlySerializedAs("manager")]
        [SerializeField] EntityManager _manager;
        [FormerlySerializedAs("effects")]
        [SerializeField] StoneEffects _effects;
        bool _isBound;

        public void Bind(EntityManager owner, StoneEffects effectsOwner)
        {
            Unbind();
            _manager = owner;
            _effects = effectsOwner;
            Subscribe();
        }

        void Subscribe()
        {
            if (!_isBound && _manager != null && isActiveAndEnabled)
            {
                _manager.OnEntityKilled.AddListener(OnEntityKilled);
                _isBound = true;
            }
        }

        void OnEntityKilled(Entity entity)
        {
            if (entity == null)
            {
                return;
            }

            HandleDeparture(entity.health, entity.GetComponentInChildren<StoneEnemyVisual>());
        }

        public void HandleDeparture(ResourceAttribute health, StoneEnemyVisual visual)
        {
            if (!isActiveAndEnabled || health == null || health.Value > 0f || visual == null)
            {
                return;
            }

            visual.Collapse(_effects);
        }

        void Unbind()
        {
            if (_isBound && _manager != null)
            {
                _manager.OnEntityKilled.RemoveListener(OnEntityKilled);
            }
            _isBound = false;
        }

        void OnEnable()
        {
            Subscribe();
        }

        void OnDisable()
        {
            Unbind();
        }
    }
}
