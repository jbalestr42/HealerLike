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

        public void Init(EntityManager owner, StoneEffects effectsOwner)
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
            HandleDeparture(entity);
        }

        // Only a lethal departure collapses, and only a stone body under the entity
        public void HandleDeparture(Entity entity)
        {
            if (!isActiveAndEnabled || entity == null || entity.health == null || entity.health.Value > 0f)
            {
                return;
            }

            StoneBody body = entity.GetComponentInChildren<StoneBody>();
            if (body == null)
            {
                return;
            }

            body.Collapse(_effects);
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
