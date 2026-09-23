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

        // Called by the projectile after the manager handed the sink
        public override void Init(GameObject source)
        {
            Bind(projectile != null ? projectile : GetComponent<Projectile>(), _sink);
        }

        void OnDisable()
        {
            Unbind();
        }

        void OnDestroy()
        {
            Unbind();
        }

        public void Bind(Projectile observed, SpellVisualSink sink)
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
