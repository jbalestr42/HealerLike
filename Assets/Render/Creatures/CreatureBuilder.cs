using UnityEngine;
using HealerLike.Render.Deliveries;
using HealerLike.Render.Spells;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Creatures
{
    // The view of a spawned entity: derives or takes its recipe, reads the entity's skills, target and health
    public class CreatureBuilder : ARigHost, IEntityView
    {
        [SerializeField] CreatureRecipe _recipe;
        [SerializeField] Material _material;
        [SerializeField] Material _bodyMaterial;
        [SerializeField] PrimitiveMeshes _meshes;

        readonly UnitReadout _readout = new UnitReadout();
        // The recipe this view derived from its entity, released with it
        CreatureRecipe _derivedRecipe;
        Entity _entity;
        ResourceAttribute _health;
        StatusObserver _statusObserver;
        RenderRegistry _registry;
        DeliveryVocabulary _deliveryVocabulary;
        ISpellVisualSink _spellSink;
        RenderManager _manager;

        public CreatureRecipe recipe { get { return _recipe; } }

        void OnEnable()
        {
            if (_entity)
            {
                EnsureRig();
                Attach();
            }
        }

        void OnDisable()
        {
            Detach();
            HideRig();
        }

        void OnDestroy()
        {
            Detach();
            ReleaseRig();
            RenderObjects.Release(_derivedRecipe);
            _derivedRecipe = null;
        }

        public void Init(Entity owner, RenderManager manager)
        {
            _manager = manager;
            if (!_meshes && manager)
            {
                _meshes = manager.meshes;
            }

            _registry = null;
            _spellSink = null;
            if (manager)
            {
                _registry = manager.registry;
                _spellSink = manager.spellSink;
                _deliveryVocabulary = manager.deliveryVocabulary;
            }

            // A view without an authored recipe draws the one derived from the entity's data
            if (!_recipe && owner && manager && manager.creatureLooks)
            {
                _recipe = manager.creatureLooks.GetRecipe(owner.data, owner.entityType);
                _derivedRecipe = _recipe;
            }

            Init(owner);
        }

        // Recompose only the view. The owner, health subscriptions and delivery leases stay live.
        public bool Rebuild(RenderManager manager)
        {
            if (manager != _manager || !_entity || rig == null)
            {
                return false;
            }

            CreatureRecipe next = _recipe;
            bool isDerived = _derivedRecipe != null;
            if (isDerived)
            {
                next = manager.creatureLooks.GetRecipe(_entity.data, _entity.entityType);
            }
            if (next == null || !rig.Recompose(next, _material, _bodyMaterial, _meshes))
            {
                if (isDerived)
                {
                    RenderObjects.Release(next);
                }
                return false;
            }

            if (isDerived)
            {
                RenderObjects.Release(_derivedRecipe);
                _derivedRecipe = next;
            }
            _recipe = next;
            RefreshArms();
            _readout.Read();
            rig.SetReadout(_readout.target, _readout.healthFraction, _readout.readiness, _readout.readiness);
            TickPresentation(0f);
            HealerLike.Render.Stones.StoneBody stone = GetComponent<HealerLike.Render.Stones.StoneBody>();
            if (stone != null)
            {
                stone.RefreshRig();
            }
            return true;
        }

        // Taking the entity alone lets the studio and tests run without the manager
        public void Init(Entity owner)
        {
            Detach();
            if (_entity != owner)
            {
                CancelGestures();
            }

            _entity = owner;
            _readout.Init(_entity);
            ObserveOutcomes();
            if (!_entity)
            {
                if (rig != null)
                {
                    rig.SetVisible(false);
                }

                return;
            }

            EnsureRig();
            if (isActiveAndEnabled)
            {
                Attach();
            }
        }

        // After his Update has moved the entity and its skills; the readout is the one thing polled every frame
        void LateUpdate()
        {
            if (!_entity || rig == null)
            {
                return;
            }

            _readout.Read();
            rig.SetReadout(_readout.target, _readout.healthFraction, _readout.readiness, _readout.readiness);
            TickPresentation(Time.deltaTime);
        }

        // The view prefab carries the status observer, which wires the outcome observers too
        void ObserveOutcomes()
        {
            if (_statusObserver == null)
            {
                _statusObserver = GetComponent<StatusObserver>();
            }

            if (_statusObserver != null)
            {
                _statusObserver.Init(_entity, _spellSink, _registry);
            }
        }

        void EnsureRig()
        {
            if (rig == null && _recipe && _material)
            {
                BuildRig(_recipe, transform, _material, _bodyMaterial, _meshes, _deliveryVocabulary,
                    StageCalibration.CellSize);
            }

            if (rig != null)
            {
                rig.SetVisible(isActiveAndEnabled);
                TickPresentation(0f);
            }
        }

        void TickPresentation(float deltaTime)
        {
            Camera camera = _manager != null ? _manager.gameCamera : null;
            rig.SetPresentationForward(camera != null ? -camera.transform.forward : (Vector3?)null);
            TickRig(Time.time, deltaTime, Frame());
        }

        FootFrame Frame()
        {
            return new FootFrame(transform.position, transform.up, StageCalibration.CellSize);
        }

        void Attach()
        {
            if (!_entity || !isActiveAndEnabled)
            {
                return;
            }

            if (_health != _entity.health)
            {
                if (_health)
                {
                    _health.OnAllConsumerProcessed.RemoveListener(OnHealthProcessed);
                }

                _health = _entity.health;
                if (_health)
                {
                    _health.OnAllConsumerProcessed.AddListener(OnHealthProcessed);
                }
            }

            Register(_registry, _entity.gameObject);
            if (rig != null)
            {
                rig.SetVisible(true);
            }
        }

        void Detach()
        {
            if (_health)
            {
                _health.OnAllConsumerProcessed.RemoveListener(OnHealthProcessed);
            }

            _health = null;
            Unregister();
        }

        void OnHealthProcessed(GameObject owner, ResourceModifier modifier, float value, bool critical)
        {
            if (!isActiveAndEnabled || value == 0f || !float.IsFinite(value))
            {
                return;
            }

            if (value < 0f && rig != null)
            {
                rig.Hit();
            }
        }

        #region IHealthVisualSink

        // Damage reaches this sink too, only a heal draws the contact
        public override void OnHealthResolved(GameObject target, float value, bool critical)
        {
            if (isActiveAndEnabled && target && value > 0f)
            {
                HealContact(target);
            }
        }

        #endregion
    }
}
