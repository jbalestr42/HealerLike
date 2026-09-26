using System.Collections.Generic;
using System;
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

        [SerializeField] bool _showBody = true;

        [SerializeField, Range(-0.3f, -0.01f)]
        float _castViewportY = -0.08f;

        [SerializeField, Min(0f)]
        float _castHeight = 1.5f;
        Camera _camera;
        public bool showBody
        {
            get { return _showBody; }
        }

        public bool castsFromScreen
        {
            get { return !_showBody && _camera; }
        }

        public static CharacterView ScreenSource(GameObject source)
        {
            CharacterView view = source ? source.GetComponentInChildren<CharacterView>() : null;
            return view && view.castsFromScreen ? view : null;
        }

        // Intersect the bottom-centre ray with the cast plane, so yaw and focus changes need no cached offset.
        public bool TryGetCastPoint(out Vector3 point)
        {
            point = transform.position;
            if (!castsFromScreen)
            {
                return false;
            }

            float height = _character ? _character.transform.position.y : transform.position.y;
            Plane plane = new Plane(Vector3.up, Vector3.up * (height + _castHeight));
            Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, _castViewportY, 0f));
            if (!plane.Raycast(ray, out float distance))
            {
                return false;
            }

            point = ray.GetPoint(distance);
            return true;
        }

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

        public Transform bud0
        {
            get { return budAnchors.Count > 0 ? budAnchors[0] : null; }
        }

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
            _camera = manager.gameCamera;
            _sink = manager.spellSink;
            _deliveryVocabulary = manager.deliveryVocabulary;
            if (!_visualAnchor)
            {
                _visualAnchor = transform;
            }

            BuildAndRegister();
            ObserveResources();
        }

        // After the character's own Update has spent or restored its mana
        void LateUpdate()
        {
            SyncGeometry();
            if (!_visualAnchor)
            {
                ReleaseRig();
                return;
            }
            if (!_showBody)
            {
                return;
            }

            if (rig != null && _character)
            {
                ResourceAttribute mana = _character.mana;
                float manaFraction = 0f;
                if (mana && mana.Max > 0)
                {
                    manaFraction = mana.Value / mana.Max;
                }

                rig.SetReadout(null, 1f, 0f, manaFraction);
            }

            TickRig(Time.time, Time.deltaTime, new FootFrame(_visualAnchor.position, _visualAnchor.up, _cellSize));
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
            if (!_showBody && _character)
            {
                ReleaseRig();
                if (isActiveAndEnabled)
                {
                    Register(_registry, _character.gameObject);
                }

                return;
            }

            if (!_character || !_recipe || !_visualAnchor || !_material)
            {
                return;
            }

            if (!BuildRig(_recipe, _visualAnchor, _material, _bodyMaterial, _meshes, _deliveryVocabulary, _cellSize))
            {
                return;
            }

            SetRigVisible(isActiveAndEnabled);
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
            if (TryGetCastPoint(out Vector3 point))
            {
                anchors = new EffectAnchors
                {
                    foot = point,
                    bodyCentre = point,
                    neck = point,
                    headCentre = point,
                    bodyRadius = 0.3f,
                    headRadius = 0.15f,
                    castPoint = point,
                    castSources = new[] { point },
                };
                return true;
            }

            if (!base.TryGetAnchors(out anchors))
            {
                return false;
            }

            if (bud0 != null && rig != null && !CreatureSources.HasExplicit(rig))
            {
                anchors.castPoint = bud0.position;
            }

            return true;
        }
        #endregion
    }
}
