using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // The body a recipe builds: its parts, roots, idle, wilt and charge, and the anchors effects sit on. The arms
    // its gestures lend out live in an ArmPool that reads the sockets and the root from here.
    public class CreatureRig : IDisposable
    {
        // How dark a tip gets on a dying unit, its hue kept
        public static readonly float WiltedTipValue = 0.45f;
        // How far a crown turns each second
        public static readonly float CrownSpinDegrees = 18f;
        // A hit pulses every part this much larger, and a charged head or tip swells this much more
        static readonly float hitSwell = 0.06f;
        static readonly float chargeSwell = 0.24f;
        // A dying unit leans this many degrees and sinks this many cells
        static readonly float wiltLean = 32f;
        static readonly float wiltSink = 0.08f;

        readonly PartPaint _paint = new PartPaint();
        readonly RootChain _roots = new RootChain();
        ShapeMeshCache _shapeMeshes = new ShapeMeshCache();
        CreatureRecipe _recipe;
        float _cellSize;
        Transform _root;
        Transform _sway;
        Transform[] _pivots;
        readonly List<Transform> _keptPivots = new List<Transform>();
        readonly List<Transform> _keptGeometry = new List<Transform>();
        Transform[] _geometry = Array.Empty<Transform>();
        float[] _appearanceDelays = Array.Empty<float>();
        Vector3[] _growthAnchors = Array.Empty<Vector3>();
        float _appearanceElapsed = CreatureAppearance.Duration;
        public float appearanceElapsed { get { return _appearanceElapsed; } }
        public bool isAppearing { get { return _appearanceElapsed < CreatureAppearance.Duration; } }
        Renderer[] _bodyRenderers;
        bool[] _hasOchreFaces;
        Color[] _colours;
        IdleDefinition _idle;
        float _crownPulse;
        float _hitPulse;
        Vector3? _aimTarget;
        float _budPower;
        bool _isDisposed;
        float _charge;
        float _healthFraction = 1f;
        Quaternion _aim = Quaternion.identity;
        Vector3? _presentationForward;
        Quaternion _presentationBasis = Quaternion.identity;
        float _presentationTurn;

        Transform[] _budAnchors;
        public Transform[] budAnchors { get { return _budAnchors; } }

        public Transform root { get { return _root; } }

        public CreatureRecipe recipe { get { return _recipe; } }
        int _revision;
        public int revision { get { return _revision; } }

        public float cellSize { get { return _cellSize; } }

        // One per recipe part, the transform carrying that part's mesh
        public IReadOnlyList<Transform> partTransforms { get { return _geometry; } }

        // The solid body's meshes: every root segment and joint first, the feet the grass feels most, then every
        // part. Arms, deliveries and effects are left out; they are drawn as chains or float around the body.
        public void CollectBodyMeshes(List<MeshFilter> into)
        {
            _roots.CollectMeshes(into);
            foreach (Transform part in _geometry)
            {
                MeshFilter filter = part ? part.GetComponent<MeshFilter>() : null;
                if (filter != null)
                {
                    into.Add(filter);
                }
            }
        }

        // The rotation the arms rest in: the root turned by the unit's aim, idle and wilt
        public Quaternion armRotation { get { return _root.rotation * _sway.localRotation; } }

        public bool Init(CreatureRecipe data, Transform parent, Material material, PrimitiveMeshes meshes,
            float cellSize = 1f)
        {
            return Init(data, parent, material, material, meshes, cellSize);
        }

        // A recipe that fails validation logs and leaves the view empty. Body, Head and Stem surfaces use the
        // body material; tips and roots retain the shared material. The caller gives stones one material
        // for both slots, so surface shading does not infer a side from colour, geometry or an object name.
        public bool Init(CreatureRecipe data, Transform parent, Material material, Material bodyMaterial,
            PrimitiveMeshes meshes, float cellSize)
        {
            if (!CreatureValidator.TryValidate(data, out string error))
            {
                Debug.LogError($"[CreatureRig] {error}");
                return false;
            }

            if (!parent || !material || !meshes || !RenderMath.IsPositive(cellSize))
            {
                Debug.LogError("[CreatureRig] Needs a parent, a material, the meshes and a positive cell size.");
                return false;
            }

            Vector3 scale = parent.lossyScale;
            if (scale.x <= 0f || Mathf.Abs(scale.x - scale.y) > 0.0001f || Mathf.Abs(scale.x - scale.z) > 0.0001f)
            {
                Debug.LogError("[CreatureRig] Creature rig ancestors must have positive uniform scale.");
                return false;
            }

            _cellSize = cellSize;
            _root = new GameObject("GeneratedCreature").transform;
            _root.SetParent(parent, false);
            _root.localScale = Vector3.one / parent.lossyScale.x;
            _sway = new GameObject("Sway").transform;
            _sway.SetParent(_root, false);
            return Recompose(data, material, bodyMaterial, meshes);
        }

        // Keep the live root and pivots: held effects and projectiles can still resolve their source while the
        // vocabulary changes. Spare pivots are reused after a topology edit, bounded by the validated part cap.
        public bool Recompose(CreatureRecipe data, Material material, Material bodyMaterial, PrimitiveMeshes meshes)
        {
            if (_isDisposed || !_root || !material || !meshes
                || !CreatureValidator.TryValidate(data, out _))
            {
                return false;
            }
            if (bodyMaterial == null)
            {
                bodyMaterial = material;
            }

            // Resolve every mesh before replacing the live assembly. A rejected profile leaves the old rig intact.
            ShapeMeshCache nextMeshes = new ShapeMeshCache();
            Mesh[] resolved = new Mesh[data.parts.Length];
            for (int i = 0; i < data.parts.Length; i++)
            {
                CreaturePart part = data.parts[i];
                resolved[i] = part.shape.isProcedural ? nextMeshes.Get(part.shape, part.variant)
                    : meshes.GetMesh(part.primitive, part.variant);
                if (part.shape.isProcedural && resolved[i] == null)
                {
                    nextMeshes.Dispose();
                    return false;
                }
            }
            if (!data.roots.segmentShape.IsValid() || !data.roots.jointShape.IsValid())
            {
                nextMeshes.Dispose();
                return false;
            }
            _recipe = data;
            _pivots = new Transform[data.parts.Length];
            _geometry = new Transform[data.parts.Length];
            _appearanceDelays = new float[data.parts.Length];
            _growthAnchors = new Vector3[data.parts.Length];
            _bodyRenderers = new Renderer[data.parts.Length];
            _hasOchreFaces = new bool[data.parts.Length];
            _colours = new Color[data.parts.Length];
            _idle = data.idle;
            _idle.seed ^= _root.parent.GetEntityId().GetHashCode();
            foreach (Transform pivot in _keptPivots)
            {
                pivot.SetParent(_sway, false);
                pivot.gameObject.SetActive(false);
            }
            List<Transform> buds = new List<Transform>();
            for (int i = 0; i < data.parts.Length; i++)
            {
                CreaturePart part = data.parts[i];
                // The accent stays the palette's own colour, only the body varies from unit to unit
                _colours[i] = part.role == PartRole.Tip ? part.colour : ColourJitter.Vary(part.colour, _idle.seed);
                if (i == _keptPivots.Count)
                {
                    _keptPivots.Add(new GameObject(part.id).transform);
                    _keptGeometry.Add(null);
                }
                _pivots[i] = _keptPivots[i];
                _pivots[i].name = part.id;
                _pivots[i].gameObject.SetActive(true);
                Transform pivotParent = _sway;
                if (part.parent >= 0)
                {
                    pivotParent = _pivots[part.parent];
                }
                _pivots[i].SetParent(pivotParent, false);
                _pivots[i].localPosition = part.localPosition * _cellSize;
                _pivots[i].localRotation = Quaternion.Euler(part.localEuler);
                Mesh mesh = resolved[i];
                Material partMaterial = material;
                if (part.role == PartRole.Body || part.role == PartRole.Head || part.role == PartRole.Stem)
                {
                    partMaterial = bodyMaterial;
                }
                if (!_keptGeometry[i])
                {
                    _keptGeometry[i] = PrimitiveMeshes.Geometry("Geometry", _pivots[i], mesh, partMaterial,
                        _colours[i], part.glow);
                }
                _geometry[i] = _keptGeometry[i];
                _geometry[i].gameObject.SetActive(true);
                _geometry[i].GetComponent<MeshFilter>().sharedMesh = mesh;
                _geometry[i].localPosition = Vector3.zero;
                _geometry[i].localScale = part.dimensions * _cellSize;
                _growthAnchors[i] = mesh ? new Vector3(mesh.bounds.center.x, mesh.bounds.min.y, mesh.bounds.center.z)
                    : Vector3.down * 0.5f;
                _bodyRenderers[i] = _geometry[i].GetComponent<Renderer>();
                _bodyRenderers[i].sharedMaterials = new Material[] { partMaterial };
                _hasOchreFaces[i] = mesh && mesh.subMeshCount > 1;
                if (_hasOchreFaces[i])
                {
                    _bodyRenderers[i].sharedMaterials = new Material[] { partMaterial, partMaterial };
                }

                Paint(i, _colours[i], part.glow);
                if (part.role == PartRole.Tip)
                {
                    buds.Add(_pivots[i]);
                }
            }

            // Measure the assembled pivots, not recipe array order, which puts some mineral supports last.
            float min = float.PositiveInfinity;
            float max = float.NegativeInfinity;
            for (int i = 0; i < _pivots.Length; i++)
            {
                float height = _sway.InverseTransformPoint(_pivots[i].position).y;
                _appearanceDelays[i] = height;
                min = Mathf.Min(min, height);
                max = Mathf.Max(max, height);
            }
            for (int i = 0; i < _pivots.Length; i++)
            {
                _appearanceDelays[i] = CreatureAppearance.PartDelay(data.parts[i].role,
                    Mathf.InverseLerp(min, max, _appearanceDelays[i]));
            }
            _budAnchors = buds.ToArray();
            _roots.Init(data.roots, _root, meshes, material, ColourJitter.Vary(data.roots.colour, _idle.seed));
            _shapeMeshes.Dispose();
            _shapeMeshes = nextMeshes;
            _revision++;
            return true;
        }

        public void SetReadout(Vector3? target, float health, float readiness, float glow)
        {
            _aimTarget = target.HasValue && RenderMath.IsFinite(target.Value) ? target : null;
            _healthFraction = float.IsFinite(health) ? Mathf.Clamp01(health) : 1f;
            _charge = float.IsFinite(readiness) ? Mathf.Clamp01(readiness) : 0f;
            _budPower = float.IsFinite(glow) ? Mathf.Clamp01(glow) : 0f;
        }

        // The live view supplies its assigned camera's horizontal forward. A null input keeps the deliberate
        // target/idle orientation used by Studio and standalone rigs; the rig never looks up a scene camera.
        public void SetPresentationForward(Vector3? forward)
        {
            if (_presentationForward.HasValue != forward.HasValue) _presentationTurn = 0f;
            _presentationForward = forward;
        }

        // Default rigs stay grown for Studio and existing callers. Only a new live view or placement opts in.
        public void BeginAppearance()
        {
            _appearanceElapsed = 0f;
        }

        public void AdvanceAppearance(float deltaTime)
        {
            if (float.IsFinite(deltaTime) && deltaTime > 0f)
                _appearanceElapsed = Mathf.Min(CreatureAppearance.Duration, _appearanceElapsed + deltaTime);
        }

        public void CompleteAppearance()
        {
            _appearanceElapsed = CreatureAppearance.Duration;
        }

        public void Hit()
        {
            _hitPulse = 1f;
        }

        // A heal pulses the crown
        public void Heal()
        {
            _crownPulse = 1f;
        }

        public void Tick(float time, float deltaTime, FootFrame frame)
        {
            if (!_root)
            {
                return;
            }

            _root.SetPositionAndRotation(frame.origin, Quaternion.FromToRotation(Vector3.up, frame.normal));
            _root.localScale = Vector3.one / _root.parent.lossyScale.x;
            float dt = Mathf.Max(0f, deltaTime);
            Aim(time, dt, frame.origin);

            _hitPulse = Mathf.Max(0f, _hitPulse - dt * 5f);
            IdlePose idlePose = IdleMotion.Evaluate(_idle, time);
            float shake = Mathf.Sin(_hitPulse * 24f) * _hitPulse * 9f;
            Quaternion wilt = Quaternion.Euler((1f - _healthFraction) * wiltLean, 0f, shake);
            _sway.localRotation = _aim * idlePose.sway * wilt;
            _sway.localPosition = Vector3.down * ((1f - _healthFraction) * wiltSink * _cellSize);
            _crownPulse = Mathf.Max(0f, _crownPulse - dt / 0.2f);
            float light = Mathf.Max(_budPower, _charge) * _healthFraction;
            for (int i = 0; i < _geometry.Length; i++)
            {
                CreaturePart part = _recipe.parts[i];
                bool isSwelling = part.role == PartRole.Head || part.role == PartRole.Tip;
                float swell = 1f + _crownPulse * hitSwell;
                if (isSwelling)
                {
                    swell += _charge * chargeSwell;
                }
                Vector3 posedScale = Vector3.Scale(part.dimensions, idlePose.bodyScale) * _cellSize * swell;
                float growth = CreatureAppearance.Scale(_appearanceElapsed, _appearanceDelays[i]);
                _geometry[i].localScale = posedScale * growth;
                _geometry[i].localPosition = Vector3.Scale(_growthAnchors[i], posedScale) * (1f - growth);
                if (part.role == PartRole.Crown)
                {
                    Quaternion spin = Quaternion.AngleAxis(time * CrownSpinDegrees, Vector3.up);
                    _pivots[i].localRotation = Quaternion.Euler(part.localEuler) * spin;
                }

                if (part.role == PartRole.Tip)
                {
                    // The accent keeps its hue: charge brightens it, the wilt darkens it
                    float value = Mathf.Lerp(WiltedTipValue, 1f, _healthFraction);
                    Color tip = _colours[i];
                    Paint(i, new Color(tip.r * value, tip.g * value, tip.b * value, tip.a), part.glow * light);
                    continue;
                }

                Color wiltColour = _recipe.wiltColour;
                wiltColour.a = _colours[i].a;
                Paint(i, Color.Lerp(wiltColour, _colours[i], _healthFraction), part.glow * light);
            }

            _roots.Place(_sway, _root, _cellSize, _appearanceElapsed);
        }

        void Aim(float time, float deltaTime, Vector3 origin)
        {
            float damping = 1f - Mathf.Exp(-deltaTime * 7f);
            if (_presentationForward.HasValue)
            {
                Vector3 forward = _presentationForward.Value;
                if (RenderMath.IsFinite(forward))
                {
                    forward = _root.InverseTransformDirection(forward);
                    forward.y = 0f;
                    if (forward.sqrMagnitude > 0.000001f) _presentationBasis = Quaternion.LookRotation(forward);
                }
                // Follow camera heading immediately, including the initial paused frame. Only the small
                // reaction to a target is damped, so a moving camera cannot leave the full silhouette edge-on.
                float turn = Mathf.Sin(time * 0.3f) * 4f;
                if (_aimTarget.HasValue)
                {
                    Vector3 target = _root.InverseTransformDirection(_aimTarget.Value - origin);
                    target.y = 0f;
                    turn = target.sqrMagnitude > 0.000001f
                        ? Vector3.Dot(target.normalized, _presentationBasis * Vector3.right) * 18f : 0f;
                }
                // Lateral response is continuous even when the target crosses directly behind the creature.
                _presentationTurn = Mathf.Lerp(_presentationTurn, turn, damping);
                _aim = _presentationBasis * Quaternion.AngleAxis(_presentationTurn, Vector3.up);
                return;
            }

            Vector3 direction = new Vector3(Mathf.Sin(time * 0.3f) * 0.4f, 0f, 1f);
            if (_aimTarget.HasValue) direction = _root.InverseTransformDirection(_aimTarget.Value - origin);
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.000001f)
            {
                _aim = Quaternion.Slerp(_aim, Quaternion.LookRotation(direction), damping);
            }
        }

        // Where arm index leaves the body now, in world space
        public Vector3 ArmSocket(int index)
        {
            if (_recipe.arms.Length == 0)
            {
                return _sway.TransformPoint(_recipe.neckLocal * _cellSize);
            }
            index %= _recipe.arms.Length;
            ArmDefinition definition = _recipe.arms[index];
            return _pivots[definition.bodyPart].TransformPoint(definition.rootLocal * _cellSize);
        }

        // Arm index as the recipe authors it, its colour varied like the rest of the unit
        public ArmDefinition GetArm(int index)
        {
            ArmDefinition definition = _recipe.arms[index];
            definition.colour = ColourJitter.Vary(definition.colour, _idle.seed);
            return definition;
        }

        // The body, neck, head and foot as they stand now, read from the recipe's roles and the live transforms
        public bool TryGetAnchors(out EffectAnchors anchors)
        {
            if (_pivots == null || _pivots.Length == 0)
            {
                anchors = new EffectAnchors();
                return false;
            }
            return PartAnchors.TryMeasure(_recipe, _root, _pivots[0], _bodyRenderers, _cellSize, out anchors);
        }

        public void SetVisible(bool visible)
        {
            if (_root)
            {
                _root.gameObject.SetActive(visible);
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _roots.Clear();
            _shapeMeshes.Dispose();
            if (!_root)
            {
                return;
            }

            _root.gameObject.SetActive(false);
            RenderObjects.Release(_root.gameObject);
        }

        // A stone's ochre faces fade to the wilt colour with the rest of the body
        void Paint(int index, Color colour, float glow)
        {
            bool isTip = _recipe.parts[index].role == PartRole.Tip;
            if (!_hasOchreFaces[index])
            {
                _paint.Paint(_bodyRenderers[index], isTip, colour, glow);
                return;
            }

            Color ochre = Color.Lerp(_recipe.wiltColour, _recipe.stoneOchre, _healthFraction);
            _paint.Paint(_bodyRenderers[index], isTip, colour, ochre, glow);
        }
    }
}
