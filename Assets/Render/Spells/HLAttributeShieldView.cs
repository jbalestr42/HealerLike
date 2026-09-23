using UnityEngine;

namespace HealerLike.Render.Spells
{
    // HitArmor is an attribute and instant grants emit no buff-start event, so the attribute itself is watched.
    public class HLAttributeShieldView : MonoBehaviour, IVisualBehaviour
    {
        AttributeManager _attributes;
        Entity _entity;
        Transform _anchor;
        Material _material;
        float _born;

        HLSpellEffect _effect;
        public HLSpellEffect effect { get { return _effect; } }

        public void Init(Entity entity)
        {
            _entity = entity;
            if (!entity)
            {
                Bind(null, null);
                return;
            }
            Bind(entity.attributeManager, entity.targetPoint ? entity.targetPoint.transform : entity.transform);
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

        public void Bind(AttributeManager manager, Transform target, Material sharedMaterial = null)
        {
            if (_attributes != manager || _anchor != target)
            {
                Clear();
            }
            _attributes = manager;
            _anchor = target;
            _material = sharedMaterial;
            Refresh();
        }

        public void Refresh()
        {
            if (_entity && _entity.targetPoint)
            {
                _anchor = _entity.targetPoint.transform;
            }
            float charges = 0f;
            if (_attributes && _attributes.Has(AttributeType.HitArmor))
            {
                charges = _attributes.Get(AttributeType.HitArmor).Value;
            }
            if (!isActiveAndEnabled || !_anchor || !(charges > 0f) || float.IsInfinity(charges))
            {
                Clear();
                return;
            }
            if (!_effect)
            {
                GameObject go = new GameObject("HLObservedHitArmor");
                go.transform.SetParent(_anchor, false);
                _effect = go.AddComponent<HLSpellEffect>();
                _effect.kind = HLSpellEffectKind.Shield;
                _effect.material = _material
                    ? _material
                    : (HLRenderRegistry.Current?.SpellSink as HLSpellVisualSink)?.material;
                _effect.Initialize();
                _born = Time.time;
                HLSpellSignature signature = new HLSpellSignature
                {
                    operation = HLOperation.Attribute,
                    sign = HLSign.Positive,
                    hasAttribute = true,
                    attribute = AttributeType.HitArmor,
                    topology = HLTopology.Single,
                    duration = HLDurationShape.Infinite,
                    tempo = HLTempo.Continuous
                };
                _effect.SetStatus(1, 0f, float.PositiveInfinity, HLClockKind.Simulation, signature);
            }
            float elapsed = Mathf.Max(0f, Time.time - _born);
            _effect.SetStatus(1, elapsed, float.PositiveInfinity, HLClockKind.Simulation, _effect.signature);
            _effect.SetShieldState(charges);
        }

        void Clear()
        {
            if (!_effect)
            {
                return;
            }
            _effect.gameObject.SetActive(false);
            if (Application.isPlaying)
            {
                Destroy(_effect.gameObject);
            }
            else
            {
                _effect.ReleaseResources();
                DestroyImmediate(_effect.gameObject);
            }
            _effect = null;
        }
    }
}
