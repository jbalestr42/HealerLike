using UnityEngine;

namespace HealerLike.Render.Spells
{
    // The thread between the successive targets a chain projectile hits
    public class ContactThread
    {
        SpellVisualSink _sink;
        Vector3 _previous;
        bool _hasPrevious = false;

        public void Init(SpellVisualSink sink)
        {
            _sink = sink;
            _hasPrevious = false;
        }

        public void Contact(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            Vector3 contact = RenderTargets.Point(target);
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

        public void Clear()
        {
            _hasPrevious = false;
        }
    }
}
