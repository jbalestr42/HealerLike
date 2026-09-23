using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using HealerLike.Render.Spells;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Creatures
{
    // Presentation at an authored anchor. Character.Init and Entity.Init are never called from here.
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
        [SerializeField] HLPrimitiveMeshes _meshes;
        [FormerlySerializedAs("cellSize")]
        [SerializeField] float _cellSize = 1f;

        HLRenderRegistry _registry;
        HLRenderRegistry _registeredRegistry;
        IHLSpellVisualSink _sink;
        GameObject _registeredSource;
        HLResourceOutcomeObserver _resourceObserver;
        HLStatusObserver _statusObserver;
        BuffManager _boundManager;

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

        // The view prefab carries recipe, material and meshes, and anchors the body on itself
        public void Init(Character owner, RenderManager manager)
        {
            if (owner == null || manager == null)
            {
                Debug.LogError("[HLCharacterView] Init needs the character and the RenderManager.");
                return;
            }

            if (!_meshes)
            {
                _meshes = manager.meshes;
            }

            StopObserving();
            Unregister();
            _character = owner;
            _registry = manager.registry;
            _sink = manager.spellSink;
            if (!_visualAnchor)
            {
                _visualAnchor = transform;
            }

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
                _resourceObserver = HLResourceOutcomeObserver.Ensure(_character.gameObject);
            }

            _resourceObserver.Bind(null, _character.mana, _sink, _registry);
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

                _statusObserver.Bind(_character.buffManager, _sink);
                _boundManager = _character.buffManager;
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
            if (_resourceObserver)
            {
                _resourceObserver.Bind(null, null, _sink, _registry);
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
                HLCreatureRig created = new HLCreatureRig();
                if (!created.Init(_recipe, _visualAnchor, _material, _meshes, _cellSize))
                {
                    return;
                }

                rig = created;
            }

            rig.SetVisible(isActiveAndEnabled);
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (_registeredRegistry != _registry)
            {
                Unregister();
                _registeredRegistry = _registry;
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

        // The registry reports every health change the character caused, heals and damage both gesture
        public void OnHealResolved(GameObject target, float value, bool critical)
        {
            if (value == 0f || !target || !float.IsFinite(value))
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
