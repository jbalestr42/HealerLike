using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Spells
{
    // One element built from its recipe, no prefab: the parts come from the vocabulary and move by the recipe's motion
    public partial class SpellEffect : MonoBehaviour
    {
        // An element alive longer than this keeps clear of the head
        public static readonly float LastingSeconds = 0.6f;

        static readonly float removalSeconds = 0.25f;
        // Shortest cycle a phase divides by, and the thinnest part a stalk is scaled against
        static readonly float minCycle = 0.01f;
        static readonly float minWidth = 0.0001f;

        readonly EffectParts _parts = new EffectParts();
        EffectRecipe _recipe;
        float _age;
        float _sinceStatus;
        bool _isStatus = false;
        bool _isRemoving = false;
        float _removalAge;
        float _fallDistance = 1f;
        Vector3 _linkStart;
        public Vector3 castOrigin => _linkStart;
        Vector3 _linkEnd;

        int _stacks;
        public int stacks { get { return _stacks; } }

        float _elapsedSeconds;
        public float elapsedSeconds { get { return _elapsedSeconds; } }

        float _durationSeconds;
        public float durationSeconds { get { return _durationSeconds; } }

        int _count;
        public int count { get { return _count; } }

        bool _isContactThread = false;
        public bool isContactThread { get { return _isContactThread; } }

        public EffectRecipe recipe { get { return _recipe; } }
        public EffectKey element { get { return _recipe != null ? _recipe.element : EffectKey.Burst; } }
        public float lifetime => CompositeLifetime();
        public List<Transform> shapes { get { return _parts.shapes; } }
        public List<Transform> stalks { get { return _parts.stalks; } }
        public List<Transform> parts { get { return _parts.all; } }
        public List<Transform> rings { get { return _parts.rings; } }
        public bool removalComplete => _isRemoving && _removalAge >= RemovalSeconds() && LayersRemoved();

        // Ticking or held statuses last, and so does a single run longer than the lasting limit
        public bool isLasting
        {
            get
            {
                return _recipe != null && (_recipe.tempo != EffectTempo.Once || _recipe.cycleSeconds > LastingSeconds);
            }
        }

        void Update()
        {
            Advance(Time.deltaTime);
            if (removalComplete || (!_isStatus && _age >= lifetime))
            {
                Dispose(gameObject);
            }
        }

        public void Init(EffectRecipe recipe, PrimitiveMeshes meshes, Material material, LookSide targetSide)
        {
            if (recipe == null || meshes == null)
            {
                Debug.LogError("[SpellEffect] Init needs a recipe and the primitive meshes.");
                return;
            }

            if (_recipe != null)
            {
                Debug.LogError("[SpellEffect] Init can only be called once per effect.");
                return;
            }
            if (!EffectValidator.TryValidate(recipe, out string error))
            {
                Debug.LogError("[SpellEffect] " + error);
                return;
            }

            _recipe = recipe.ResolveCycle();
            _parts.Build(_recipe, transform, meshes, material, targetSide);
            BuildLayers(meshes, material, targetSide);
            SetCount(recipe.count);
            Advance(0f);
        }

        public void SetCount(int shown)
        {
            _count = _parts.ShowShapes(shown);
        }

        public void SetStatus(int stacks, float elapsed, float duration)
        {
            PruneLayers();
            int visibleStacks = Mathf.Max(0, stacks);
            if (!_isStatus || _stacks != visibleStacks)
            {
                _parts.ShowStacks(visibleStacks);
            }

            float safeElapsed = float.IsFinite(elapsed) ? Mathf.Max(0f, elapsed) : 0f;
            if (!_isStatus || safeElapsed != _elapsedSeconds)
            {
                _sinceStatus = 0f;
            }

            _isStatus = true;
            _stacks = visibleStacks;
            _elapsedSeconds = safeElapsed;
            _durationSeconds = duration;
            foreach (SpellEffect layer in _layers)
            {
                layer.SetStatus(stacks, elapsed, duration);
            }

            Advance(0f);
        }

        // How far a drop falls, in the element's own units
        public void SetFallDistance(float distance)
        {
            if (float.IsFinite(distance) && distance > 0f)
            {
                _fallDistance = distance;
            }
        }

        public void SetSide(Entity.EntityType side)
        {
            PruneLayers();
            _parts.ShowSide(side);
            foreach (SpellEffect layer in _layers)
            {
                layer.SetSide(side);
            }
        }

        public void ShowCritical()
        {
            PruneLayers();
            _parts.ShowCritical();
            foreach (SpellEffect layer in _layers)
            {
                layer.ShowCritical();
            }
        }

        public void BeginRemoval()
        {
            PruneLayers();
            ReleaseGround();
            _isRemoving = true;
            _removalAge = 0f;
            foreach (SpellEffect layer in _layers)
            {
                layer.BeginRemoval();
            }
        }

        CastSourceLease _castSource;
        void OnDestroy() => ReleaseResourcesTree();

        public bool IsCastFrom(CreatureRig rig) => _castSource != null && _castSource.IsFrom(rig);

        public void SetCastSource(GameObject source)
        {
            PruneLayers();
            _castSource?.Dispose();
            _castSource = source ? CastSourceLease.From(source) : null;
            foreach (SpellEffect layer in _layers)
            {
                layer.SetCastSource(source);
            }
        }

        public void SetEndpoints(Vector3 start, Vector3 end, bool isContactThread)
        {
            PruneLayers();
            _linkStart = start;
            _linkEnd = end;
            _isContactThread = isContactThread;
            foreach (SpellEffect layer in _layers)
            {
                layer.SetEndpoints(start, end, isContactThread);
            }

            Advance(0f);
        }

        public void Advance(float delta)
        {
            if (_recipe == null || !float.IsFinite(delta) || delta < 0f)
            {
                return;
            }

            AdvanceLayers(delta);
            _age += delta;
            _sinceStatus += delta;
            if (_isRemoving)
            {
                _removalAge += delta;
            }

            float time = _isStatus ? StatusTime() : _age;
            if (_recipe.socket == EffectSocket.Link)
            {
                if (_castSource != null && !_castSource.TryGet(out _linkStart))
                {
                    Dispose(gameObject);
                    return;
                }
                _parts.PoseLink(_linkStart, _linkEnd, _isContactThread, _age, _count);
                UpdateGround();
                _parts.Fade(Visibility());
                return;
            }

            float cycle = Mathf.Max(minCycle, _recipe.cycleSeconds);
            bool isVisible = true;
            float phase;
            bool polished = _recipe.presentation != null && _recipe.presentation.enabled;
            if (_recipe.tempo == EffectTempo.PerPeriod && _isStatus)
            {
                // Polished statuses retain a readable presence between gameplay ticks.
                isVisible = polished || time >= cycle;
                phase = Mathf.Repeat(time, cycle) / cycle;
            }
            else if (_recipe.tempo == EffectTempo.ForDuration && _isStatus)
            {
                phase = Mathf.Repeat(time, cycle) / cycle;
            }
            else
            {
                phase = Mathf.Clamp01(time / cycle);
                bool isHeld = _recipe.motion == EffectMotionKind.Close || _recipe.motion == EffectMotionKind.Orbit;
                isVisible = time < cycle || isHeld;
            }

            if (polished && _isStatus && _recipe.tempo == EffectTempo.PerPeriod)
            {
                phase = Mathf.Lerp(_recipe.presentation.idleVisibility * .3f, .94f, phase);
            }

            Pose(phase, time);
            UpdateGround();
            float fade = Visibility();
            if (!isVisible)
            {
                _parts.Fade(0f);
            }
            else
            {
                _parts.Fade(fade);
            }
        }

        // Lays every shape and stalk at one point of the motion,
        // the sink samples it to keep lasting elements off the head
        public void Pose(float phase, float time)
        {
            if (_recipe == null)
            {
                return;
            }

            MotionState state = new MotionState();
            state.isStatus = _isStatus;
            state.isRemoving = _isRemoving;
            state.removal = Mathf.Clamp01(_removalAge / RemovalSeconds());
            state.fallDistance = _fallDistance;
            for (int i = 0; i < _parts.shapes.Count; i++)
            {
                LookPart part = _parts.shapeParts[i];
                PartPose pose = EffectMotion.Pose(_recipe, part, i, phase, time, state);
                _parts.shapes[i].localPosition = pose.position;
                _parts.shapes[i].localRotation = pose.rotation;
                _parts.shapes[i].localScale = pose.scale;
                if (i < _parts.stalks.Count)
                {
                    _parts.PoseStalk(i, pose.position, pose.scale.x / Mathf.Max(minWidth, part.size.x));
                }
            }
        }

        // Keeps running past the duration: gameplay removes the status, and a clock frozen on a period boundary
        // would hold a growing element at nothing
        float StatusTime()
        {
            return _elapsedSeconds + _sinceStatus;
        }

    }
}
