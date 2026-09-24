using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Stage;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    // What makes a derived unit behave like a stone, on the parts the CreatureBuilder beside it built:
    // it chips where it is hit, sheds a limb when hurt and collapses on death; the StoneThrow beside it throws
    public class StoneBody : MonoBehaviour, IEntityView
    {
        // Seed salts, one per kind of emission so two kinds never share a random stream
        static readonly uint shedSalt = 201;
        static readonly uint collapseSalt = 301;
        static readonly Transform[] noParts = new Transform[0];

        [SerializeField] StoneGroundDisc _groundShadow;
        [SerializeField] float _shedHealthFraction = 0.5f;
        // The thrown shard's colour, handed to the StoneThrow
        [SerializeField] LookPalette _palette;

        CreatureBuilder _builder;
        CreatureRig _rig;
        int _rigRevision;
        StoneThrow _throw;
        StoneEffects _effects;
        StageKeyLight _keyLight;
        Entity _entity;
        ResourceAttribute _health;
        uint _seed;
        Vector3 _planarVelocity;
        bool _isBound;

        readonly StoneHealthState _state = new StoneHealthState();
        readonly StoneMotionSampler _sampler = new StoneMotionSampler();
        readonly StoneImpacts _impacts = new StoneImpacts();

        bool _isCollapsed;
        public bool isCollapsed { get { return _isCollapsed; } }

        // The recipe part the stone lost when it was hurt, -1 while it is whole
        int _shedPart = -1;
        public int shedPart { get { return _shedPart; } }

        public IReadOnlyList<Transform> parts { get { return _rig != null ? _rig.partTransforms : noParts; } }

        public void Init(Entity entity, RenderManager manager)
        {
            _entity = entity;
            _keyLight = manager != null ? manager.keyLight : null;
            StoneEffects effects = manager != null ? manager.stoneEffects : null;
            ResourceAttribute health = entity != null ? entity.health : null;
            Init(health, (uint)transform.GetEntityId().GetHashCode(), effects);
        }

        // Taking the resource directly lets tests run without Entity.Init or the game managers
        public void Init(ResourceAttribute resource, uint visualSeed, StoneEffects effects)
        {
            Unsubscribe();
            _health = resource;
            _seed = visualSeed;
            _effects = effects;
            _builder = GetComponent<CreatureBuilder>();
            _rig = null;
            _state.Reset(_shedHealthFraction);
            _shedPart = -1;
            _isCollapsed = false;
            _sampler.Reset();
            _planarVelocity = Vector3.zero;
            _impacts.Init(transform, effects, visualSeed);
            _throw = GetComponent<StoneThrow>();
            if (_throw != null)
            {
                _throw.Init(_builder, effects, _palette, visualSeed);
            }
            FindRig();
            Subscribe();
        }

        // An in-place recompose changes the revision while keeping anchors and delivery leases alive.
        public void RefreshRig()
        {
            FindRig();
        }

        void FindRig()
        {
            CreatureRig rig = _builder != null ? _builder.rig : null;
            if (rig == _rig && (rig == null || rig.revision == _rigRevision))
            {
                return;
            }

            _rig = rig;
            _rigRevision = rig != null ? rig.revision : 0;
            IReadOnlyList<Transform> partTransforms = parts;
            _impacts.ReadParts(partTransforms);
            if (_isCollapsed)
            {
                HideParts();
                return;
            }

            // A body set up again on the same rig stands whole, a rebuilt rig keeps the limb it lost
            for (int i = 0; i < partTransforms.Count; i++)
            {
                partTransforms[i].gameObject.SetActive(i != _shedPart);
            }

            if (_groundShadow != null && partTransforms.Count > 0)
            {
                Bounds bounds = StoneGroundDisc.Measure(transform, partTransforms);
                _groundShadow.Init(bounds, StageKeyLight.KeyDirection, _keyLight);
                _groundShadow.Show(isActiveAndEnabled);
            }
        }

        public void RecordImpact(ResourceModifier modifier, StoneImpact impact)
        {
            if (modifier == null || !_isBound || !isActiveAndEnabled)
            {
                return;
            }

            _impacts.Record(modifier, impact);
        }

        public StoneImpact EstimateImpact(Vector3 query)
        {
            return _impacts.Estimate(query);
        }

        void OnConsumersProcessed(GameObject owner, ResourceModifier modifier, float delta, bool critical)
        {
            _state.RecordProcessedDelta(delta);
            _impacts.Resolve(modifier, delta < 0f && !_isCollapsed, critical);
        }

        void OnHealthChanged(ResourceAttribute resource)
        {
            // The callback comes after the clamp, and before the entity is destroyed
            if (resource.Value <= 0f)
            {
                Collapse(null);
            }
        }

        public void CompleteHealthBatch()
        {
            if (_health == null)
            {
                return;
            }

            StoneHealthAction action = _state.CompleteBatch(_health.Value, _health.Max);
            if (action == StoneHealthAction.Collapse)
            {
                Collapse(null);
            }
            else if (action == StoneHealthAction.ShedPart)
            {
                ShedPart();
            }
        }

        // One limb or accessory, picked by the seed, falls and splits
        void ShedPart()
        {
            if (_rig == null || _rig.recipe == null)
            {
                return;
            }

            IReadOnlyList<Transform> partTransforms = _rig.partTransforms;
            List<int> candidates = new List<int>();
            for (int i = 0; i < partTransforms.Count; i++)
            {
                PartRole role = _rig.recipe.parts[i].role;
                if ((role == PartRole.Limb || role == PartRole.Accessory) && partTransforms[i].gameObject.activeSelf)
                {
                    candidates.Add(i);
                }
            }

            if (candidates.Count == 0)
            {
                return;
            }

            _shedPart = candidates[(int)(_seed % (uint)candidates.Count)];
            Transform part = partTransforms[_shedPart];
            Mesh mesh = part.GetComponent<MeshFilter>().sharedMesh;
            Material material = part.GetComponent<Renderer>().sharedMaterial;
            StoneEmitters.DetachedPart(_effects, mesh, material, part.localToWorldMatrix, _planarVelocity,
                transform.position.y, SeededRandom.ForPart(_seed, shedSalt));
            part.gameObject.SetActive(false);
        }

        // Every part breaks into debris at once; effects handed by the death bridge replace missing ones
        public void Collapse(StoneEffects effects)
        {
            if (effects != null)
            {
                _effects = effects;
            }

            if (_isCollapsed)
            {
                return;
            }

            _isCollapsed = true;
            _state.TryBeginCollapse();
            if (_throw != null)
            {
                _throw.Enable(false);
            }
            uint seed = SeededRandom.ForPart(_seed, collapseSalt);
            StoneEmitters.Collapse(_effects, parts, _planarVelocity, transform.position.y, seed);
            HideParts();
        }

        void HideParts()
        {
            if (_groundShadow != null)
            {
                _groundShadow.Show(false);
            }

            foreach (Transform part in parts)
            {
                part.gameObject.SetActive(false);
            }
        }

        // After his Update: the health batch his consumers resolved this frame, and where the entity moved to
        void LateUpdate()
        {
            FindRig();
            if (!_isBound)
            {
                return;
            }

            CompleteHealthBatch();
            Vector3 position = _entity != null ? _entity.transform.position : transform.position;
            _planarVelocity = _sampler.Sample(position, Time.deltaTime, _entity != null && _entity.isDraggable);
            if (_groundShadow != null && !_isCollapsed)
            {
                _groundShadow.Refresh();
            }
            _impacts.CompleteFrame();
        }

        void Subscribe()
        {
            if (_isBound || _health == null || !isActiveAndEnabled)
            {
                return;
            }

            _health.OnAllConsumerProcessed.AddListener(OnConsumersProcessed);
            _health.OnValueChanged.AddListener(OnHealthChanged);
            _isBound = true;
        }

        void Unsubscribe()
        {
            if (_isBound && _health != null)
            {
                _health.OnAllConsumerProcessed.RemoveListener(OnConsumersProcessed);
                _health.OnValueChanged.RemoveListener(OnHealthChanged);
            }

            _isBound = false;
            _impacts.Clear();
            _sampler.Reset();
            _state.CompleteBatch(float.PositiveInfinity, 1f);
        }

        void OnEnable()
        {
            if (_groundShadow != null && _rig != null)
            {
                _groundShadow.Show(!_isCollapsed);
            }
            Subscribe();
        }

        void OnDisable()
        {
            Unsubscribe();
            if (_groundShadow != null)
            {
                _groundShadow.Show(false);
            }
        }

        void OnDestroy()
        {
            Unsubscribe();
        }
    }
}
