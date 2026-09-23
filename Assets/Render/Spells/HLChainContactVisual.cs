using UnityEngine;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Spells
{
    // Draws a thread between the successive targets a chain projectile hits
    public class HLChainContactVisual : AProjectileBehaviour
    {
        HLSpellVisualSink _sink;
        Projectile _subscribed;
        Vector3 _previous;
        bool _hasPrevious = false;

        public void Init(RenderManager manager)
        {
            if (manager == null)
            {
                Debug.LogError("[HLChainContactVisual] Init needs the RenderManager.");
                return;
            }

            _sink = manager.spellSink;
        }

        // Called by the projectile, the sink comes from the manager or, on the old stage path, from the registry
        public override void Init(GameObject source)
        {
            HLSpellVisualSink sink = _sink;
            if (sink == null && HLRenderRegistry.current != null)
            {
                sink = HLRenderRegistry.current.spellSink as HLSpellVisualSink;
            }
            Bind(projectile != null ? projectile : GetComponent<Projectile>(), sink);
        }

        void OnDisable()
        {
            Unbind();
        }

        void OnDestroy()
        {
            Unbind();
        }

        public void Bind(Projectile observed, HLSpellVisualSink sink)
        {
            Unbind();
            _sink = sink;
            _subscribed = observed;
            if (_subscribed != null)
            {
                _subscribed.OnHit.AddListener(OnHit);
            }
        }

        void OnHit(OnHitData hit)
        {
            if (!isActiveAndEnabled || hit == null || hit.target == null)
            {
                return;
            }

            Entity entity = hit.target.GetComponent<Entity>();
            Vector3 contact = hit.target.transform.position;
            if (entity != null && entity.targetPoint != null)
            {
                contact = entity.targetPoint.transform.position;
            }

            if (!float.IsFinite(contact.x) || !float.IsFinite(contact.y) || !float.IsFinite(contact.z))
            {
                return;
            }

            if (_hasPrevious && _sink != null)
            {
                _sink.ShowContactLink(_previous, contact);
            }
            _previous = contact;
            _hasPrevious = true;
        }

        void Unbind()
        {
            if (_subscribed != null)
            {
                _subscribed.OnHit.RemoveListener(OnHit);
            }
            _subscribed = null;
            _hasPrevious = false;
        }
    }
}
