using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Creatures
{
    // Presentation at an authored anchor. Character.Init and Entity.Init are never called from here.
    [DefaultExecutionOrder(-200)]
    public class HLCharacterView : MonoBehaviour, IHLHealVisualSink, IHLDeliverySource
    {
        [FormerlySerializedAs("character")]
        [SerializeField] Character _character;
        [FormerlySerializedAs("recipe")]
        [SerializeField] HLCreatureRecipe _recipe;
        [FormerlySerializedAs("visualAnchor")]
        [SerializeField] Transform _visualAnchor;
        [FormerlySerializedAs("material")]
        [SerializeField] Material _material;
        [FormerlySerializedAs("cellSize")]
        [SerializeField] float _cellSize = 1f;

        HLRenderRegistry _injectedRegistry;
        HLRenderRegistry _registeredRegistry;
        bool _injected;
        GameObject _registeredSource;
        HLResourceOutcomeObserver _resourceObserver;
        HLStatusObserver _statusObserver;
        BuffManager _boundManager;
        bool _isListening;

        public HLCreatureRig rig { get; private set; }

        public IReadOnlyList<Transform> budAnchors { get { return rig?.budAnchors ?? Array.Empty<Transform>(); } }

        public Transform bud0 { get { return budAnchors.Count > 0 ? budAnchors[0] : null; } }

        public Transform bud1 { get { return budAnchors.Count > 1 ? budAnchors[1] : null; } }

        public Transform bud2 { get { return budAnchors.Count > 2 ? budAnchors[2] : null; } }

        public int castGestureCount { get; private set; }

        void OnEnable()
        {
            BuildAndRegister();
            ObserveResources();
        }

        void OnDisable()
        {
            StopObserving();
            Unregister();
            if (rig != null)
            {
                rig.CancelAll();
                rig.SetVisible(false);
            }
        }

        void OnDestroy()
        {
            StopObserving();
            Unregister();
            if (rig != null)
            {
                rig.Dispose();
            }

            rig = null;
        }

        void Update()
        {
            ObserveResources();
        }

        void LateUpdate()
        {
            BuildAndRegister();
            if (rig != null && _character)
            {
                rig.SetStatusTint(HLBodyTintState.Read(_character.gameObject));
                ResourceAttribute mana = _character.mana;
                float manaFraction = mana && mana.Max > 0 ? mana.Value / mana.Max : 0f;
                rig.SetReadout(null, 1f, 0f, manaFraction);
            }

            if (rig != null && _visualAnchor)
            {
                HLFootFrame frame = new HLFootFrame(_visualAnchor.position, _visualAnchor.up, _cellSize);
                rig.Tick(Time.time, Time.deltaTime, frame);
            }
        }

        public void Bind(Character owner, HLCreatureRecipe data, Transform anchor, Material sharedMaterial,
            HLRenderRegistry registry, float size = 1f)
        {
            StopObserving();
            Unregister();
            bool hasChanged = _recipe != data || _visualAnchor != anchor || _material != sharedMaterial
                || _cellSize != size;
            if (hasChanged)
            {
                if (rig != null)
                {
                    rig.Dispose();
                }

                rig = null;
            }

            _character = owner;
            _recipe = data;
            _visualAnchor = anchor;
            _material = sharedMaterial;
            _cellSize = size;
            _injectedRegistry = registry;
            _injected = true;
            BuildAndRegister();
            ObserveResources();
        }

        void ObserveResources()
        {
            if (!_character || !isActiveAndEnabled)
            {
                return;
            }

            if (!_resourceObserver)
            {
                _resourceObserver = _character.GetComponent<HLResourceOutcomeObserver>();
            }

            if (!_resourceObserver)
            {
                _resourceObserver = _character.gameObject.AddComponent<HLResourceOutcomeObserver>();
            }

            _resourceObserver.Bind(null, _character.mana, _injectedRegistry, _injected);
            if (!_isListening)
            {
                HLResourceOutcomeObserver.Outcome += OnOutcome;
                _isListening = true;
            }

            if (_character.buffManager && _boundManager != _character.buffManager)
            {
                if (!_statusObserver)
                {
                    _statusObserver = GetComponent<HLStatusObserver>();
                }

                if (!_statusObserver)
                {
                    _statusObserver = gameObject.AddComponent<HLStatusObserver>();
                }

                IHLSpellVisualSink sink = _injected && _injectedRegistry != null ? _injectedRegistry.spellSink : null;
                _statusObserver.Bind(_character.buffManager, sink);
                _boundManager = _character.buffManager;
            }
        }

        void OnOutcome(GameObject source, GameObject owner, HLResourceKind kind, float amount, bool critical)
        {
            // Positive health already arrives once through the heal registry, and mana is never a heal gesture
            if (_character && source == _character.gameObject && kind == HLResourceKind.Health && amount < 0f)
            {
                Cast(owner);
            }
        }

        void Cast(GameObject target)
        {
            if (!isActiveAndEnabled || !target || rig == null)
            {
                return;
            }

            castGestureCount++;
            rig.HealContact(HLCreatureBuilder.TargetPosition(target));
        }

        void StopObserving()
        {
            if (_isListening)
            {
                HLResourceOutcomeObserver.Outcome -= OnOutcome;
            }

            _isListening = false;
            if (_resourceObserver)
            {
                _resourceObserver.Bind(null, null, _injectedRegistry, _injected);
            }

            if (_statusObserver)
            {
                _statusObserver.Detach();
            }

            _boundManager = null;
        }

        void BuildAndRegister()
        {
            if (!_character || !_recipe || !_visualAnchor || !_material)
            {
                return;
            }

            if (rig == null)
            {
                rig = HLCreatureRig.Build(_recipe, _visualAnchor, _material, _cellSize);
            }

            rig.SetVisible(isActiveAndEnabled);
            if (!isActiveAndEnabled)
            {
                return;
            }

            HLRenderRegistry registry = _injected ? _injectedRegistry : HLRenderRegistry.current;
            if (_registeredRegistry != registry)
            {
                Unregister();
                _registeredRegistry = registry;
                _registeredSource = _character.gameObject;
                if (_registeredRegistry != null)
                {
                    _registeredRegistry.Register(_registeredSource, this);
                }
            }
        }

        void Unregister()
        {
            if (_registeredRegistry != null)
            {
                _registeredRegistry.Unregister(_registeredSource, this);
            }

            _registeredRegistry = null;
            _registeredSource = null;
        }

        #region IHLHealVisualSink

        public void OnHealResolved(GameObject target, float value, bool critical)
        {
            if (value <= 0f || !target || !HLChainSolver.Finite(value))
            {
                return;
            }

            Cast(target);
        }

        #endregion

        #region IHLDeliverySource

        public bool BeginDelivery(int token, HLDeliveryStyle style, Transform projectile, Vector3 end)
        {
            return isActiveAndEnabled && rig != null && rig.BeginDelivery(token, style, projectile, end);
        }

        public void UpdateDelivery(int token, Vector3 position)
        {
            if (rig != null)
            {
                rig.UpdateDelivery(token, position);
            }
        }

        public void ContactDelivery(int token, Vector3 position, GameObject target)
        {
            if (rig != null)
            {
                rig.ContactDelivery(token, position, target);
            }
        }

        public void EndDelivery(int token)
        {
            if (rig != null)
            {
                rig.EndDelivery(token);
            }
        }

        #endregion
    }
}
