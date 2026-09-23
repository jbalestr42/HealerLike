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

        static readonly int baseColorId = Shader.PropertyToID("_BaseColor");
        static readonly float riseHeight = 1.8f;
        static readonly float pressDepth = 0.2f;
        static readonly float removalSeconds = 0.25f;
        static readonly float threadWidth = 0.012f;
        static readonly float beamWidth = 0.025f;

        EffectRecipe _recipe;
        readonly List<Transform> _shapes = new List<Transform>();
        readonly List<LookPart> _shapeParts = new List<LookPart>();
        readonly List<Transform> _stalks = new List<Transform>();
        readonly List<LookPart> _stalkParts = new List<LookPart>();
        readonly List<Transform> _beads = new List<Transform>();
        readonly List<Transform> _rings = new List<Transform>();
        readonly List<Transform> _rims = new List<Transform>();
        readonly List<Transform> _all = new List<Transform>();
        MaterialPropertyBlock _block;
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

        ClockKind _clock;
        public ClockKind clock { get { return _clock; } }

        int _count;
        public int count { get { return _count; } }

        bool _isContactThread = false;
        public bool isContactThread { get { return _isContactThread; } }

        public EffectRecipe recipe { get { return _recipe; } }
        public EffectElement element { get { return _recipe != null ? _recipe.element : EffectElement.Burst; } }
        public float lifetime { get { return _recipe != null ? _recipe.cycleSeconds : 0f; } }
        public List<Transform> shapes { get { return _shapes; } }
        public List<Transform> stalks { get { return _stalks; } }
        public List<Transform> parts { get { return _all; } }
        public List<Transform> rings { get { return _rings; } }
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
            Advance(_clock == ClockKind.Realtime ? Time.unscaledDeltaTime : Time.deltaTime);
            if (removalComplete || (!_isStatus && _age >= lifetime))
            {
                Dispose(gameObject);
            }
        }

        public void Init(EffectRecipe recipe, PrimitiveMeshes meshes, Material material)
        {
            if (recipe == null || meshes == null)
            {
                Debug.LogError("[SpellEffect] Init needs a recipe and the primitive meshes.");
                return;
            }

            _recipe = recipe;
            _block = new MaterialPropertyBlock();
            foreach (LookPart part in recipe.entry.parts)
            {
                bool isStalk = part.role == PartRole.Stem;
                Transform built = Build(part, meshes, material, Colour(part));
                if (isStalk)
                {
                    _stalks.Add(built);
                    _stalkParts.Add(part);
                }
                else
                {
                    _shapes.Add(built);
                    _shapeParts.Add(part);
                }
            }

            BuildAll(recipe.entry.stackBeads, meshes, material, _beads);
            BuildAll(recipe.entry.criticalRings, meshes, material, _rings);
            BuildAll(recipe.entry.sideRim, meshes, material, _rims);
            if (recipe.socket == EffectSocket.Link)
            {
                SortByX(_shapes, _shapeParts);
                SortByX(_stalks, _stalkParts);
            }

            SetCount(recipe.count);
            Advance(0f);
        }

        public void SetCount(int shown)
        {
            _count = Mathf.Clamp(shown, 0, _shapes.Count);
            for (int i = 0; i < _shapes.Count; i++)
            {
                _shapes[i].gameObject.SetActive(i < _count);
                if (i < _stalks.Count)
                {
                    _stalks[i].gameObject.SetActive(i < _count);
                }
            }
        }

        public void SetStatus(int stacks, float elapsed, float duration, ClockKind clock)
        {
            int visibleStacks = Mathf.Max(0, stacks);
            if (!_isStatus || _stacks != visibleStacks)
            {
                for (int i = 0; i < _beads.Count; i++)
                {
                    _beads[i].gameObject.SetActive(i < visibleStacks);
                }
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
            _clock = clock;
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
            if (_rims.Count == 0 || _recipe == null || _recipe.palette == null)
            {
                return;
            }

            Color colour = _recipe.palette.stoneBody;
            if (side == Entity.EntityType.Player)
            {
                colour = _recipe.palette.plantBody;
            }
            else if (side == Entity.EntityType.Computer)
            {
                colour = _recipe.palette.baneLit;
            }

            foreach (Transform rim in _rims)
            {
                rim.gameObject.SetActive(true);
                Paint(rim, colour);
            }
        }

        public void ShowCritical()
        {
            foreach (Transform ring in _rings)
            {
                ring.gameObject.SetActive(true);
            }
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
                PoseLink();
                return;
            }

            float cycle = Mathf.Max(0.01f, _recipe.cycleSeconds);
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
                isVisible = time < cycle || _recipe.motion == EffectMotion.Close || _recipe.motion == EffectMotion.Orbit;
            }

            Pose(phase, time);
            float fade = _isRemoving ? 1f - Mathf.Clamp01(_removalAge / removalSeconds) : 1f;
            if (!isVisible || fade < 1f)
            {
                Fade(isVisible ? fade : 0f);
            }
        }

        // Lays every shape and stalk at one point of the motion, the sink samples it to keep lasting elements off the head
        public void Pose(float phase, float time)
        {
            if (_recipe == null)
            {
                return;
            }

            for (int i = 0; i < _shapes.Count; i++)
            {
                LookPart part = _shapeParts[i];
                Quaternion rotation = Quaternion.Euler(part.euler);
                Vector3 position = part.position;
                Vector3 scale = part.size;
                PoseShape(i, part, phase, time, ref position, ref rotation, ref scale);
                _shapes[i].localPosition = position;
                _shapes[i].localRotation = rotation;
                _shapes[i].localScale = scale;
                if (i < _stalks.Count)
                {
                    PoseStalk(i, position, scale.x / Mathf.Max(0.0001f, part.size.x));
                }
            }
        }

        void PoseShape(int index, LookPart part, float phase, float time, ref Vector3 position, ref Quaternion rotation,
                       ref Vector3 scale)
        {
            switch (_recipe.motion)
            {
                case EffectMotion.Burst:
                {
                    Vector3 flat = new Vector3(part.position.x, 0f, part.position.z);
                    float seconds = phase * _recipe.cycleSeconds;
                    float shrink = Mathf.Sqrt(1f - phase);
                    // The flat star pops in place, the cones are its shards
                    if (part.primitive != Primitive.Cone)
                    {
                        scale = part.size * ((0.6f + 0.4f * Mathf.SmoothStep(0f, 1f, phase / 0.3f)) * shrink);
                        break;
                    }

                    // Shards fly outward and fall
                    Vector3 velocity = flat * 4f + Vector3.up * (5.5f + index * 0.6f);
                    position = part.position + velocity * seconds + Vector3.down * (14f * seconds * seconds);
                    rotation = rotation * Quaternion.Euler(seconds * 180f, seconds * 70f, 0f);
                    scale = part.size * shrink;
                    break;
                }
                case EffectMotion.Rise:
                {
                    float own = Mathf.Clamp01(phase * (1f + index * 0.025f));
                    float grow = Mathf.SmoothStep(0.2f, 1f, Mathf.Clamp01(own / 0.6f));
                    float pop = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((own - 0.78f) / 0.16f));
                    position = part.position + Vector3.up * (own * riseHeight);
                    scale = part.size * (grow * (1f - pop));
                    break;
                }
                case EffectMotion.Grow:
                {
                    float emerge = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(phase / 0.3f));
                    // A single run sinks away at its end, a ticking or held one stays up until it grows again
                    float exit = 0f;
                    if (_recipe.tempo == EffectTempo.Once || !_isStatus)
                    {
                        exit = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((phase - 0.8f) / 0.2f));
                    }
                    // After its first growth a ticking or held element stays up and each tick lifts it from lower down
                    if (_isStatus && _recipe.tempo != EffectTempo.Once && time >= 2f * _recipe.cycleSeconds)
                    {
                        position = new Vector3(part.position.x, part.position.y * (0.7f + 0.3f * emerge), part.position.z);
                        break;
                    }

                    position = new Vector3(part.position.x, part.position.y * emerge - part.size.y * 0.5f * exit, part.position.z);
                    scale = part.size * (emerge * (1f - exit));
                    break;
                }
                case EffectMotion.Fall:
                {
                    // A drop swells where it hangs for the first part of the cycle, then falls and shrinks at the end
                    float own = Mathf.Clamp01(phase * 1.15f - index * 0.03f);
                    float fall = Mathf.Clamp01((own - 0.4f) / 0.6f);
                    position = part.position + Vector3.down * (fall * fall * _fallDistance);
                    scale = part.size * (Mathf.SmoothStep(0.3f, 1f, Mathf.Clamp01(own / 0.2f)) * Mathf.Clamp01((1f - fall) * 6f));
                    break;
                }
                case EffectMotion.Orbit:
                {
                    // The tilted plane turns around the body, spinning a torus in its own plane would not show
                    float spin = index % 2 == 0 ? 34f : -28f;
                    rotation = Quaternion.Euler(0f, time * spin, 0f) * rotation;
                    scale = part.size * (1f + 0.035f * Mathf.Sin(time * 2.4f + index));
                    break;
                }
                case EffectMotion.Close:
                {
                    float closure = Mathf.Clamp01(time / Mathf.Max(0.01f, _recipe.cycleSeconds));
                    if (_isRemoving)
                    {
                        closure = 1f - Mathf.Clamp01(_removalAge / removalSeconds);
                    }

                    rotation = rotation * Quaternion.Euler(0f, 0f, Mathf.Lerp(-32f, 0f, closure));
                    position = new Vector3(part.position.x * Mathf.Lerp(1.3f, 1f, closure), part.position.y,
                                           part.position.z * Mathf.Lerp(1.3f, 1f, closure));
                    break;
                }
                case EffectMotion.Press:
                {
                    position = part.position + Vector3.down * (pressDepth * (0.5f - 0.5f * Mathf.Cos(phase * Mathf.PI * 2f)));
                    break;
                }
                case EffectMotion.Shed:
                {
                    float own = Mathf.Repeat(phase + index * 0.13f, 1f);
                    float fall = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((own - 0.55f) / 0.45f));
                    float appear = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(own / 0.15f));
                    Vector3 outward = new Vector3(part.position.x, 0f, part.position.z).normalized;
                    position = part.position + Vector3.down * (fall * 0.6f) + outward * (fall * 0.25f);
                    rotation = rotation * Quaternion.Euler(fall * 25f, 0f, 0f);
                    scale = part.size * (appear * (1f - fall));
                    break;
                }
            }
        }

        // A stalk runs from the socket's floor up to its shape
        void PoseStalk(int index, Vector3 top, float width)
        {
            LookPart part = _stalkParts[index];
            float height = Mathf.Max(0f, top.y);
            _stalks[index].localPosition = new Vector3(top.x, height * 0.5f, top.z);
            _stalks[index].localRotation = Quaternion.identity;
            _stalks[index].localScale = new Vector3(part.size.x * width, height, part.size.z * width);
        }

        // Beads travel along segments on a curve from start to end, a contact thread keeps only the segments
        void PoseLink()
        {
            int segments = Mathf.Max(1, _stalks.Count);
            float width = _isContactThread ? threadWidth : beamWidth;
            for (int i = 0; i < _stalks.Count; i++)
            {
                float t = (float)i / segments;
                Vector3 point = LinkPoint(t);
                Vector3 next = LinkPoint(Mathf.Min(1f, t + 1f / segments));
                _stalks[i].position = (point + next) * 0.5f;
                _stalks[i].rotation = next == point ? Quaternion.identity : Quaternion.FromToRotation(Vector3.up, next - point);
                _stalks[i].localScale = new Vector3(width, Vector3.Distance(point, next), width);
            }

            for (int i = 0; i < _shapes.Count; i++)
            {
                _shapes[i].gameObject.SetActive(!_isContactThread && i < _count);
                _shapes[i].position = Curve(Mathf.Repeat((float)i / Mathf.Max(1, _shapes.Count) + _age / 0.6f, 1f));
            }
        }

        Vector3 LinkPoint(float t)
        {
            if (!_isContactThread)
            {
                return Curve(t);
            }

            Vector3 side = Vector3.Cross((_linkEnd - _linkStart).normalized, Vector3.up);
            float wave = Mathf.Sin(t * Mathf.PI * 8f) * Mathf.Sin(t * Mathf.PI) * 0.035f;
            return Vector3.Lerp(_linkStart, _linkEnd, t) + side * wave;
        }

        Vector3 Curve(float t)
        {
            float height = 4f * t * (1f - t) * Mathf.Min(0.7f, Vector3.Distance(_linkStart, _linkEnd) * 0.2f);
            return Vector3.Lerp(_linkStart, _linkEnd, t) + Vector3.up * height;
        }

        // Keeps running past the duration: gameplay removes the status, and a clock frozen on a period boundary
        // would hold a growing element at nothing
        float StatusTime()
        {
            return _elapsedSeconds + _sinceStatus;
        }

        void Fade(float fade)
        {
            foreach (Transform shape in _shapes)
            {
                shape.localScale *= fade;
            }

            foreach (Transform stalk in _stalks)
            {
                stalk.localScale *= fade;
            }
        }

        Color Colour(LookPart part)
        {
            // A beam's segments and beads all take the accent
            if (part.colour == ColourRole.Accent || _recipe.socket == EffectSocket.Link || _recipe.palette == null)
            {
                return _recipe.colour;
            }
            return _recipe.palette.Colour(part.colour, _recipe.family);
        }

        void BuildAll(LookPart[] source, PrimitiveMeshes meshes, Material material, List<Transform> built)
        {
            foreach (LookPart part in source)
            {
                Transform partTransform = Build(part, meshes, material, Colour(part));
                partTransform.gameObject.SetActive(false);
                built.Add(partTransform);
            }
        }

        Transform Build(LookPart part, PrimitiveMeshes meshes, Material material, Color colour)
        {
            Transform built = PrimitiveMeshes.Geometry(part.id, transform, meshes.GetMesh(part.primitive, 0), material, colour, part.glow);
            built.localPosition = part.position;
            built.localRotation = Quaternion.Euler(part.euler);
            built.localScale = part.size;
            _all.Add(built);
            return built;
        }

        void Paint(Transform part, Color colour)
        {
            _block.SetColor(baseColorId, colour);
            part.GetComponent<Renderer>().SetPropertyBlock(_block);
        }

        static void SortByX(List<Transform> built, List<LookPart> source)
        {
            for (int i = 1; i < source.Count; i++)
            {
                for (int j = i; j > 0 && source[j].position.x < source[j - 1].position.x; j--)
                {
                    LookPart part = source[j];
                    source[j] = source[j - 1];
                    source[j - 1] = part;
                    Transform swap = built[j];
                    built[j] = built[j - 1];
                    built[j - 1] = swap;
                }
            }
        }

        // Also runs from edit mode tests, where Destroy is not allowed
        public static void Dispose(GameObject effect)
        {
            if (effect == null)
            {
                return;
            }

            effect.SetActive(false);
            if (Application.isPlaying)
            {
                Destroy(effect);
            }
            else
            {
                DestroyImmediate(effect);
            }
        }
    }
}
