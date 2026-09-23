using System;
using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Deliveries
{
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
        static readonly int leafCount = 5;

        readonly ChainSolver _solver = new ChainSolver();
        readonly Matrix4x4[] _leaves = new Matrix4x4[leafCount];
        readonly Matrix4x4[] _beads = new Matrix4x4[leafCount + 1];
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

        int _token;
        public int token { get { return _token; } }

        Vector3 _goal;
        public Vector3 goal { get { return _goal; } }

        public DeliveryStyle style { get; set; }

        public bool deliveryProfile { get; set; }

        public GesturePhase phase { get; private set; }

        public ChainResult lastResult { get; private set; }

        public int meshRevision { get; private set; }

        public int activeLeafCount { get { return _isVisible && phase != GesturePhase.Rest ? leafCount : 0; } }

        public Matrix4x4 tipMatrix { get { return _beads[leafCount]; } }

        public Vector3 tip { get { return _joints[_joints.Length - 1]; } }

        public int segmentCount { get { return _lengths.Length; } }

        public bool isAvailable { get { return phase == GesturePhase.Rest; } }

        // Without a parent the arm only solves its chain and draws nothing
        public bool Init(ArmDefinition definition, Transform parent, Material material, PrimitiveMeshes meshes,
            float cellSize = 1f)
        {
            if (definition.restJoints == null || definition.restJoints.Length != definition.segmentCount + 1
                || definition.segmentCount < 2 || !float.IsFinite(cellSize) || cellSize <= 0f)
            {
                Debug.LogError("[LianaArm] Invalid arm definition.");
                return false;
            }

            if (parent && !meshes)
            {
                Debug.LogError("[LianaArm] A drawn arm needs the primitive meshes.");
                return false;
            }

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
            if (!parent)
            {
                return true;
            }

            _leafMesh = meshes.cone;
            _beadMesh = meshes.sphere;
            _detailMaterial = material;
            _detailColour = new MaterialPropertyBlock();
            Vector4[] detailColours = new Vector4[leafCount + 1];
            for (int i = 0; i < detailColours.Length; i++)
            {
                detailColours[i] = definition.colour;
            }

            _detailColour.SetVectorArray("_BaseColor", detailColours);
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
            block.SetColor("_BaseColor", definition.colour);
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

        public void Begin(int gestureToken, GestureKind gestureKind, Vector3 worldTarget)
        {
            _token = gestureToken;
            _kind = gestureKind;
            _goal = worldTarget;
            _startGoal = tip;
            _isEndPending = false;
            phase = GesturePhase.Extend;
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
                phase = GesturePhase.Hold;
            }
        }

        public void Contact(int gestureToken, Vector3 worldPosition)
        {
            if (_token != gestureToken || phase == GesturePhase.Rest)
            {
                return;
            }

            _goal = worldPosition;
            phase = GesturePhase.Contact;
            _elapsed = 0f;
        }

        public void End(int gestureToken)
        {
            if (_token != gestureToken || phase == GesturePhase.Rest || phase == GesturePhase.Retract)
            {
                return;
            }

            if (phase == GesturePhase.Contact && _elapsed < 0.04f)
            {
                _isEndPending = true;
                return;
            }

            StartReturn();
        }

        public void Cancel(int gestureToken)
        {
            End(gestureToken);
        }

        public void Tick(float deltaTime, Vector3 rootWorld, Quaternion restOrientation)
        {
            float dt = Mathf.Max(0f, deltaTime);
            float retractSeconds = deliveryProfile && style == DeliveryStyle.Rigid ? 0.045f : 0.2f;
            Vector3 restTip = rootWorld + restOrientation * _rest[_rest.Length - 1];
            _elapsed += dt;
            if (phase == GesturePhase.Rest || (phase == GesturePhase.Retract && _elapsed >= retractSeconds))
            {
                phase = GesturePhase.Rest;
                for (int i = 0; i < _joints.Length; i++)
                {
                    _joints[i] = rootWorld + restOrientation * _rest[i];
                }
            }
            else
            {
                bool isRootFinite = float.IsFinite(rootWorld.x) && float.IsFinite(rootWorld.y)
                    && float.IsFinite(rootWorld.z);
                bool isGoalFinite = float.IsFinite(_goal.x) && float.IsFinite(_goal.y) && float.IsFinite(_goal.z);
                if (!isRootFinite || !isGoalFinite)
                {
                    HideForFrame();
                    return;
                }

                Vector3 target = _goal;
                if (phase == GesturePhase.Extend)
                {
                    target = Vector3.Lerp(_startGoal, _goal, Mathf.Clamp01(_elapsed / 0.1f));
                    if (_elapsed >= 0.1f)
                    {
                        phase = GesturePhase.Hold;
                        _elapsed = 0f;
                    }
                }

                if (phase == GesturePhase.Retract)
                {
                    float blend = Mathf.Clamp01(_elapsed / retractSeconds);
                    target = Vector3.Lerp(_returnGoal, restTip, blend);
                    // Only an initial guess, FABRIK projects every link right below
                    for (int i = 0; i < _joints.Length; i++)
                    {
                        _joints[i] = Vector3.Lerp(_joints[i], rootWorld + restOrientation * _rest[i], blend);
                    }
                }

                bool isRod = style == DeliveryStyle.Rigid || style == DeliveryStyle.Direct
                    || style == DeliveryStyle.Swarm || style == DeliveryStyle.Bounce
                    || style == DeliveryStyle.ChainSync;
                if (!deliveryProfile)
                {
                    if (!_solver.Solve(_joints, _lengths, rootWorld, target, restOrientation * _pole,
                        out ChainResult result, 64))
                    {
                        HideForFrame();
                        return;
                    }

                    lastResult = result;
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
                        float lift = 4f * t * (1f - t) * Mathf.Min(2f, Vector3.Distance(rootWorld, target) * 0.4f);
                        _joints[i] = Vector3.Lerp(rootWorld, target, t) + Vector3.up * lift;
                    }
                }

                if (phase == GesturePhase.Contact && _elapsed >= 0.04f)
                {
                    if (_isEndPending || _kind == GestureKind.Heal)
                    {
                        StartReturn();
                    }
                    else
                    {
                        phase = GesturePhase.Hold;
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
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(_container.gameObject);
                UnityEngine.Object.Destroy(_mesh);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(_container.gameObject);
                UnityEngine.Object.DestroyImmediate(_mesh);
            }
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
            phase = GesturePhase.Retract;
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
            for (int j = 0; j < _joints.Length; j++)
            {
                Vector3 tangent = _joints[Mathf.Min(j + 1, _joints.Length - 1)] - _joints[Mathf.Max(j - 1, 0)];
                if (tangent.sqrMagnitude < 0.000000000001f)
                {
                    tangent = Vector3.up;
                }

                tangent.Normalize();
                Vector3 axis = Mathf.Abs(tangent.y) < 0.9f ? Vector3.up : Vector3.right;
                Vector3 u = Vector3.Cross(tangent, axis).normalized;
                Vector3 v = Vector3.Cross(tangent, u);
                float width = Mathf.Lerp(_radius, _radius * 0.65f, (float)j / _lengths.Length);
                width *= style == DeliveryStyle.Swarm ? 0.6f : 1f;
                // Narrow collars between broader internodes read as a jointed plant arm at gameplay scale
                width *= (j % 3 == 0) ? 0.76f : 1.12f;
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
            meshRevision++;
        }

        void UpdateDetails()
        {
            float width = style == DeliveryStyle.Swarm ? 0.45f : 1f;
            for (int i = 0; i < leafCount; i++)
            {
                int j = Mathf.Clamp((i + 1) * _lengths.Length / (leafCount + 1), 1, _lengths.Length - 1);
                Vector3 tangent = (_joints[j + 1] - _joints[j - 1]).normalized;
                if (tangent.sqrMagnitude < 0.001f)
                {
                    tangent = Vector3.up;
                }

                Vector3 axis = Mathf.Abs(tangent.y) < 0.9f ? Vector3.up : Vector3.right;
                Vector3 side = Vector3.Cross(tangent, axis).normalized;
                Vector3 direction = (side * (i % 2 == 0 ? 1f : -1f) + tangent * 0.45f).normalized;
                float length = _radius * 9f * width;
                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction);
                Vector3 leafScale = new Vector3(length * 0.38f, length, length * 0.22f);
                _leaves[i] = Matrix4x4.TRS(_joints[j] + direction * length * 0.45f, rotation, leafScale);
                _beads[i] = Matrix4x4.TRS(_joints[j], Quaternion.identity, Vector3.one * _radius * 2.4f * width);
            }

            _beads[leafCount] = Matrix4x4.TRS(tip, Quaternion.identity, Vector3.one * _radius * 4.5f * width);
            if (!SystemInfo.supportsInstancing || !_detailMaterial || !_detailMaterial.enableInstancing
                || !_container.gameObject.activeInHierarchy)
            {
                return;
            }

            int layer = _container.gameObject.layer;
            Graphics.DrawMeshInstanced(_leafMesh, 0, _detailMaterial, _leaves, leafCount, _detailColour,
                ShadowCastingMode.On, true, layer);
            Graphics.DrawMeshInstanced(_beadMesh, 0, _detailMaterial, _beads, leafCount + 1, _detailColour,
                ShadowCastingMode.On, true, layer);
        }
    }
}
