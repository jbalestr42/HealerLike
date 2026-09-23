using UnityEngine;
using UnityEngine.Serialization;

namespace HealerLike.Render.Spells
{
    public class HLChainContactVisual : AProjectileBehaviour
    {
        [FormerlySerializedAs("Sink")]
        public HLSpellVisualSink sink;

        Projectile _subscribed;
        Vector3 _previous;
        bool _hasPrevious;

        public override void Init(GameObject source)
        {
            Bind(
                projectile ? projectile : GetComponent<Projectile>(),
                sink ? sink : HLRenderRegistry.current?.spellSink as HLSpellVisualSink
            );
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
            this.sink = sink;
            _subscribed = observed;
            if (_subscribed)
            {
                _subscribed.OnHit.AddListener(OnContact);
            }
        }

        void OnContact(OnHitData hit)
        {
            if (!isActiveAndEnabled || hit == null || !hit.target)
            {
                return;
            }
            Entity entity = hit.target.GetComponent<Entity>();
            Vector3 contact = hit.target.transform.position;
            if (entity && entity.targetPoint)
            {
                contact = entity.targetPoint.transform.position;
            }
            if (
                !HLSpellGrammar.Finite(contact.x)
                || !HLSpellGrammar.Finite(contact.y)
                || !HLSpellGrammar.Finite(contact.z)
            )
            {
                return;
            }
            if (_hasPrevious && sink)
            {
                sink.ShowContactLink(_previous, contact);
            }
            _previous = contact;
            _hasPrevious = true;
        }

        void Unbind()
        {
            if (_subscribed)
            {
                _subscribed.OnHit.RemoveListener(OnContact);
            }
            _subscribed = null;
            _hasPrevious = false;
        }
    }
}
