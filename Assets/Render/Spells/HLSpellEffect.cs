using UnityEngine;

namespace HealerLike.Render.Spells
{
    public enum HLSpellEffectKind { Buff, Shield, Heal, Impact, Chain, Mana, Drip, Area }
    /// <summary>Cosmetic animation only. Status timing is supplied by the observer, never used to execute ticks.</summary>
    public sealed class HLSpellEffect : MonoBehaviour
    {
        public HLSpellEffectKind kind;
        public Material material;
        public float lifetime = .65f;
        public float PeriodSeconds;
        public bool hasAuthoredSignature;
        public HLSpellSignature authoredSignature;
        public Color Tint { get; private set; } = Color.white;
        void SetTint(Color color) { Tint = color; OnTint.Invoke(color); }
        void ApplySignatureColor(HLSpellSignature signature)
        {
            Color color = signature.sign == HLSign.Negative ? new Color32(242,96,122,255) :
                signature.operation == HLOperation.Resource && signature.sign == HLSign.Positive && signature.attribute == AttributeType.HealthMax ? new Color32(198,242,74,255) : new Color32(242,194,48,255);
            var block = PropertyBlock; block.SetColor("_BaseColor",color);
            foreach(var part in parts) if(part) part.GetComponent<Renderer>().SetPropertyBlock(block);
        }
        public UnityEngine.Events.UnityEvent<Color> OnTint = new UnityEngine.Events.UnityEvent<Color>();
        bool _removing;
        float _removalAge;
        Vector3 _linkStart, _linkEnd;
        public void BeginRemoval() { _removing = true; _removalAge = 0; SetTint(Color.white); }
        public bool RemovalComplete => _removing && _removalAge >= .25f;
        public int Stacks { get; private set; }
        public float ElapsedSeconds { get; private set; }
        public float DurationSeconds { get; private set; }
        public HLClockKind Clock { get; private set; }
        public HLSpellSignature Signature { get; private set; }
        public Transform[] parts = new Transform[0];
        Vector3[] _positions, _scales;
        Quaternion[] _rotations;
        float _age;
        bool _ready, _hasSignature, _ownsPrimitives;
        Transform[] _stackBeads;
        Renderer _sideRim;
        MaterialPropertyBlock _propertyBlock;
        MaterialPropertyBlock PropertyBlock => _propertyBlock ?? (_propertyBlock = new MaterialPropertyBlock());
        Entity.EntityType _side;
        bool _hasSide, _hasShieldState;
        float? _shieldState;
        public void SetStatus(int stacks, float elapsed, float duration, HLClockKind clock, HLSpellSignature signature)
        {
            Initialize();
            bool signatureChanged = !_hasSignature || !Signature.Equals(signature);
            if (!_hasSignature) { if (!hasAuthoredSignature) HLSpellPrimitives.AddMarker(this, signature); _hasSignature = true; }
            bool stacksChanged = _stackBeads == null || Stacks != Mathf.Max(0,stacks);
            if (_stackBeads == null) _stackBeads = HLSpellPrimitives.StackBeads(this);
            if (stacksChanged) for (int i = 0; i < _stackBeads.Length; i++) _stackBeads[i].gameObject.SetActive(i < Mathf.Min(8, stacks));
            Stacks = Mathf.Max(0, stacks); ElapsedSeconds = Mathf.Max(0, elapsed);
            DurationSeconds = duration; Clock = clock; Signature = signature;
            if (signatureChanged) { ApplySignatureColor(signature); _hasShieldState = false; }
            if (kind == HLSpellEffectKind.Drip && signature.sign != HLSign.Positive) SetTint(new Color32(242,96,122,255));
            Advance(0);
        }
        public void SetSide(Entity.EntityType side)
        {
            if (_hasSide && _side == side && _sideRim) return;
            _hasSide = true; _side = side;
            if (!_sideRim) _sideRim = HLSpellPrimitives.SideRim(this);
            var color = side == Entity.EntityType.Player ? new Color32(155,210,74,255) : side == Entity.EntityType.Computer ? new Color32(58,66,87,255) : new Color32(201,196,180,255);
            var block = PropertyBlock; block.SetColor("_BaseColor", color); _sideRim.SetPropertyBlock(block);
        }
        void OnDisable() { if (Tint != Color.white) SetTint(Color.white); }
        void Start() => Initialize();
        void OnDestroy() => ReleaseResources();
        // Explicit teardown also supports editor authoring, where runtime messages do not run.
        public void ReleaseResources()
        {
            if (_ownsPrimitives) { _ownsPrimitives = false; HLSpellPrimitives.ReleaseUser(); }
        }
        internal void RetainPrimitives()
        {
            if (!_ownsPrimitives) { HLSpellPrimitives.Retain(); _ownsPrimitives = true; }
        }
        public void Initialize()
        {
            if (_ready) return;
            RetainPrimitives();
            if (parts == null || parts.Length == 0) HLSpellPrimitives.Build(this);
            _positions = new Vector3[parts.Length]; _scales = new Vector3[parts.Length]; _rotations = new Quaternion[parts.Length];
            for (int i = 0; i < parts.Length; i++) { _positions[i] = parts[i].localPosition; _scales[i] = parts[i].localScale; _rotations[i] = parts[i].localRotation; }
            Color color = kind == HLSpellEffectKind.Heal ? new Color32(198,242,74,255) : (kind == HLSpellEffectKind.Impact || kind == HLSpellEffectKind.Drip) ? new Color32(242,96,122,255) : new Color32(242,194,48,255);
            var block = PropertyBlock; block.SetColor("_BaseColor", color);
            foreach (var renderer in GetComponentsInChildren<Renderer>()) { if (material) renderer.sharedMaterial = material; renderer.SetPropertyBlock(block); }
            if (hasAuthoredSignature) ApplySignatureColor(authoredSignature);
            _ready = true;
        }
        public void SetShieldState(float? observedCharges)
        {
            if (kind != HLSpellEffectKind.Shield) return;
            if (_hasShieldState && _shieldState == observedCharges) return;
            _hasShieldState = true; _shieldState = observedCharges;
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i].gameObject.SetActive(Signature.operation != HLOperation.Attribute || Signature.attribute != AttributeType.HitArmor || !observedCharges.HasValue || i < Mathf.Clamp(Mathf.CeilToInt(observedCharges.Value), 0, 6));
            }
        }
        void Update()
        {
            Advance(Clock == HLClockKind.Realtime ? Time.unscaledDeltaTime : Time.deltaTime);
            if (RemovalComplete || (!_hasSignature && _age >= lifetime)) Destroy(gameObject);
        }
        public void Advance(float delta)
        {
            Initialize();
            if (!HLSpellGrammar.Finite(delta) || delta < 0) return;
            _age += delta;
            if (_removing) _removalAge += delta;
            float statusTime = ElapsedSeconds;
            if (HLSpellGrammar.Finite(DurationSeconds) && DurationSeconds > 0) statusTime = Mathf.Min(statusTime, DurationSeconds);
            if (kind == HLSpellEffectKind.Chain) SetEndpoints(_linkStart, _linkEnd);
            float fade = Mathf.Clamp01(1 - _age / Mathf.Max(.01f, lifetime));
            for (int i = 0; i < parts.Length; i++)
            {
                if (!parts[i]) continue;
                if (kind == HLSpellEffectKind.Buff)
                    parts[i].localRotation = _rotations[i] * Quaternion.Euler(0, statusTime * (i % 2 == 0 ? 34 : -28), 0);
                else if (kind == HLSpellEffectKind.Shield)
                    parts[i].localRotation = _rotations[i] * Quaternion.Euler(Mathf.Lerp(-32, Signature.operation == HLOperation.Attribute && Signature.attribute == AttributeType.PercentArmor ? -16 : 0, (_removing ? 1 - Mathf.Clamp01(_removalAge * 4) : Mathf.Clamp01(statusTime * 4))), 0, 0);
                else if (kind == HLSpellEffectKind.Drip)
                {
                    float phase = PeriodSeconds > 0 ? Mathf.Repeat(statusTime, PeriodSeconds) / PeriodSeconds : 0;
                    parts[i].localPosition = _positions[i] + Vector3.down * phase * .35f;
                    parts[i].localScale = _scales[i] * (statusTime >= PeriodSeconds && PeriodSeconds > 0 ? 1 - phase : .25f);
                }
                else if (kind == HLSpellEffectKind.Area)
                    parts[i].localScale = _scales[i] * Mathf.Clamp01(_age / .3f);
                else if (kind == HLSpellEffectKind.Heal || kind == HLSpellEffectKind.Mana)
                    parts[i].localPosition = _positions[i] + Vector3.up * ((_hasSignature ? 0 : _age) * (.55f + i * .04f));
                else if (kind == HLSpellEffectKind.Impact)
                    parts[i].localPosition = _positions[i] + _positions[i].normalized * (_hasSignature ? 0 : _age) * .9f;
                if (!_hasSignature && kind != HLSpellEffectKind.Chain && kind != HLSpellEffectKind.Area)
                    parts[i].localScale = _scales[i] * Mathf.Sqrt(fade);
            }
        }
        /// <summary>Endpoints supplied by a confirmed link; no target queries or inferred bounce order.</summary>
        public void SetEndpoints(Vector3 start, Vector3 end)
        {
            Initialize();
            if (kind != HLSpellEffectKind.Chain) return;
            _linkStart = start; _linkEnd = end;
            for (int i = 0; i < parts.Length; i++)
            {
                float t = (i / 2) / 16f;
                Vector3 p = Curve(start, end, t);
                if (i % 2 == 0)
                {
                    Vector3 q = Curve(start, end, Mathf.Min(1, t + 1 / 16f));
                    parts[i].position = (p + q) * .5f;
                    parts[i].rotation = q == p ? Quaternion.identity : Quaternion.FromToRotation(Vector3.up, q - p);
                    parts[i].localScale = new Vector3(.025f, Vector3.Distance(p, q) * .5f, .025f);
                }
                else parts[i].position = Curve(start, end, Mathf.Repeat(t + _age / .6f, 1));
            }
        }
        static Vector3 Curve(Vector3 a, Vector3 b, float t) => Vector3.Lerp(a, b, t) + Vector3.up * (4 * t * (1-t) * Mathf.Min(.7f, Vector3.Distance(a,b) * .2f));
    }
}
