using System;
using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Deliveries
{
    // Stored by value in assets: append new members, never reorder or remove
    public enum GestureKind
    {
        Attack,
        Heal,
        Channel
    }

    public enum GesturePhase
    {
        Rest,
        Extend,
        Contact,
        Hold,
        Retract
    }

    // A render chain owned by its source. Gestures change goals, never link lengths or gameplay.
    public class LianaArm : IDisposable
    {
        static readonly int sides = 6;
        // Leaves along the chain, and as many beads plus the tip
        public static readonly int LeafCount = 5;
        // A direction shorter than this has none
        static readonly float zeroLengthSquared = 0.000000000001f;
        // Gesture timings in seconds: the reach out, the pause at contact, the way back, and a rod's snap back
        static readonly float extendSeconds = 0.1f;
        static readonly float contactSeconds = 0.04f;
        static readonly float retractSeconds = 0.2f;
        // An arc delivery lifts its middle by this much per cell of reach, never more than the cap
        static readonly float arcLiftPerCell = 0.4f;
        static readonly float arcMaxLift = 2f;
        // Proportions in arm radii: the tip unit, the leaves and the beads
        static readonly float tipWidthRadii = 4.5f;
        static readonly float leafLengthRadii = 9f;
        static readonly float beadRadii = 2.4f;
        // The chain thins to this share of its radius at the tip, narrow collars between broader internodes
        static readonly float tipTaper = 0.65f;
        static readonly float collarWidth = 0.76f;
        static readonly float internodeWidth = 1.12f;
        // A leaf's width and depth for its length, its lean along the chain and where its centre sits along it
        static readonly float leafWidth = 0.38f;
        static readonly float leafDepth = 0.22f;
        static readonly float leafLean = 0.45f;
        static readonly float leafCentre = 0.45f;

        readonly ChainSolver _solver = new ChainSolver();
        readonly Matrix4x4[] _leaves = new Matrix4x4[LeafCount];
        readonly Matrix4x4[] _beads = new Matrix4x4[LeafCount + 1];
        readonly DeliveryTip _tip = new DeliveryTip();
        Vector3[] _rest;
        Vector3[] _joints;
        float[] _lengths;
        Transform _container;
        Mesh _mesh;
        MeshRenderer _renderer;
        Vector3[] _vertices;
        Vector3[] _normals;
        Mesh _leafMesh;
        Mesh _beadMesh;
        PrimitiveMeshes _meshes;
        Material _detailMaterial;
        MaterialPropertyBlock _detailColour;
        float _radius;
        Vector3 _pole;
        bool _isDisposed;
        bool _hasLoggedSolveError;
        bool _isVisible = true;
        float _elapsed;
        Vector3 _startGoal;
        Vector3 _returnGoal;
        bool _isEndPending;
        GestureKind _kind;
        Color _colour;
        Color _restTipColour;

        int _token;
        public int token { get { return _token; } }

        Vector3 _goal;
        public Vector3 goal { get { return _goal; } }

        DeliveryStyle _style;
        public DeliveryStyle style { get { return _style; } set { _style = value; } }

        // The tip and the arm look of each style, a bending arm with a bead tip without it
        DeliveryVocabulary _vocabulary;

        Color _tipColour;
        public Color tipColour { get { return _tipColour; } }

        public Color restTipColour { get { return _restTipColour; } }

        public DeliveryTip tipFragment { get { return _tip; } }

        // A delivery draws a rod or an arc to its projectile instead of solving a reaching chain
        bool _isDeliveryProfile;
        public bool isDeliveryProfile { get { return _isDeliveryProfile; } set { _isDeliveryProfile = value; } }

        GesturePhase _phase;
        public GesturePhase phase { get { return _phase; } }

        ChainResult _lastResult;
        public ChainResult lastResult { get { return _lastResult; } }

        int _meshRevision;
        public int meshRevision { get { return _meshRevision; } }

        public int activeLeafCount { get { return _isVisible && _phase != GesturePhase.Rest ? LeafCount : 0; } }

        public Matrix4x4 tipMatrix { get { return _beads[LeafCount]; } }

        // The width of one tip unit in world space
        public float tipWidth { get { return _radius * tipWidthRadii; } }

        public Vector3 tip { get { return _joints[_joints.Length - 1]; } }

        public int segmentCount { get { return _lengths.Length; } }

        public bool isAvailable { get { return phase == GesturePhase.Rest; } }

        // Without a parent the arm only solves its chain and draws nothing
        public bool Init(ArmDefinition definition, Transform parent, Material material, PrimitiveMeshes meshes,
            DeliveryVocabulary vocabulary, float cellSize = 1f)
        {
            if (definition.restJoints == null || definition.restJoints.Length != definition.segmentCount + 1
                || definition.segmentCount < 2 || !RenderMath.IsPositive(cellSize))
            {
                Debug.LogError("[LianaArm] Invalid arm definition.");
                return false;
            }

            if (parent && !meshes)
            {
                Debug.LogError("[LianaArm] A drawn arm needs the primitive meshes.");
                return false;
            }

            _vocabulary = vocabulary;
            _rest = new Vector3[definition.restJoints.Length];
            _joints = new Vector3[_rest.Length];
            _lengths = new float[definition.segmentCount];
            for (int i = 0; i < _rest.Length; i++)
            {
                _rest[i] = definition.restJoints[i] * cellSize;
            }

            for (int i = 0; i < _lengths.Length; i++)
            {
                _lengths[i] = definition.segmentLength * cellSize;
            }

            _radius = definition.radius * cellSize;
            _pole = definition.bendPole;
            _colour = definition.colour;
            _restTipColour = definition.tipColour.a > 0f ? definition.tipColour : definition.colour;
            _tipColour = _restTipColour;
            if (!parent)
            {
                return true;
            }

            _meshes = meshes;
            _leafMesh = meshes.cone;
            _beadMesh = meshes.sphere;
            _detailMaterial = material;
            _detailColour = new MaterialPropertyBlock();
            Vector4[] detailColours = new Vector4[LeafCount + 1];
            for (int i = 0; i < detailColours.Length; i++)
            {
                detailColours[i] = definition.colour;
            }

            _detailColour.SetVectorArray(RenderObjects.BaseColorId, detailColours);
            _container = new GameObject("LianaArm").transform;
            _container.SetParent(parent, false);
            _mesh = new Mesh { name = "LianaChain", hideFlags = HideFlags.DontSave };
            _mesh.MarkDynamic();
            _vertices = new Vector3[_joints.Length * sides + 2];
            _normals = new Vector3[_vertices.Length];
            int[] triangles = new int[_lengths.Length * sides * 6 + sides * 6];
            int index = 0;
            for (int j = 0; j < _lengths.Length; j++)
            {
                for (int side = 0; side < sides; side++)
                {
                    int a = j * sides + side;
                    int next = j * sides + (side + 1) % sides;
                    int b = a + sides;
                    int nextB = next + sides;
                    triangles[index++] = a;
                    triangles[index++] = next;
                    triangles[index++] = b;
                    triangles[index++] = next;
                    triangles[index++] = nextB;
                    triangles[index++] = b;
                }
            }

            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                int end = _lengths.Length * sides;
                triangles[index++] = _vertices.Length - 2;
                triangles[index++] = next;
                triangles[index++] = side;
                triangles[index++] = _vertices.Length - 1;
                triangles[index++] = end + side;
                triangles[index++] = end + next;
            }

            // Every vertex carries a normal from creation: SelectableEntity adds QuickOutline, whose Awake
            // reads one normal per vertex of every child mesh, before the first gesture uploads real normals.
            _mesh.vertices = _vertices;
            _mesh.normals = _normals;
            _mesh.triangles = triangles;
            _container.gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = _container.gameObject.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = material;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor(RenderObjects.BaseColorId, definition.colour);
            _renderer.SetPropertyBlock(block);
            _renderer.enabled = false;
            return true;
        }

        public Matrix4x4 LeafMatrix(int index)
        {
            return _leaves[index];
        }

        public Vector3 Joint(int index)
        {
            return _joints[index];
        }

        // The tip takes the family of the live delivery until the arm is back at rest
        public void SetTipAccent(int gestureToken, Color colour)
        {
            if (_token != gestureToken || phase == GesturePhase.Rest)
            {
                return;
            }

            _tipColour = colour;
        }

        public void Begin(int gestureToken, GestureKind gestureKind, Vector3 worldTarget)
        {
            _tipColour = _restTipColour;
            _token = gestureToken;
            _kind = gestureKind;
            _goal = worldTarget;
            _startGoal = tip;
            _isEndPending = false;
            _phase = GesturePhase.Extend;
            _elapsed = 0f;
        }

        public void SetTipGoal(int gestureToken, Vector3 worldPosition)
        {
            if (_token != gestureToken || phase == GesturePhase.Rest || phase == GesturePhase.Retract)
            {
                return;
            }

            _goal = worldPosition;
            // Projectile travel already sets the extension timing
            if (phase != GesturePhase.Contact || style == DeliveryStyle.Bounce)
            {
                _phase = GesturePhase.Hold;
            }
        }

        public void Contact(int gestureToken, Vector3 worldPosition)
        {
            if (_token != gestureToken || phase == GesturePhase.Rest)
            {
                return;
            }

            _goal = worldPosition;
            _phase = GesturePhase.Contact;
            _elapsed = 0f;
        }

        public void End(int gestureToken)
        {
            if (_token != gestureToken || phase == GesturePhase.Rest || phase == GesturePhase.Retract)
            {
                return;
            }

            if (phase == GesturePhase.Contact && _elapsed < contactSeconds)
            {
                _isEndPending = true;
                return;
            }

            StartReturn();
        }

        public void Tick(float deltaTime, Vector3 rootWorld, Quaternion restOrientation)
        {
            float dt = Mathf.Max(0f, deltaTime);
            float retract = retractSeconds;
            ArmStyle arm = ArmStyleOf(style);
            if (_isDeliveryProfile && arm.retractSeconds > 0f)
            {
                retract = arm.retractSeconds;
            }

            Vector3 restTip = rootWorld + restOrientation * _rest[_rest.Length - 1];
            _elapsed += dt;
            if (phase == GesturePhase.Rest || (phase == GesturePhase.Retract && _elapsed >= retract))
            {
                _phase = GesturePhase.Rest;
                _tipColour = _restTipColour;
                for (int i = 0; i < _joints.Length; i++)
                {
                    _joints[i] = rootWorld + restOrientation * _rest[i];
                }
            }
            else
            {
                if (!RenderMath.IsFinite(rootWorld) || !RenderMath.IsFinite(_goal))
                {
                    HideForFrame();
                    return;
                }

                Vector3 target = _goal;
                if (phase == GesturePhase.Extend)
                {
                    target = Vector3.Lerp(_startGoal, _goal, Mathf.Clamp01(_elapsed / extendSeconds));
                    if (_elapsed >= extendSeconds)
                    {
                        _phase = GesturePhase.Hold;
                        _elapsed = 0f;
                    }
                }

                if (phase == GesturePhase.Retract)
                {
                    float blend = Mathf.Clamp01(_elapsed / retract);
                    target = Vector3.Lerp(_returnGoal, restTip, blend);
                    // Only an initial guess, FABRIK projects every link right below
                    for (int i = 0; i < _joints.Length; i++)
                    {
                        _joints[i] = Vector3.Lerp(_joints[i], rootWorld + restOrientation * _rest[i], blend);
                    }
                }

                bool isRod = arm.isRod;
                if (!_isDeliveryProfile)
                {
                    if (!_solver.Solve(_joints, _lengths, rootWorld, target, restOrientation * _pole,
                        out ChainResult result, 64))
                    {
                        HideForFrame();
                        return;
                    }

                    _lastResult = result;
                }
                else if (isRod)
                {
                    // Delivery rods telescope visually, the projectile endpoint stays authoritative
                    if (style == DeliveryStyle.Rigid && phase != GesturePhase.Retract)
                    {
                        target = _goal;
                    }

                    for (int i = 0; i < _joints.Length; i++)
                    {
                        _joints[i] = Vector3.Lerp(rootWorld, target, (float)i / (_joints.Length - 1));
                    }
                }
                else
                {
                    for (int i = 0; i < _joints.Length; i++)
                    {
                        float t = (float)i / (_joints.Length - 1);
                        // A parabola through both ends, highest halfway
                        float height = Mathf.Min(arcMaxLift, Vector3.Distance(rootWorld, target) * arcLiftPerCell);
                        float lift = 4f * t * (1f - t) * height;
                        _joints[i] = Vector3.Lerp(rootWorld, target, t) + Vector3.up * lift;
                    }
                }

                if (phase == GesturePhase.Contact && _elapsed >= contactSeconds)
                {
                    if (_isEndPending || _kind == GestureKind.Heal)
                    {
                        StartReturn();
                    }
                    else
                    {
                        _phase = GesturePhase.Hold;
                        _elapsed = 0f;
                    }
                }
            }

            Draw();
        }

        public void SetVisible(bool value)
        {
            _isVisible = value;
            if (_renderer && (!_isVisible || phase == GesturePhase.Rest))
            {
                _renderer.enabled = false;
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            if (!_container)
            {
                return;
            }

            // The chain mesh is rewritten every frame, so it belongs to this arm alone
            _container.gameObject.SetActive(false);
            RenderObjects.Release(_container.gameObject);
            RenderObjects.Release(_mesh);
        }

        // How the vocabulary draws the style, a bending arm when there is no vocabulary
        ArmStyle ArmStyleOf(DeliveryStyle deliveryStyle)
        {
            if (_vocabulary == null)
            {
                return DeliveryVocabulary.BendingArm;
            }
            return _vocabulary.GetArm(deliveryStyle);
        }

        void HideForFrame()
        {
            if (_renderer)
            {
                _renderer.enabled = false;
            }

            if (!_hasLoggedSolveError)
            {
                Debug.LogError("[LianaArm] The chain has no finite solution, it stays hidden this frame.");
                _hasLoggedSolveError = true;
            }
        }

        void StartReturn()
        {
            _returnGoal = tip;
            _phase = GesturePhase.Retract;
            _elapsed = 0f;
            _isEndPending = false;
        }

        void Draw()
        {
            if (!_container)
            {
                return;
            }

            _renderer.enabled = _isVisible && phase != GesturePhase.Rest;
            if (!_renderer.enabled)
            {
                return;
            }

            UpdateDetails();
            Matrix4x4 worldToLocal = _container.worldToLocalMatrix;
            float styleWidth = ArmStyleOf(style).width;
            for (int j = 0; j < _joints.Length; j++)
            {
                Vector3 tangent = _joints[Mathf.Min(j + 1, _joints.Length - 1)] - _joints[Mathf.Max(j - 1, 0)];
                if (tangent.sqrMagnitude < zeroLengthSquared)
                {
                    tangent = Vector3.up;
                }

                tangent.Normalize();
                Vector3 axis = Mathf.Abs(tangent.y) < 0.9f ? Vector3.up : Vector3.right;
                Vector3 u = Vector3.Cross(tangent, axis).normalized;
                Vector3 v = Vector3.Cross(tangent, u);
                float width = Mathf.Lerp(_radius, _radius * tipTaper, (float)j / _lengths.Length);
                width *= styleWidth;

                // Narrow collars between broader internodes read as a jointed plant arm at gameplay scale
                if (j % 3 == 0)
                {
                    width *= collarWidth;
                }
                else
                {
                    width *= internodeWidth;
                }
                for (int side = 0; side < sides; side++)
                {
                    float angle = side * Mathf.PI * 2f / sides;
                    Vector3 normal = u * Mathf.Cos(angle) + v * Mathf.Sin(angle);
                    int index = j * sides + side;
                    _vertices[index] = worldToLocal.MultiplyPoint3x4(_joints[j] + normal * width);
                    _normals[index] = worldToLocal.MultiplyVector(normal);
                }
            }

            _vertices[_vertices.Length - 2] = worldToLocal.MultiplyPoint3x4(_joints[0]);
            _vertices[_vertices.Length - 1] = worldToLocal.MultiplyPoint3x4(tip);
            _normals[_normals.Length - 2] = worldToLocal.MultiplyVector((_joints[0] - _joints[1]).normalized);
            Vector3 tipDirection = (tip - _joints[_joints.Length - 2]).normalized;
            _normals[_normals.Length - 1] = worldToLocal.MultiplyVector(tipDirection);
            _mesh.vertices = _vertices;
            _mesh.normals = _normals;
            _mesh.RecalculateBounds();
            _meshRevision++;
        }

        void UpdateDetails()
        {
            float width = ArmStyleOf(style).leafWidth;
            for (int i = 0; i < LeafCount; i++)
            {
                int j = Mathf.Clamp((i + 1) * _lengths.Length / (LeafCount + 1), 1, _lengths.Length - 1);
                Vector3 tangent = (_joints[j + 1] - _joints[j - 1]).normalized;
                if (tangent.sqrMagnitude < zeroLengthSquared)
                {
                    tangent = Vector3.up;
                }

                Vector3 axis = Mathf.Abs(tangent.y) < 0.9f ? Vector3.up : Vector3.right;
                Vector3 side = Vector3.Cross(tangent, axis).normalized;
                // The leaves alternate sides along the chain
                if (i % 2 == 1)
                {
                    side = -side;
                }

                Vector3 direction = (side + tangent * leafLean).normalized;
                float length = _radius * leafLengthRadii * width;
                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction);
                Vector3 leafScale = new Vector3(length * leafWidth, length, length * leafDepth);
                _leaves[i] = Matrix4x4.TRS(_joints[j] + direction * length * leafCentre, rotation, leafScale);
                _beads[i] = Matrix4x4.TRS(_joints[j], Quaternion.identity, Vector3.one * _radius * beadRadii * width);
            }

            _beads[LeafCount] = DeliveryTip.Frame(tip, tip - _joints[_joints.Length - 2], tipWidth);
            if (!_tip.isSet || _tip.style != style)
            {
                _tip.SetStyle(style, _vocabulary, _meshes);
            }

            if (!SystemInfo.supportsInstancing || !_detailMaterial || !_detailMaterial.enableInstancing
                || !_container.gameObject.activeInHierarchy)
            {
                return;
            }

            int layer = _container.gameObject.layer;
            Graphics.DrawMeshInstanced(_leafMesh, 0, _detailMaterial, _leaves, LeafCount, _detailColour,
                ShadowCastingMode.On, true, layer);
            Graphics.DrawMeshInstanced(_beadMesh, 0, _detailMaterial, _beads, LeafCount, _detailColour,
                ShadowCastingMode.On, true, layer);
            _tip.Draw(_beads[LeafCount], _detailMaterial, _tipColour, _colour, layer);
        }
    }
}
