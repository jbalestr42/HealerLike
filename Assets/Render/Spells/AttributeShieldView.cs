using UnityEngine;

namespace HealerLike.Render.Spells
{
    // HitArmor is an attribute and instant grants emit no buff start event, so the attribute itself is watched
    // and its charges go to the sink, which draws them as the plates of the target. StatusObserver wires it.
    public class AttributeShieldView : MonoBehaviour
    {
        AttributeManager _attributes;
        GameObject _target;
        SpellVisualSink _sink;

        public SpellEffect effect
        {
            get
            {
                if (_sink == null || _target == null)
                {
                    return null;
                }
                return _sink.GetElement(_target, EffectElement.Plates);
            }
        }

        void LateUpdate()
        {
            Refresh();
        }

        void OnDisable()
        {
            Clear();
        }

        void OnDestroy()
        {
            Clear();
        }

        public void Init(AttributeManager attributes, GameObject target, SpellVisualSink sink)
        {
            if (_attributes != attributes || _target != target || _sink != sink)
            {
                Clear();
            }

            _attributes = attributes;
            _target = target;
            _sink = sink;
            Refresh();
        }

        public void Refresh()
        {
            if (_sink == null || _target == null)
            {
                return;
            }

            float charges = 0f;
            if (isActiveAndEnabled && _attributes != null && _attributes.Has(AttributeType.HitArmor))
            {
                charges = _attributes.Get(AttributeType.HitArmor).Value;
            }

            if (float.IsInfinity(charges))
            {
                charges = 0f;
            }
            _sink.SetCharges(_target, charges);
        }

        void Clear()
        {
            if (_sink != null && _target != null)
            {
                _sink.SetCharges(_target, 0f);
            }
        }
    }
}
