using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace HealerLike.Render.Spells
{
    public enum HLSpellEffectKind
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

    public class HLSpellEffect : MonoBehaviour
    {
        public UnityEvent<Color> OnTint = new UnityEvent<Color>();

        public HLSpellEffectKind kind;
        public Material material;
        public float lifetime = 0.65f;
        [FormerlySerializedAs("PeriodSeconds")]
        public float periodSeconds;
        public bool hasAuthoredSignature;
        public HLSpellSignature authoredSignature;
        public Transform[] stalks = new Transform[0];
        public Transform[] parts = new Transform[0];
        // Optional explicit camera; otherwise the tagged main camera is read in LateUpdate.
        [FormerlySerializedAs("FacingCamera")]
        public Transform facingCamera;
        [FormerlySerializedAs("ContactThread")]
        public bool contactThread;

        static readonly int baseColorId = Shader.PropertyToID("_BaseColor");

        Renderer[] _renderers;
        Color _baseColor;
        Vector3[] _positions;
        Vector3[] _scales;
        Quaternion[] _rotations;
        float _age;
        bool _ready;
        bool _hasSignature;
        bool _ownsPrimitives;
        Transform[] _stackBeads;
        Renderer _sideRim;
        Entity.EntityType _side;
        bool _hasSide;
        bool _hasShieldState;
        float? _shieldState;
        bool _removing;
        float _removalAge;
        Vector3 _linkStart;
        Vector3 _linkEnd;

        MaterialPropertyBlock _propertyBlock;
        MaterialPropertyBlock propertyBlock
        {
            get
            {
                if (_propertyBlock == null)
                {
                    _propertyBlock = new MaterialPropertyBlock();
                }
                return _propertyBlock;
            }
        }

        Color _tint = Color.white;
        public Color tint { get { return _tint; } }

        int _stacks;
        public int stacks { get { return _stacks; } }

        float _elapsedSeconds;
        public float elapsedSeconds { get { return _elapsedSeconds; } }

        float _durationSeconds;
        public float durationSeconds { get { return _durationSeconds; } }

        HLClockKind _clock;
        public HLClockKind clock { get { return _clock; } }

        HLSpellSignature _signature;
        public HLSpellSignature signature { get { return _signature; } }

        public bool removalComplete { get { return _removing && _removalAge >= 0.25f; } }

        void Start()
        {
            Initialize();
        }

        void Update()
        {
            Advance(_clock == HLClockKind.Realtime ? Time.unscaledDeltaTime : Time.deltaTime);
            if (removalComplete || (!_hasSignature && _age >= lifetime))
            {
                Destroy(gameObject);
            }
        }

        void LateUpdate()
        {
            if (kind != HLSpellEffectKind.Impact)
            {
                return;
            }
            Transform cameraTransform = facingCamera;
            if (!cameraTransform)
            {
                Camera camera = Camera.main;
                if (camera)
                {
                    cameraTransform = camera.transform;
                }
            }
            FaceCamera(cameraTransform);
        }

        void OnDisable()
        {
            if (_tint != Color.white)
            {
                SetTint(Color.white);
            }
        }

        void OnDestroy()
        {
            ReleaseResources();
        }

        public void Initialize()
        {
            if (_ready)
            {
                return;
            }
            RetainPrimitives();
            if (parts == null || parts.Length == 0)
            {
                HLSpellPrimitives.Build(this);
            }
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

            Color color = new Color32(242, 194, 48, 255);
            if (kind == HLSpellEffectKind.Heal)
            {
                color = new Color32(198, 242, 74, 255);
            }
            else if (kind == HLSpellEffectKind.Impact || kind == HLSpellEffectKind.Drip)
            {
                color = new Color32(242, 96, 122, 255);
            }
            else if (kind == HLSpellEffectKind.Litter)
            {
                color = new Color32(58, 66, 87, 255);
            }
            else if (kind == HLSpellEffectKind.Area)
            {
                color = Color.white;
            }
            _baseColor = color;
            MaterialPropertyBlock block = propertyBlock;
            block.SetColor(baseColorId, color);
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            {
                if (material)
                {
                    renderer.sharedMaterial = material;
                }
                renderer.SetPropertyBlock(block);
            }
            if (hasAuthoredSignature)
            {
                ApplySignatureColor(authoredSignature);
            }
            _ready = true;
        }

        public void SetStatus(int stacks, float elapsed, float duration, HLClockKind clock, HLSpellSignature signature)
        {
            Initialize();
            bool signatureChanged = !_hasSignature || !_signature.Equals(signature);
            if (!_hasSignature)
            {
                if (!hasAuthoredSignature)
                {
                    HLSpellPrimitives.AddMarker(this, signature);
                }
                _hasSignature = true;
            }
            bool stacksChanged = _stackBeads == null || _stacks != Mathf.Max(0, stacks);
            if (_stackBeads == null)
            {
                _stackBeads = HLSpellPrimitives.StackBeads(this);
            }
            if (stacksChanged)
            {
                for (int i = 0; i < _stackBeads.Length; i++)
                {
                    _stackBeads[i].gameObject.SetActive(i < Mathf.Min(8, stacks));
                }
            }
            _stacks = Mathf.Max(0, stacks);
            _elapsedSeconds = Mathf.Max(0f, elapsed);
            _durationSeconds = duration;
            _clock = clock;
            _signature = signature;
            if (signatureChanged)
            {
                ApplySignatureColor(signature);
                _hasShieldState = false;
            }
            if (kind == HLSpellEffectKind.Drip && signature.sign != HLSign.Positive)
            {
                SetTint(new Color32(242, 96, 122, 255));
            }
            Advance(0f);
        }

        public void SetSide(Entity.EntityType side)
        {
            if (_hasSide && _side == side && _sideRim)
            {
                return;
            }
            _hasSide = true;
            _side = side;
            if (!_sideRim)
            {
                _sideRim = HLSpellPrimitives.SideRim(this);
            }

            Color color = new Color32(201, 196, 180, 255);
            if (side == Entity.EntityType.Player)
            {
                color = new Color32(155, 210, 74, 255);
            }
            else if (side == Entity.EntityType.Computer)
            {
                color = new Color32(58, 66, 87, 255);
            }
            MaterialPropertyBlock block = propertyBlock;
            block.SetColor("_BaseColor", color);
            _sideRim.SetPropertyBlock(block);
        }

        public void SetShieldState(float? observedCharges)
        {
            if (kind != HLSpellEffectKind.Shield)
            {
                return;
            }
            if (_hasShieldState && _shieldState == observedCharges)
            {
                return;
            }
            _hasShieldState = true;
            _shieldState = observedCharges;
            bool isHitArmor = _signature.operation == HLOperation.Attribute
                && _signature.attribute == AttributeType.HitArmor;
            for (int i = 0; i < parts.Length; i++)
            {
                bool visible = !isHitArmor
                    || !observedCharges.HasValue
                    || i < Mathf.Clamp(Mathf.CeilToInt(observedCharges.Value), 0, 6);
                parts[i].gameObject.SetActive(visible);
            }
        }

        public void BeginRemoval()
        {
            _removing = true;
            _removalAge = 0f;
            SetTint(Color.white);
        }

        // Explicit teardown also supports editor authoring, where runtime messages do not run.
        public void ReleaseResources()
        {
            if (_ownsPrimitives)
            {
                _ownsPrimitives = false;
                HLSpellPrimitives.ReleaseUser();
            }
        }

        public void RetainPrimitives()
        {
            if (!_ownsPrimitives)
            {
                HLSpellPrimitives.Retain();
                _ownsPrimitives = true;
            }
        }

        public void FaceCamera(Transform cameraTransform)
        {
            if (kind == HLSpellEffectKind.Impact && cameraTransform && parts.Length > 0)
            {
                parts[0].rotation = cameraTransform.rotation;
            }
        }

        public void Advance(float delta)
        {
            Initialize();
            if (!HLSpellGrammar.Finite(delta) || delta < 0f)
            {
                return;
            }
            _age += delta;
            if (_removing)
            {
                _removalAge += delta;
            }
            float statusTime = _elapsedSeconds;
            if (HLSpellGrammar.Finite(_durationSeconds) && _durationSeconds > 0f)
            {
                statusTime = Mathf.Min(statusTime, _durationSeconds);
            }
            if (kind == HLSpellEffectKind.Chain)
            {
                SetEndpoints(_linkStart, _linkEnd);
            }
            float fade = Mathf.Clamp01(1f - _age / Mathf.Max(0.01f, lifetime));
            for (int i = 0; i < parts.Length; i++)
            {
                if (!parts[i])
                {
                    continue;
                }
                if (kind == HLSpellEffectKind.Buff)
                {
                    // Rotate the tilted plane around the body; spinning a torus in its own plane is invisible.
                    float spin = i % 2 == 0 ? 34f : -28f;
                    parts[i].localRotation = Quaternion.Euler(0f, statusTime * spin, 0f) * _rotations[i];
                    parts[i].localScale = _scales[i] * (1f + 0.035f * Mathf.Sin(statusTime * 2.4f + i));
                    Brighten(i, 1.12f + 0.12f * Mathf.Sin(statusTime * 2.4f + i));
                }
                else if (kind == HLSpellEffectKind.Shield)
                {
                    float closure = _removing
                        ? 1f - Mathf.Clamp01(_removalAge * 4f)
                        : Mathf.Clamp01(statusTime * 4f);
                    float closedAngle = 0f;
                    if (_signature.operation == HLOperation.Attribute
                        && _signature.attribute == AttributeType.PercentArmor)
                    {
                        closedAngle = -16f;
                    }
                    parts[i].localRotation =
                        _rotations[i] * Quaternion.Euler(0f, 0f, Mathf.Lerp(-32f, closedAngle, closure));
                    parts[i].localPosition = _positions[i] * Mathf.Lerp(1.3f, 1f, closure);
                }
                else if (kind == HLSpellEffectKind.Drip)
                {
                    bool ticking = HLSpellGrammar.Finite(periodSeconds)
                        && periodSeconds > 0f
                        && statusTime >= periodSeconds;
                    float phase = ticking ? Mathf.Repeat(statusTime, periodSeconds) / periodSeconds : 0f;
                    Vector3 direction = _signature.sign == HLSign.Positive ? Vector3.up : Vector3.down;
                    parts[i].localPosition = _positions[i] + direction * phase * phase * 0.6f;
                    parts[i].localScale = _scales[i] * (ticking ? Mathf.Clamp01((1f - phase) * 4f) : 0f);
                }
                else if (kind == HLSpellEffectKind.Area)
                {
                    parts[i].localScale = _scales[i] * Mathf.Clamp01(_age / 0.3f);
                }
                else if (kind == HLSpellEffectKind.Litter)
                {
                    float emergence = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_age / 0.15f));
                    float sinking = Mathf.Clamp01((_age - lifetime * 0.55f) / (lifetime * 0.45f));
                    float sink = Mathf.SmoothStep(0f, 1f, sinking);
                    parts[i].localPosition = _positions[i] + Vector3.down * (0.22f * (1f - emergence + sink));
                    parts[i].localScale = _scales[i] * emergence;
                }
                else if (kind == HLSpellEffectKind.Heal)
                {
                    bool periodic = _hasSignature
                        && _signature.tempo == HLTempo.HandlerTick
                        && HLSpellGrammar.Finite(periodSeconds)
                        && periodSeconds > 0f;
                    float time = periodic
                        ? Mathf.Repeat(statusTime, periodSeconds) / periodSeconds
                        : _age / Mathf.Max(0.01f, lifetime);
                    bool isStill = _hasSignature && !periodic;
                    float phase = isStill ? 0f : Mathf.Clamp01(time * (1f + i * 0.025f));
                    float bud = 1f;
                    if (!isStill)
                    {
                        float grow = Mathf.SmoothStep(0.2f, 1f, Mathf.Clamp01(phase / 0.6f));
                        float pop = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((phase - 0.78f) / 0.16f));
                        bud = grow * (1f - pop);
                    }
                    if (periodic && statusTime < periodSeconds)
                    {
                        bud = 0f;
                    }
                    parts[i].localPosition = _positions[i] + Vector3.up * phase * (0.45f + i * 0.025f);
                    parts[i].localScale = _scales[i] * bud;
                    if (i < stalks.Length && stalks[i])
                    {
                        float height = parts[i].localPosition.y + 0.12f;
                        stalks[i].localPosition = new Vector3(_positions[i].x, height * 0.5f - 0.12f, _positions[i].z);
                        stalks[i].localScale = new Vector3(0.009f * bud, height * 0.5f, 0.009f * bud);
                    }
                }
                else if (kind == HLSpellEffectKind.Mana)
                {
                    float rise = _hasSignature ? 0f : _age;
                    parts[i].localPosition = _positions[i] + Vector3.up * (rise * (0.55f + i * 0.04f));
                }
                else if (kind == HLSpellEffectKind.Impact)
                {
                    float time = _hasSignature ? 0f : _age;
                    if (i > 0)
                    {
                        Vector3 velocity = new Vector3(_positions[i].x * 4f, 1.1f + i * 0.12f, _positions[i].z * 4f);
                        parts[i].localPosition = _positions[i] + velocity * time + Vector3.down * (2.8f * time * time);
                        parts[i].localRotation = _rotations[i] * Quaternion.Euler(time * 180f, time * 70f, 0f);
                    }
                }
                if (
                    !_hasSignature
                    && kind != HLSpellEffectKind.Chain
                    && kind != HLSpellEffectKind.Area
                    && kind != HLSpellEffectKind.Heal
                    && kind != HLSpellEffectKind.Litter
                    && kind != HLSpellEffectKind.Drip
                )
                {
                    parts[i].localScale = _scales[i] * Mathf.Sqrt(fade);
                }
            }
        }

        public void SetEndpoints(Vector3 start, Vector3 end)
        {
            Initialize();
            if (kind != HLSpellEffectKind.Chain)
            {
                return;
            }
            _linkStart = start;
            _linkEnd = end;
            float width = contactThread ? 0.012f : 0.025f;
            for (int i = 0; i < parts.Length; i++)
            {
                // Even parts are segments, odd parts are the beads travelling along them.
                float t = (i / 2) / 16f;
                Vector3 p = LinkPoint(start, end, t);
                if (i % 2 == 0)
                {
                    Vector3 q = LinkPoint(start, end, Mathf.Min(1f, t + 1f / 16f));
                    parts[i].position = (p + q) * 0.5f;
                    parts[i].rotation = q == p ? Quaternion.identity : Quaternion.FromToRotation(Vector3.up, q - p);
                    parts[i].localScale = new Vector3(width, Vector3.Distance(p, q) * 0.5f, width);
                }
                else
                {
                    parts[i].gameObject.SetActive(!contactThread);
                    parts[i].position = Curve(start, end, Mathf.Repeat(t + _age / 0.6f, 1f));
                }
                Brighten(i, 1.15f + 0.25f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(_age / Mathf.Max(0.01f, lifetime))));
            }
        }

        void SetTint(Color color)
        {
            _tint = color;
            OnTint.Invoke(color);
        }

        void ApplySignatureColor(HLSpellSignature signature)
        {
            Color color = new Color32(242, 194, 48, 255);
            if (kind == HLSpellEffectKind.Shield)
            {
                color = new Color32(151, 203, 99, 255);
            }
            else if (signature.sign == HLSign.Negative)
            {
                color = new Color32(242, 96, 122, 255);
            }
            else if (
                signature.operation == HLOperation.Resource
                && signature.sign == HLSign.Positive
                && signature.attribute == AttributeType.HealthMax
            )
            {
                color = new Color32(198, 242, 74, 255);
            }
            _baseColor = color;
            MaterialPropertyBlock block = propertyBlock;
            block.SetColor(baseColorId, color);
            foreach (Transform part in parts)
            {
                if (part)
                {
                    part.GetComponent<Renderer>().SetPropertyBlock(block);
                }
            }
        }

        void Brighten(int index, float brightness)
        {
            Color color = _baseColor * brightness;
            color.a = 1f;
            MaterialPropertyBlock block = propertyBlock;
            block.SetColor(baseColorId, color);
            _renderers[index].SetPropertyBlock(block);
        }

        Vector3 LinkPoint(Vector3 a, Vector3 b, float t)
        {
            if (!contactThread)
            {
                return Curve(a, b, t);
            }
            Vector3 side = Vector3.Cross((b - a).normalized, Vector3.up);
            float wave = Mathf.Sin(t * Mathf.PI * 8f) * Mathf.Sin(t * Mathf.PI) * 0.035f;
            return Vector3.Lerp(a, b, t) + side * wave;
        }

        static Vector3 Curve(Vector3 a, Vector3 b, float t)
        {
            float height = 4f * t * (1f - t) * Mathf.Min(0.7f, Vector3.Distance(a, b) * 0.2f);
            return Vector3.Lerp(a, b, t) + Vector3.up * height;
        }
    }
}
