using UnityEngine;
using UnityEngine.Serialization;

namespace HealerLike.Render.Stones
{
    public class HLStoneDeathBridge : MonoBehaviour
    {
        [FormerlySerializedAs("manager")]
        [SerializeField] EntityManager _manager;
        [FormerlySerializedAs("effects")]
        [SerializeField] HLStoneEffects _effects;
        bool _isBound;

        public void Bind(EntityManager owner, HLStoneEffects effectsOwner)
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

            HandleDeparture(entity.health, entity.GetComponentInChildren<HLStoneEnemyVisual>());
        }

        public void HandleDeparture(ResourceAttribute health, HLStoneEnemyVisual visual)
        {
            if (!isActiveAndEnabled || health == null || health.Value > 0f)
            {
                return;
            }

            visual?.Collapse(_effects);
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
