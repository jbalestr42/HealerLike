using UnityEngine;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Spells
{
    // Draws a thread between the successive targets a chain projectile hits
    public class ChainContactVisual : AProjectileBehaviour
    {
        SpellVisualSink _sink;
        Projectile _subscribed;
        Vector3 _previous;
        bool _hasPrevious = false;

        public void Init(RenderManager manager)
        {
            if (manager == null)
            {
                Debug.LogError("[ChainContactVisual] Init needs the RenderManager.");
                return;
            }

            _sink = manager.spellSink;
        }

        // Projectile.Init sets the projectile and calls it, after the manager handed the sink
        public override void Init(GameObject source)
        {
            Observe(projectile, _sink);
        }

        void OnDisable()
        {
            Unbind();
        }

        void OnDestroy()
        {
            Unbind();
        }

        void Observe(Projectile observed, SpellVisualSink sink)
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

            Vector3 contact = RenderTargets.Point(hit.target);
            if (!RenderMath.IsFinite(contact))
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
