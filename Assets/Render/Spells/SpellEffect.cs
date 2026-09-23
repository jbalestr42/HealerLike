using UnityEngine;
using UnityEngine.Events;

namespace HealerLike.Render.Spells
{
    public enum SpellEffectKind
    {
        Buff,
        Shield,
        Heal,
        Impact,
        Chain,
        Mana,
        Drip,
        Area,
        Litter
    }

    public class SpellEffect : MonoBehaviour
    {
        public UnityEvent<Color> OnBodyTint = new UnityEvent<Color>();

        public SpellEffectKind kind;
        public float lifetime = 0.65f;
        public Transform[] stalks = new Transform[0];
        public Transform[] parts = new Transform[0];
        public bool contactThread;

        [SerializeField] Renderer _sideRim;
        [SerializeField] Transform[] _stackBeads = new Transform[0];
        [SerializeField] Renderer[] _criticalRings = new Renderer[0];

        static readonly int baseColorId = Shader.PropertyToID("_BaseColor");
        static readonly Color gold = new Color32(242, 194, 48, 255);
        static readonly Color lime = new Color32(198, 242, 74, 255);
        static readonly Color coral = new Color32(242, 96, 122, 255);
        static readonly Color slate = new Color32(58, 66, 87, 255);

        Renderer[] _renderers;
        Color _baseColor;
        Vector3[] _positions;
        Vector3[] _scales;
        Quaternion[] _rotations;
        MaterialPropertyBlock _propertyBlock;
        float _age;
        bool _isReady = false;
        bool _isStatus = false;
        bool _isPeriodic = false;
        float _periodSeconds;
        Entity.EntityType _side;
        bool _hasSide = false;
        bool _hasShieldState = false;
        float _shieldState;
        bool _isRemoving = false;
        float _removalAge;
        Vector3 _linkStart;
        Vector3 _linkEnd;

        Color _bodyTint = Color.white;
        public Color bodyTint { get { return _bodyTint; } }

        int _stacks;
        public int stacks { get { return _stacks; } }

        float _elapsedSeconds;
        public float elapsedSeconds { get { return _elapsedSeconds; } }

        float _durationSeconds;
        public float durationSeconds { get { return _durationSeconds; } }

        ClockKind _clock;
        public ClockKind clock { get { return _clock; } }

        public bool removalComplete { get { return _isRemoving && _removalAge >= 0.25f; } }

        void Start()
        {
            Init();
        }

        void Update()
        {
            Advance(_clock == ClockKind.Realtime ? Time.unscaledDeltaTime : Time.deltaTime);
            if (removalComplete || (!_isStatus && _age >= lifetime))
            {
                Destroy(gameObject);
            }
        }

        void OnDisable()
        {
            if (_bodyTint != Color.white)
            {
                SetBodyTint(Color.white);
            }
        }

        public void Init()
        {
            if (_isReady)
            {
                return;
            }

            _propertyBlock = new MaterialPropertyBlock();
            _renderers = new Renderer[parts.Length];
            _positions = new Vector3[parts.Length];
            _scales = new Vector3[parts.Length];
            _rotations = new Quaternion[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                _renderers[i] = parts[i].GetComponent<Renderer>();
                _positions[i] = parts[i].localPosition;
                _scales[i] = parts[i].localScale;
                _rotations[i] = parts[i].localRotation;
            }

            SetColor(DefaultColor());
            _propertyBlock.SetColor(baseColorId, gold);
            foreach (Transform bead in _stackBeads)
            {
                bead.GetComponent<Renderer>().SetPropertyBlock(_propertyBlock);
            }
            _isReady = true;
        }

        // Colours every part, a look sets its own tint right after spawning
        public void SetColor(Color color)
        {
            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }

            _baseColor = color;
            _propertyBlock.SetColor(baseColorId, color);
            foreach (Transform part in parts)
            {
                if (part != null)
                {
                    part.GetComponent<Renderer>().SetPropertyBlock(_propertyBlock);
                }
            }
        }

        // Read from the buff handler, a periodic heal or drip only moves on its ticks
        public void SetPeriod(bool isPeriodic, float periodSeconds)
        {
            _isPeriodic = isPeriodic && float.IsFinite(periodSeconds) && periodSeconds > 0f;
            _periodSeconds = _isPeriodic ? periodSeconds : 0f;
        }

        public void SetStatus(int stacks, float elapsed, float duration, ClockKind clock)
        {
            Init();
            int visibleStacks = Mathf.Max(0, stacks);
            if (!_isStatus || _stacks != visibleStacks)
            {
                for (int i = 0; i < _stackBeads.Length; i++)
                {
                    _stackBeads[i].gameObject.SetActive(i < Mathf.Min(_stackBeads.Length, visibleStacks));
                }
            }

            _isStatus = true;
            _stacks = visibleStacks;
            _elapsedSeconds = Mathf.Max(0f, elapsed);
            _durationSeconds = duration;
            _clock = clock;
            if (kind == SpellEffectKind.Drip)
            {
                SetBodyTint(coral);
            }
            Advance(0f);
        }

        public void SetSide(Entity.EntityType side)
        {
            if (_sideRim == null || (_hasSide && _side == side))
            {
                return;
            }

            Init();
            _hasSide = true;
            _side = side;
            _sideRim.gameObject.SetActive(true);

            Color color = new Color32(201, 196, 180, 255);
            if (side == Entity.EntityType.Player)
            {
                color = new Color32(155, 210, 74, 255);
            }
            else if (side == Entity.EntityType.Computer)
            {
                color = slate;
            }
            _propertyBlock.SetColor(baseColorId, color);
            _sideRim.SetPropertyBlock(_propertyBlock);
        }

        public void ShowCritical()
        {
            Init();
            _propertyBlock.SetColor(baseColorId, kind == SpellEffectKind.Impact ? coral : lime);
            foreach (Renderer ring in _criticalRings)
            {
                ring.gameObject.SetActive(true);
                ring.SetPropertyBlock(_propertyBlock);
            }
        }

        // One plate per observed charge
        public void SetShieldState(float charges)
        {
            if (kind != SpellEffectKind.Shield || (_hasShieldState && _shieldState == charges))
            {
                return;
            }

            _hasShieldState = true;
            _shieldState = charges;
            int visible = Mathf.Clamp(Mathf.CeilToInt(charges), 0, parts.Length);
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i].gameObject.SetActive(i < visible);
            }
        }

        public void BeginRemoval()
        {
            _isRemoving = true;
            _removalAge = 0f;
            SetBodyTint(Color.white);
        }

        public void Advance(float delta)
        {
            Init();
            if (!float.IsFinite(delta) || delta < 0f)
            {
                return;
            }

            _age += delta;
            if (_isRemoving)
            {
                _removalAge += delta;
            }

            float statusTime = _elapsedSeconds;
            if (float.IsFinite(_durationSeconds) && _durationSeconds > 0f)
            {
                statusTime = Mathf.Min(statusTime, _durationSeconds);
            }

            if (kind == SpellEffectKind.Chain)
            {
                SetEndpoints(_linkStart, _linkEnd);
            }

            float fade = Mathf.Clamp01(1f - _age / Mathf.Max(0.01f, lifetime));
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == null)
                {
                    continue;
                }

                if (kind == SpellEffectKind.Buff)
                {
                    // Rotate the tilted plane around the body, spinning a torus in its own plane is invisible
                    float spin = i % 2 == 0 ? 34f : -28f;
                    parts[i].localRotation = Quaternion.Euler(0f, statusTime * spin, 0f) * _rotations[i];
                    parts[i].localScale = _scales[i] * (1f + 0.035f * Mathf.Sin(statusTime * 2.4f + i));
                    Brighten(i, 1.12f + 0.12f * Mathf.Sin(statusTime * 2.4f + i));
                }
                else if (kind == SpellEffectKind.Shield)
                {
                    float closure = _isRemoving ? 1f - Mathf.Clamp01(_removalAge * 4f) : Mathf.Clamp01(statusTime * 4f);
                    parts[i].localRotation = _rotations[i] * Quaternion.Euler(0f, 0f, Mathf.Lerp(-32f, 0f, closure));
                    parts[i].localPosition = _positions[i] * Mathf.Lerp(1.3f, 1f, closure);
                }
                else if (kind == SpellEffectKind.Drip)
                {
                    bool isTicking = _isPeriodic && statusTime >= _periodSeconds;
                    float phase = isTicking ? Mathf.Repeat(statusTime, _periodSeconds) / _periodSeconds : 0f;
                    parts[i].localPosition = _positions[i] + Vector3.down * phase * phase * 0.6f;
                    parts[i].localScale = _scales[i] * (isTicking ? Mathf.Clamp01((1f - phase) * 4f) : 0f);
                }
                else if (kind == SpellEffectKind.Area)
                {
                    parts[i].localScale = _scales[i] * Mathf.Clamp01(_age / 0.3f);
                }
                else if (kind == SpellEffectKind.Litter)
                {
                    float emergence = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_age / 0.15f));
                    float sinking = Mathf.Clamp01((_age - lifetime * 0.55f) / (lifetime * 0.45f));
                    float sink = Mathf.SmoothStep(0f, 1f, sinking);
                    parts[i].localPosition = _positions[i] + Vector3.down * (0.22f * (1f - emergence + sink));
                    parts[i].localScale = _scales[i] * emergence;
                }
                else if (kind == SpellEffectKind.Heal)
                {
                    AdvanceBud(i, statusTime);
                }
                else if (kind == SpellEffectKind.Mana)
                {
                    float rise = _isStatus ? 0f : _age;
                    parts[i].localPosition = _positions[i] + Vector3.up * (rise * (0.55f + i * 0.04f));
                }
                else if (kind == SpellEffectKind.Impact && i > 0)
                {
                    float time = _isStatus ? 0f : _age;
                    Vector3 velocity = new Vector3(_positions[i].x * 4f, 1.1f + i * 0.12f, _positions[i].z * 4f);
                    parts[i].localPosition = _positions[i] + velocity * time + Vector3.down * (2.8f * time * time);
                    parts[i].localRotation = _rotations[i] * Quaternion.Euler(time * 180f, time * 70f, 0f);
                }

                if (!_isStatus && (kind == SpellEffectKind.Buff || kind == SpellEffectKind.Shield
                    || kind == SpellEffectKind.Mana || kind == SpellEffectKind.Impact))
                {
                    parts[i].localScale = _scales[i] * Mathf.Sqrt(fade);
                }
            }
        }

        public void SetEndpoints(Vector3 start, Vector3 end)
        {
            Init();
            if (kind != SpellEffectKind.Chain)
            {
                return;
            }

            _linkStart = start;
            _linkEnd = end;
            float width = contactThread ? 0.012f : 0.025f;
            for (int i = 0; i < parts.Length; i++)
            {
                // Even parts are segments, odd parts are the beads travelling along them
                float t = (i / 2) / 16f;
                Vector3 point = LinkPoint(start, end, t);
                if (i % 2 == 0)
                {
                    Vector3 next = LinkPoint(start, end, Mathf.Min(1f, t + 1f / 16f));
                    parts[i].position = (point + next) * 0.5f;
                    parts[i].rotation = next == point ? Quaternion.identity : Quaternion.FromToRotation(Vector3.up, next - point);
                    parts[i].localScale = new Vector3(width, Vector3.Distance(point, next) * 0.5f, width);
                }
                else
                {
                    parts[i].gameObject.SetActive(!contactThread);
                    parts[i].position = Curve(start, end, Mathf.Repeat(t + _age / 0.6f, 1f));
                }
                Brighten(i, 1.15f + 0.25f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(_age / Mathf.Max(0.01f, lifetime))));
            }
        }

        void AdvanceBud(int index, float statusTime)
        {
            bool isPeriodic = _isStatus && _isPeriodic;
            float time = isPeriodic ? Mathf.Repeat(statusTime, _periodSeconds) / _periodSeconds : _age / Mathf.Max(0.01f, lifetime);
            bool isStill = _isStatus && !isPeriodic;
            float phase = isStill ? 0f : Mathf.Clamp01(time * (1f + index * 0.025f));
            float bud = 1f;
            if (!isStill)
            {
                float grow = Mathf.SmoothStep(0.2f, 1f, Mathf.Clamp01(phase / 0.6f));
                float pop = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((phase - 0.78f) / 0.16f));
                bud = grow * (1f - pop);
            }

            if (isPeriodic && statusTime < _periodSeconds)
            {
                bud = 0f;
            }

            parts[index].localPosition = _positions[index] + Vector3.up * phase * (0.45f + index * 0.025f);
            parts[index].localScale = _scales[index] * bud;
            if (index < stalks.Length && stalks[index] != null)
            {
                float height = parts[index].localPosition.y + 0.12f;
                stalks[index].localPosition = new Vector3(_positions[index].x, height * 0.5f - 0.12f, _positions[index].z);
                stalks[index].localScale = new Vector3(0.009f * bud, height * 0.5f, 0.009f * bud);
            }
        }

        Color DefaultColor()
        {
            switch (kind)
            {
                case SpellEffectKind.Heal:
                    return lime;
                case SpellEffectKind.Impact:
                case SpellEffectKind.Drip:
                    return coral;
                case SpellEffectKind.Litter:
                    return slate;
                case SpellEffectKind.Area:
                    return Color.white;
                default:
                    return gold;
            }
        }

        void SetBodyTint(Color color)
        {
            _bodyTint = color;
            OnBodyTint.Invoke(color);
        }

        void Brighten(int index, float brightness)
        {
            Color color = _baseColor * brightness;
            color.a = 1f;
            _propertyBlock.SetColor(baseColorId, color);
            _renderers[index].SetPropertyBlock(_propertyBlock);
        }

        Vector3 LinkPoint(Vector3 start, Vector3 end, float t)
        {
            if (!contactThread)
            {
                return Curve(start, end, t);
            }

            Vector3 side = Vector3.Cross((end - start).normalized, Vector3.up);
            float wave = Mathf.Sin(t * Mathf.PI * 8f) * Mathf.Sin(t * Mathf.PI) * 0.035f;
            return Vector3.Lerp(start, end, t) + side * wave;
        }

        static Vector3 Curve(Vector3 start, Vector3 end, float t)
        {
            float height = 4f * t * (1f - t) * Mathf.Min(0.7f, Vector3.Distance(start, end) * 0.2f);
            return Vector3.Lerp(start, end, t) + Vector3.up * height;
        }
    }
}
