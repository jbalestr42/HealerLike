using UnityEngine;
namespace HealerLike.Render.Stones
{
    public sealed class HLStoneDeathBridge : MonoBehaviour
    {
        [SerializeField] EntityManager manager;
        [SerializeField] HLStoneEffects effects;
        bool bound;
        public void Bind(EntityManager owner,HLStoneEffects effectsOwner)
        { Unbind(); manager=owner; effects=effectsOwner; Subscribe(); }
        void Subscribe() { if(!bound && manager!=null && isActiveAndEnabled) { manager.OnEntityKilled.AddListener(OnEntityKilled); bound=true; } }
        void OnEntityKilled(Entity entity)
        {
            if(entity==null) return;
            HandleDeparture(entity.health,entity.GetComponentInChildren<HLStoneEnemyVisual>());
        }
        public void HandleDeparture(ResourceAttribute health,HLStoneEnemyVisual visual)
        {
            if(!isActiveAndEnabled || health==null || health.Value>0) return;
            visual?.Collapse(effects);
        }
        void Unbind() { if(bound && manager!=null) manager.OnEntityKilled.RemoveListener(OnEntityKilled); bound=false; }
        void OnEnable() => Subscribe();
        void OnDisable() => Unbind();
    }
}
