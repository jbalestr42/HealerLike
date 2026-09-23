using UnityEngine;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Spells
{
    // HitArmor is an attribute and instant grants emit no buff start event, so the attribute itself is watched
    public class HLAttributeShieldView : MonoBehaviour, IVisualBehaviour, IEntityView
    {
        AttributeManager _attributes;
        Entity _entity;
        Transform _anchor;
        SpellLooks _looks;
        float _born;

        HLSpellEffect _effect;
        public HLSpellEffect effect { get { return _effect; } }

        public void Init(Entity entity, RenderManager manager)
        {
            if (entity == null || manager == null)
            {
                Debug.LogError("[HLAttributeShieldView] Init needs an entity and the RenderManager.");
                return;
            }

            _entity = entity;
            Bind(entity.attributeManager, Anchor(entity), manager.spellLooks);
        }

        // Old path while the stage prefabs still walk IVisualBehaviour, the looks then come from the registry sink
        public void Init(Entity entity)
        {
            _entity = entity;
            if (entity == null)
            {
                Bind(null, null, null);
                return;
            }

            HLSpellVisualSink sink = HLRenderRegistry.current != null
                ? HLRenderRegistry.current.spellSink as HLSpellVisualSink
                : null;
            Bind(entity.attributeManager, Anchor(entity), sink != null ? sink.looks : null);
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

        public void Bind(AttributeManager attributes, Transform anchor, SpellLooks looks)
        {
            if (_attributes != attributes || _anchor != anchor || _looks != looks)
            {
                Clear();
            }

            _attributes = attributes;
            _anchor = anchor;
            _looks = looks;
            Refresh();
        }

        public void Refresh()
        {
            if (_entity != null && _entity.targetPoint != null)
            {
                _anchor = _entity.targetPoint.transform;
            }

            float charges = 0f;
            if (_attributes != null && _attributes.Has(AttributeType.HitArmor))
            {
                charges = _attributes.Get(AttributeType.HitArmor).Value;
            }

            if (!isActiveAndEnabled || _anchor == null || !(charges > 0f) || float.IsInfinity(charges))
            {
                Clear();
                return;
            }

            if (_effect == null)
            {
                if (_looks == null || _looks.shield == null || _looks.shield.effectPrefab == null)
                {
                    return;
                }

                _effect = Instantiate(_looks.shield.effectPrefab, _anchor, false);
                _effect.transform.localPosition = _looks.shield.offset;
                _effect.Init();
                _effect.SetColor(_looks.shield.tint);
                _born = Time.time;
            }

            float elapsed = Mathf.Max(0f, Time.time - _born);
            _effect.SetStatus(1, elapsed, float.PositiveInfinity, HLClockKind.Simulation);
            _effect.SetShieldState(charges);
        }

        void Clear()
        {
            if (_effect == null)
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
                DestroyImmediate(_effect.gameObject);
            }
            _effect = null;
        }

        static Transform Anchor(Entity entity)
        {
            return entity.targetPoint != null ? entity.targetPoint.transform : entity.transform;
        }
    }
}
