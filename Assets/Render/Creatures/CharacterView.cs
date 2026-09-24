using System;
using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Deliveries;
using HealerLike.Render.Spells;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Creatures
{
    // Presentation at an authored anchor. Character.Init and Entity.Init are never called from here.
    public class CharacterView : ARigHost
    {
        [SerializeField] Character _character;
        [SerializeField] CreatureRecipe _recipe;
        [SerializeField] Transform _visualAnchor;
        [SerializeField] Material _material;
        [SerializeField] Material _bodyMaterial;
        [SerializeField] PrimitiveMeshes _meshes;
        [SerializeField] float _cellSize = 1f;

        RenderRegistry _registry;
        ISpellVisualSink _sink;
        DeliveryVocabulary _deliveryVocabulary;
        StatusObserver _statusObserver;

        public IReadOnlyList<Transform> budAnchors
        {
            get
            {
                if (rig == null || rig.budAnchors == null)
                {
                    return Array.Empty<Transform>();
                }
                return rig.budAnchors;
            }
        }

        public Transform bud0 { get { return budAnchors.Count > 0 ? budAnchors[0] : null; } }

        public Transform bud1 { get { return budAnchors.Count > 1 ? budAnchors[1] : null; } }

        public Transform bud2 { get { return budAnchors.Count > 2 ? budAnchors[2] : null; } }

        void OnEnable()
        {
            BuildAndRegister();
            ObserveResources();
        }

        void OnDisable()
        {
            StopObserving();
            Unregister();
            HideRig();
        }

        void OnDestroy()
        {
            StopObserving();
            Unregister();
            ReleaseRig();
        }

        // The view prefab carries recipe, material and meshes, and anchors the body on itself
        public void Init(Character owner, RenderManager manager)
        {
            if (owner == null || manager == null)
            {
                Debug.LogError("[CharacterView] Init needs the character and the RenderManager.");
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
            _deliveryVocabulary = manager.deliveryVocabulary;
            if (!_visualAnchor)
            {
                _visualAnchor = transform;
            }

            BuildAndRegister();
            ObserveResources();
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
                ResourceAttribute mana = _character.mana;
                float manaFraction = mana && mana.Max > 0 ? mana.Value / mana.Max : 0f;
                rig.SetReadout(null, 1f, 0f, manaFraction);
            }

            if (_visualAnchor)
            {
                TickRig(Time.time, Time.deltaTime, new FootFrame(_visualAnchor.position, _visualAnchor.up, _cellSize));
            }
        }

        void ObserveResources()
        {
            if (!_character || !isActiveAndEnabled)
            {
                return;
            }

            // The view prefab carries the status observer, which wires the mana outcomes too
            if (!_statusObserver)
            {
                _statusObserver = GetComponent<StatusObserver>();
            }

            if (_statusObserver)
            {
                _statusObserver.Init(_character, _sink, _registry);
            }
        }

        void Cast(GameObject target)
        {
            if (!isActiveAndEnabled || !target || rig == null)
            {
                return;
            }

            HealContact(target);
        }

        void StopObserving()
        {
            if (_statusObserver)
            {
                _statusObserver.Init((Character)null, _sink, _registry);
            }
        }

        void BuildAndRegister()
        {
            if (!_character || !_recipe || !_visualAnchor || !_material)
            {
                return;
            }

            if (!BuildRig(_recipe, _visualAnchor, _material, _bodyMaterial, _meshes, _deliveryVocabulary, _cellSize))
            {
                return;
            }

            rig.SetVisible(isActiveAndEnabled);
            if (isActiveAndEnabled)
            {
                Register(_registry, _character.gameObject);
            }
        }

        #region IHealthVisualSink

        // The registry reports every health change the character caused, heals and damage both gesture
        public override void OnHealthResolved(GameObject target, float value, bool critical)
        {
            if (value == 0f || !target || !float.IsFinite(value))
            {
                return;
            }

            Cast(target);
        }

        #endregion

        #region IEffectAnchors

        // A character casts from its first bud
        public override bool TryGetAnchors(out EffectAnchors anchors)
        {
            if (!base.TryGetAnchors(out anchors))
            {
                return false;
            }

            if (bud0 != null)
            {
                anchors.castPoint = bud0.position;
            }
            return true;
        }

        #endregion
    }
}
