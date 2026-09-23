using UnityEngine;
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
        Litter,
    }

    public class HLSpellEffect : MonoBehaviour
    {
        public HLSpellEffectKind kind;
        public Material material;
        public float lifetime = 0.65f;
        [FormerlySerializedAs("periodSeconds")]
        public float periodSeconds;
        public bool hasAuthoredSignature;
        public HLSpellSignature authoredSignature;
        public Color Tint { get; set; } = Color.white;

        void SetTint(Color color)
        {
            Tint = color;
            OnTint.Invoke(color);
        }

        void ApplySignatureColor(HLSpellSignature signature)
        {
            Color color =
                kind == HLSpellEffectKind.Shield ? new Color32(151, 203, 99, 255)
                : signature.sign == HLSign.Negative ? new Color32(242, 96, 122, 255)
                : signature.operation == HLOperation.Resource
                && signature.sign == HLSign.Positive
                && signature.attribute == AttributeType.HealthMax
                    ? new Color32(198, 242, 74, 255)
                : new Color32(242, 194, 48, 255);
            _baseColor = color;
            MaterialPropertyBlock block = PropertyBlock;
            block.SetColor(BaseColorId, color);
            foreach (Transform part in parts)
            {
                if (part)
                {
                    part.GetComponent<Renderer>().SetPropertyBlock(block);
                }
            }
        }

        public UnityEngine.Events.UnityEvent<Color> OnTint = new UnityEngine.Events.UnityEvent<Color>();
        bool _removing;
        float _removalAge;
        Vector3 _linkStart,
            _linkEnd;

        public void BeginRemoval()
        {
            _removing = true;
            _removalAge = 0;
            SetTint(Color.white);
        }

        public bool RemovalComplete => _removing && _removalAge >= 0.25f;
        public int Stacks { get; set; }
        public float ElapsedSeconds { get; set; }
        public float DurationSeconds { get; set; }
        public HLClockKind Clock { get; set; }
        public HLSpellSignature Signature { get; set; }
        public Transform[] stalks = new Transform[0];

        // Optional explicit camera; otherwise the tagged main camera is read in LateUpdate.
        [FormerlySerializedAs("facingCamera")]
        public Transform facingCamera;
        [FormerlySerializedAs("contactThread")]
        public bool contactThread;
        Renderer[] _renderers;
        Color _baseColor;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        public Transform[] parts = new Transform[0];
        Vector3[] _positions,
            _scales;
        Quaternion[] _rotations;
        float _age;
        bool _ready,
            _hasSignature,
            _ownsPrimitives;
        Transform[] _stackBeads;
        Renderer _sideRim;
        MaterialPropertyBlock _propertyBlock;
        MaterialPropertyBlock PropertyBlock => _propertyBlock ?? (_propertyBlock = new MaterialPropertyBlock());
        Entity.EntityType _side;
        bool _hasSide,
            _hasShieldState;
        float? _shieldState;

        public void SetStatus(int stacks, float elapsed, float duration, HLClockKind clock, HLSpellSignature signature)
        {
            Initialize();
            bool signatureChanged = !_hasSignature || !Signature.Equals(signature);
            if (!_hasSignature)
            {
                if (!hasAuthoredSignature)
                {
                    HLSpellPrimitives.AddMarker(this, signature);
                }
                _hasSignature = true;
            }
            bool stacksChanged = _stackBeads == null || Stacks != Mathf.Max(0, stacks);
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
            Stacks = Mathf.Max(0, stacks);
            ElapsedSeconds = Mathf.Max(0, elapsed);
            DurationSeconds = duration;
            Clock = clock;
            Signature = signature;
            if (signatureChanged)
            {
                ApplySignatureColor(signature);
                _hasShieldState = false;
            }
            if (kind == HLSpellEffectKind.Drip && signature.sign != HLSign.Positive)
            {
                SetTint(new Color32(242, 96, 122, 255));
            }
            Advance(0);
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
            Color color =
                side == Entity.EntityType.Player ? new Color32(155, 210, 74, 255)
                : side == Entity.EntityType.Computer ? new Color32(58, 66, 87, 255)
                : new Color32(201, 196, 180, 255);
            MaterialPropertyBlock block = PropertyBlock;
            block.SetColor("_BaseColor", color);
            _sideRim.SetPropertyBlock(block);
        }

        void OnDisable()
        {
            if (Tint != Color.white)
            {
                SetTint(Color.white);
            }
        }

        void Start()
        {
            Initialize();
        }

        void OnDestroy()
        {
            ReleaseResources();
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

        void RetainPrimitives()
        {
            if (!_ownsPrimitives)
            {
                HLSpellPrimitives.Retain();
                _ownsPrimitives = true;
            }
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
            Color color =
                kind == HLSpellEffectKind.Heal ? new Color32(198, 242, 74, 255)
                : (kind == HLSpellEffectKind.Impact || kind == HLSpellEffectKind.Drip) ? new Color32(242, 96, 122, 255)
                : new Color32(242, 194, 48, 255);
            if (kind == HLSpellEffectKind.Litter)
            {
                color = new Color32(58, 66, 87, 255);
            }
            if (kind == HLSpellEffectKind.Area)
            {
                color = Color.white;
            }
            _baseColor = color;
            MaterialPropertyBlock block = PropertyBlock;
            block.SetColor(BaseColorId, color);
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
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i]
                    .gameObject.SetActive(
                        Signature.operation != HLOperation.Attribute
                            || Signature.attribute != AttributeType.HitArmor
                            || !observedCharges.HasValue
                            || i < Mathf.Clamp(Mathf.CeilToInt(observedCharges.Value), 0, 6)
                    );
            }
        }

        public void FaceCamera(Transform cameraTransform)
        {
            if (kind == HLSpellEffectKind.Impact && cameraTransform && parts.Length > 0)
            {
                parts[0].rotation = cameraTransform.rotation;
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

        void Update()
        {
            Advance(Clock == HLClockKind.Realtime ? Time.unscaledDeltaTime : Time.deltaTime);
            if (RemovalComplete || (!_hasSignature && _age >= lifetime))
            {
                Destroy(gameObject);
            }
        }

        public void Advance(float delta)
        {
            Initialize();
            if (!HLSpellGrammar.Finite(delta) || delta < 0)
            {
                return;
            }
            _age += delta;
            if (_removing)
            {
                _removalAge += delta;
            }
            float statusTime = ElapsedSeconds;
            if (HLSpellGrammar.Finite(DurationSeconds) && DurationSeconds > 0)
            {
                statusTime = Mathf.Min(statusTime, DurationSeconds);
            }
            if (kind == HLSpellEffectKind.Chain)
            {
                SetEndpoints(_linkStart, _linkEnd);
            }
            float fade = Mathf.Clamp01(1 - _age / Mathf.Max(0.01f, lifetime));
            for (int i = 0; i < parts.Length; i++)
            {
                if (!parts[i])
                {
                    continue;
                }
                if (kind == HLSpellEffectKind.Buff)
                {
                    // Rotate the tilted plane around the body; spinning a torus in its own plane is invisible.
                    parts[i].localRotation =
                        Quaternion.Euler(0, statusTime * (i % 2 == 0 ? 34 : -28), 0) * _rotations[i];
                    parts[i].localScale = _scales[i] * (1 + 0.035f * Mathf.Sin(statusTime * 2.4f + i));
                    Brighten(i, 1.12f + 0.12f * Mathf.Sin(statusTime * 2.4f + i));
                }
                else if (kind == HLSpellEffectKind.Shield)
                {
                    float closure = _removing ? 1 - Mathf.Clamp01(_removalAge * 4) : Mathf.Clamp01(statusTime * 4);
                    float closedAngle =
                        Signature.operation == HLOperation.Attribute
                        && Signature.attribute == AttributeType.PercentArmor
                            ? -16
                            : 0;
                    parts[i].localRotation =
                        _rotations[i] * Quaternion.Euler(0, 0, Mathf.Lerp(-32, closedAngle, closure));
                    parts[i].localPosition = _positions[i] * Mathf.Lerp(1.3f, 1, closure);
                }
                else if (kind == HLSpellEffectKind.Drip)
                {
                    bool ticking =
                        HLSpellGrammar.Finite(periodSeconds) && periodSeconds > 0 && statusTime >= periodSeconds;
                    float phase = ticking ? Mathf.Repeat(statusTime, periodSeconds) / periodSeconds : 0;
                    bool rising = Signature.sign == HLSign.Positive;
                    parts[i].localPosition =
                        _positions[i] + (rising ? Vector3.up : Vector3.down) * phase * phase * 0.6f;
                    parts[i].localScale = _scales[i] * (ticking ? Mathf.Clamp01((1 - phase) * 4) : 0);
                }
                else if (kind == HLSpellEffectKind.Area)
                {
                    parts[i].localScale = _scales[i] * Mathf.Clamp01(_age / 0.3f);
                }
                else if (kind == HLSpellEffectKind.Litter)
                {
                    float emergence = Mathf.SmoothStep(0, 1, Mathf.Clamp01(_age / 0.15f));
                    float sink = Mathf.SmoothStep(0, 1, Mathf.Clamp01((_age - lifetime * 0.55f) / (lifetime * 0.45f)));
                    parts[i].localPosition = _positions[i] + Vector3.down * (0.22f * (1 - emergence + sink));
                    parts[i].localScale = _scales[i] * emergence;
                }
                else if (kind == HLSpellEffectKind.Heal)
                {
                    bool periodic =
                        _hasSignature
                        && Signature.tempo == HLTempo.HandlerTick
                        && HLSpellGrammar.Finite(periodSeconds)
                        && periodSeconds > 0;
                    float time = periodic
                        ? Mathf.Repeat(statusTime, periodSeconds) / periodSeconds
                        : _age / Mathf.Max(0.01f, lifetime);
                    float phase = _hasSignature && !periodic ? 0 : Mathf.Clamp01(time * (1 + i * 0.025f));
                    float bud =
                        _hasSignature && !periodic
                            ? 1
                            : Mathf.SmoothStep(0.2f, 1, Mathf.Clamp01(phase / 0.6f))
                                * (1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01((phase - 0.78f) / 0.16f)));
                    if (periodic && statusTime < periodSeconds)
                    {
                        bud = 0;
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
                    parts[i].localPosition =
                        _positions[i] + Vector3.up * ((_hasSignature ? 0 : _age) * (0.55f + i * 0.04f));
                }
                else if (kind == HLSpellEffectKind.Impact)
                {
                    float time = _hasSignature ? 0 : _age;
                    if (i > 0)
                    {
                        Vector3 velocity = new Vector3(_positions[i].x * 4, 1.1f + i * 0.12f, _positions[i].z * 4);
                        parts[i].localPosition = _positions[i] + velocity * time + Vector3.down * (2.8f * time * time);
                        parts[i].localRotation = _rotations[i] * Quaternion.Euler(time * 180, time * 70, 0);
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

        void Brighten(int index, float brightness)
        {
            Color color = _baseColor * brightness;
            color.a = 1;
            MaterialPropertyBlock block = PropertyBlock;
            block.SetColor(BaseColorId, color);
            _renderers[index].SetPropertyBlock(block);
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
            for (int i = 0; i < parts.Length; i++)
            {
                float t = (i / 2) / 16f;
                Vector3 p = LinkPoint(start, end, t);
                if (i % 2 == 0)
                {
                    Vector3 q = LinkPoint(start, end, Mathf.Min(1, t + 1 / 16f));
                    parts[i].position = (p + q) * 0.5f;
                    parts[i].rotation = q == p ? Quaternion.identity : Quaternion.FromToRotation(Vector3.up, q - p);
                    parts[i].localScale = new Vector3(
                        contactThread ? 0.012f : 0.025f,
                        Vector3.Distance(p, q) * 0.5f,
                        contactThread ? 0.012f : 0.025f
                    );
                }
                else
                {
                    parts[i].gameObject.SetActive(!contactThread);
                    parts[i].position = Curve(start, end, Mathf.Repeat(t + _age / 0.6f, 1));
                }
                Brighten(i, 1.15f + 0.25f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(_age / Mathf.Max(0.01f, lifetime))));
            }
        }

        Vector3 LinkPoint(Vector3 a, Vector3 b, float t)
        {
            if (!contactThread)
            {
                return Curve(a, b, t);
            }
            Vector3 side = Vector3.Cross((b - a).normalized, Vector3.up);
            return Vector3.Lerp(a, b, t) + side * (Mathf.Sin(t * Mathf.PI * 8) * Mathf.Sin(t * Mathf.PI) * 0.035f);
        }

        static Vector3 Curve(Vector3 a, Vector3 b, float t)
        {
            return Vector3.Lerp(a, b, t)
                + Vector3.up * (4 * t * (1 - t) * Mathf.Min(0.7f, Vector3.Distance(a, b) * 0.2f));
        }
    }
}
