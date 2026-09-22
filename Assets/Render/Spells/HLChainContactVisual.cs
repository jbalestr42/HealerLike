using UnityEngine;

namespace HealerLike.Render.Spells
{
    /// <summary>Gold threads between ordered, confirmed lightning contacts. Attach before Projectile.Init.</summary>
    [DisallowMultipleComponent]
    public sealed class HLChainContactVisual : AProjectileBehaviour
    {
        public HLSpellVisualSink Sink;
        Projectile _subscribed;
        Vector3 _previous;
        bool _hasPrevious;

        public override void Init(GameObject source)
        {
            Bind(projectile ? projectile : GetComponent<Projectile>(),
                Sink ? Sink : HLRenderRegistry.Current?.SpellSink as HLSpellVisualSink);
        }
        public void Bind(Projectile observed, HLSpellVisualSink sink)
        {
            Unbind(); Sink = sink; _subscribed = observed;
            if (_subscribed) _subscribed.OnHit.AddListener(OnContact);
        }
        void OnContact(OnHitData hit)
        {
            if (!isActiveAndEnabled || hit == null || !hit.target) return;
            var entity = hit.target.GetComponent<Entity>();
            Vector3 contact = entity && entity.targetPoint ? entity.targetPoint.transform.position : hit.target.transform.position;
            if (!HLSpellGrammar.Finite(contact.x) || !HLSpellGrammar.Finite(contact.y) || !HLSpellGrammar.Finite(contact.z)) return;
            if (_hasPrevious && Sink) Sink.ShowContactLink(_previous,contact);
            _previous = contact; _hasPrevious = true;
        }
        void Unbind()
        {
            if (_subscribed) _subscribed.OnHit.RemoveListener(OnContact);
            _subscribed = null; _hasPrevious = false;
        }
        void OnDisable() => Unbind();
        void OnDestroy() => Unbind();
    }
}
