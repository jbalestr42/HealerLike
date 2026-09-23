using System;
using UnityEngine;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Stones
{
    public class HLStoneLife : MonoBehaviour
    {
        HLStoneLifeState _state = new HLStoneLifeState();
        HLStoneEffects _effects;
        Transform _top;
        Quaternion _rest;
        float _wobbleAge = 2f;
        float _radius;
        uint _seed;
        uint _index;
        bool _isTerrain;
        bool _isSubscribed;

        public void Configure(HLStoneEffects owner, uint visualSeed, float footprint, bool isTerrain,
            Transform cairnTop = null)
        {
            Restore();
            _state = new HLStoneLifeState();
            _effects = owner;
            _seed = visualSeed;
            _index = 0;
            _radius = footprint;
            _isTerrain = isTerrain;
            _top = cairnTop;
            _rest = _top != null ? _top.localRotation : Quaternion.identity;
            _wobbleAge = 2f;
            Subscribe();
        }

        void Subscribe()
        {
            if (_isSubscribed)
            {
                return;
            }

            HLStoneEffects.ImpactRecorded += OnImpact;
            _isSubscribed = true;
        }

        void OnEnable()
        {
            Subscribe();
        }

        void OnDisable()
        {
            if (_isSubscribed)
            {
                HLStoneEffects.ImpactRecorded -= OnImpact;
            }
            _isSubscribed = false;
            Restore();
            _wobbleAge = 2f;
        }

        void OnDestroy()
        {
            OnDisable();
        }

        void Restore()
        {
            if (_top != null)
            {
                _top.localRotation = _rest;
            }
        }

        void OnImpact(Vector3 position)
        {
            if (_top != null && (position - transform.position).sqrMagnitude <= (_radius + 2f) * (_radius + 2f))
            {
                _wobbleAge = 0f;
            }
        }

        HLStoneEffects Effects()
        {
            if (_effects == null && Application.isPlaying)
            {
                _effects = HLStoneEffects.ForScene(gameObject.scene, null);
            }
            return _effects;
        }

        public void PollHealth(float fraction, float dt, Vector3 origin)
        {
            if (isActiveAndEnabled && _state.PollHealth(fraction, dt))
            {
                Effects()?.EmitTrickle(origin, _seed + ++_index);
            }
        }

        public void Advance(float dt)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (float.IsFinite(dt))
            {
                _wobbleAge += Mathf.Max(0f, dt);
            }
            if (_top != null && _top.gameObject.activeSelf)
            {
                float wobble = HLStoneLifeState.Wobble(_wobbleAge);
                _top.localRotation = _rest * Quaternion.Euler(wobble, 0f, wobble * 0.4f);
            }
            if (!_isTerrain)
            {
                return;
            }

            HLZoneRegistry registry = HLZoneRegistry.current;
            ReadOnlySpan<HLZone> snapshot = registry != null ? registry.snapshot : default;
            int pulses = _state.PollZones(snapshot, transform.position, _radius);
            for (int i = 0; i < pulses; i++)
            {
                Effects()?.EmitDust(transform.position + Vector3.up * 0.1f, _seed + ++_index);
            }
        }

        void LateUpdate()
        {
            Advance(Time.deltaTime);
        }
    }
}
