using UnityEngine;
using UnityEngine.Serialization;

namespace HealerLike.Render.Spells
{
    public class HLChainContactVisual : AProjectileBehaviour
    {
        [FormerlySerializedAs("sink")]
        public HLSpellVisualSink sink;
        Projectile _subscribed;
        Vector3 _previous;
        bool _hasPrevious;

        public override void Init(GameObject source)
        {
            Bind(
                projectile ? projectile : GetComponent<Projectile>(),
                sink ? sink : HLRenderRegistry.Current?.SpellSink as HLSpellVisualSink
            );
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
            Vector3 contact =
                entity && entity.targetPoint ? entity.targetPoint.transform.position : hit.target.transform.position;
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

        void OnDisable()
        {
            Unbind();
        }

        void OnDestroy()
        {
            Unbind();
        }
    }
}
