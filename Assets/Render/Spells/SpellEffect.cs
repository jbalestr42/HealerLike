using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Spells
{
    // One element built from its recipe, no prefab: the parts come from the vocabulary and move by the recipe's motion
    public class SpellEffect : MonoBehaviour
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
        public EffectElement element { get { return _recipe != null ? _recipe.element : EffectElement.Burst; } }
        public float lifetime { get { return _recipe != null ? _recipe.cycleSeconds : 0f; } }
        public List<Transform> shapes { get { return _parts.shapes; } }
        public List<Transform> stalks { get { return _parts.stalks; } }
        public List<Transform> parts { get { return _parts.all; } }
        public List<Transform> rings { get { return _parts.rings; } }
        public bool removalComplete { get { return _isRemoving && _removalAge >= removalSeconds; } }

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

            _recipe = recipe;
            _parts.Build(recipe, transform, meshes, material, targetSide);
            SetCount(recipe.count);
            Advance(0f);
        }

        public void SetCount(int shown)
        {
            _count = _parts.ShowShapes(shown);
        }

        public void SetStatus(int stacks, float elapsed, float duration)
        {
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
            _parts.ShowSide(side);
        }

        public void ShowCritical()
        {
            _parts.ShowCritical();
        }

        public void BeginRemoval()
        {
            _isRemoving = true;
            _removalAge = 0f;
        }

        public void SetEndpoints(Vector3 start, Vector3 end, bool isContactThread)
        {
            _linkStart = start;
            _linkEnd = end;
            _isContactThread = isContactThread;
            Advance(0f);
        }

        public void Advance(float delta)
        {
            if (_recipe == null || !float.IsFinite(delta) || delta < 0f)
            {
                return;
            }

            _age += delta;
            _sinceStatus += delta;
            if (_isRemoving)
            {
                _removalAge += delta;
            }

            float time = _isStatus ? StatusTime() : _age;
            if (_recipe.socket == EffectSocket.Link)
            {
                _parts.PoseLink(_linkStart, _linkEnd, _isContactThread, _age, _count);
                return;
            }

            float cycle = Mathf.Max(minCycle, _recipe.cycleSeconds);
            bool isVisible = true;
            float phase;
            if (_recipe.tempo == EffectTempo.PerPeriod && _isStatus)
            {
                // A ticking status moves on its ticks, so nothing shows before the first one
                isVisible = time >= cycle;
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

            Pose(phase, time);
            float fade = _isRemoving ? 1f - Mathf.Clamp01(_removalAge / removalSeconds) : 1f;
            if (!isVisible)
            {
                _parts.Fade(0f);
            }
            else if (fade < 1f)
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
            state.removal = Mathf.Clamp01(_removalAge / removalSeconds);
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

        // A new element under the parent; on a unit, its body and stem parts take the unit's side
        public static SpellEffect Create(EffectRecipe recipe, Transform parent, PrimitiveMeshes meshes,
            Material material, GameObject target)
        {
            if (recipe == null || meshes == null)
            {
                return null;
            }

            LookSide targetSide = LookSide.Plant;
            Entity entity = null;
            if (target != null)
            {
                entity = target.GetComponent<Entity>();
            }

            if (entity != null)
            {
                targetSide = LookDerivation.Side(entity.entityType);
            }

            GameObject effectGo = new GameObject(recipe.element.ToString());
            effectGo.transform.SetParent(parent, false);
            SpellEffect effect = effectGo.AddComponent<SpellEffect>();
            effect.Init(recipe, meshes, material, targetSide);
            return effect;
        }

        // Also runs from edit mode tests, where Destroy is not allowed
        public static void Dispose(GameObject effect)
        {
            if (effect == null)
            {
                return;
            }

            effect.SetActive(false);
            RenderObjects.Release(effect);
        }
    }
}
