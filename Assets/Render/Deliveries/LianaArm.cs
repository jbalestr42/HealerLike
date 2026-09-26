using System;
using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Deliveries
{
    // A render chain owned by its source: the pose it follows, the tube it draws, its leaves, beads and tip
    public class LianaArm : IDisposable
    {
        // Leaves along the chain, and as many beads plus the tip
        public static readonly int LeafCount = 5;
        // Proportions in arm radii: the tip unit, the leaves and the beads
        static readonly float tipWidthRadii = 4.5f;
        static readonly float leafLengthRadii = 9f;
        static readonly float beadRadii = 2.4f;
        // A leaf's width and depth for its length, its lean along the chain and where its centre sits along it
        static readonly float leafWidth = 0.38f;
        static readonly float leafDepth = 0.22f;
        static readonly float leafLean = 0.45f;
        static readonly float leafCentre = 0.45f;
        // A direction shorter than this has none
        static readonly float zeroLengthSquared = 0.000000000001f;

        readonly LianaPose _pose = new LianaPose();
        readonly LianaMesh _mesh = new LianaMesh();
        readonly Matrix4x4[] _leaves = new Matrix4x4[LeafCount];
        readonly Matrix4x4[] _beads = new Matrix4x4[LeafCount + 1];
        readonly DeliveryTip _tip = new DeliveryTip();
        Mesh _leafMesh;
        Mesh _beadMesh;
        PrimitiveMeshes _meshes;
        Material _detailMaterial;
        MaterialPropertyBlock _detailColour;
        float _radius;
        bool _isDisposed;
        bool _hasLoggedSolveError;
        bool _isVisible = true;
        Color _colour;
        // The tip and the arm look of each style, a bending arm with a bead tip without it
        DeliveryVocabulary _vocabulary;

        public int token { get { return _pose.token; } }

        public Vector3 goal { get { return _pose.goal; } }

        public DeliveryStyle style { get { return _pose.style; } set { _pose.style = value; } }

        public bool isDeliveryProfile
            { get { return _pose.isDeliveryProfile; } set { _pose.isDeliveryProfile = value; } }

        public GesturePhase phase { get { return _pose.phase; } }

        public Vector3 tip { get { return _pose.tip; } }

        public int segmentCount { get { return _pose.segmentCount; } }

        public bool isAvailable { get { return _pose.isAvailable; } }

        Color _tipColour;
        public Color tipColour { get { return _tipColour; } }

        Color _restTipColour;
        public Color restTipColour { get { return _restTipColour; } }

        // The width of one tip unit in world space
        public float tipWidth { get { return _radius * tipWidthRadii; } }

        // The tube's radius at its root in world space; it thins toward the tip
        public float radius { get { return _radius; } }

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
            _pose.Init(definition, cellSize);
            _radius = definition.radius * cellSize;
            _colour = definition.colour;
            _restTipColour = definition.colour;
            if (definition.tipColour.a > 0f)
            {
                _restTipColour = definition.tipColour;
            }

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
            _mesh.Init(parent, material, definition.colour, definition.segmentCount + 1);
            return true;
        }

        public Matrix4x4 LeafMatrix(int index)
        {
            return _leaves[index];
        }

        public Vector3 Joint(int index)
        {
            return _pose.Joint(index);
        }

        // The tip takes the family of the live delivery until the arm is back at rest
        public void SetTipAccent(int gestureToken, Color colour)
        {
            if (_pose.token != gestureToken || _pose.phase == GesturePhase.Rest)
            {
                return;
            }

            _tipColour = colour;
        }

        public void Begin(int gestureToken, GestureKind gestureKind, Vector3 worldTarget)
        {
            _tipColour = _restTipColour;
            _pose.Begin(gestureToken, gestureKind, worldTarget);
        }

        public void SetTipGoal(int gestureToken, Vector3 worldPosition)
        {
            _pose.SetTipGoal(gestureToken, worldPosition);
        }

        public void Contact(int gestureToken, Vector3 worldPosition)
        {
            _pose.Contact(gestureToken, worldPosition);
        }

        public void End(int gestureToken)
        {
            _pose.End(gestureToken);
        }

        public void Tick(float deltaTime, Vector3 rootWorld, Quaternion restOrientation)
        {
            if (!_pose.Tick(deltaTime, rootWorld, restOrientation, ArmStyleOf(_pose.style)))
            {
                HideForFrame();
                return;
            }

            if (_pose.phase == GesturePhase.Rest)
            {
                _tipColour = _restTipColour;
            }

            Draw();
        }

        public void SetVisible(bool value)
        {
            _isVisible = value;
            if (_mesh.renderer && (!_isVisible || _pose.phase == GesturePhase.Rest))
            {
                _mesh.renderer.enabled = false;
                _tip.Hide();
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _mesh.Dispose();
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
            if (_mesh.renderer)
            {
                _mesh.renderer.enabled = false;
                _tip.Hide();
            }

            if (!_hasLoggedSolveError)
            {
                Debug.LogError("[LianaArm] The chain has no finite solution, it stays hidden this frame.");
                _hasLoggedSolveError = true;
            }
        }

        void Draw()
        {
            if (!_mesh.container)
            {
                return;
            }

            _mesh.renderer.enabled = _isVisible && _pose.phase != GesturePhase.Rest;
            if (!_mesh.renderer.enabled)
            {
                _tip.Hide();
                return;
            }

            UpdateDetails();
            _mesh.Write(_pose, _radius, ArmStyleOf(_pose.style).width);
        }

        void UpdateDetails()
        {
            float width = ArmStyleOf(_pose.style).leafWidth;
            int segments = _pose.segmentCount;
            for (int i = 0; i < LeafCount; i++)
            {
                int j = Mathf.Clamp((i + 1) * segments / (LeafCount + 1), 1, segments - 1);
                Vector3 tangent = (_pose.Joint(j + 1) - _pose.Joint(j - 1)).normalized;
                if (tangent.sqrMagnitude < zeroLengthSquared)
                {
                    tangent = Vector3.up;
                }

                Vector3 axis = Vector3.right;
                if (Mathf.Abs(tangent.y) < 0.9f)
                {
                    axis = Vector3.up;
                }

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
                _leaves[i] = Matrix4x4.TRS(_pose.Joint(j) + direction * length * leafCentre, rotation, leafScale);
                Vector3 beadScale = Vector3.one * _radius * beadRadii * width;
                _beads[i] = Matrix4x4.TRS(_pose.Joint(j), Quaternion.identity, beadScale);
            }

            Vector3 last = _pose.Joint(_pose.jointCount - 2);
            _beads[LeafCount] = DeliveryTip.Frame(_pose.tip, _pose.tip - last, tipWidth);
            if (!_tip.isSet || _tip.style != _pose.style)
            {
                _tip.SetStyle(_pose.style, _vocabulary, _meshes);
            }

            _tip.Draw(_mesh.container, _beads[LeafCount], _detailMaterial, _tipColour, _colour);
            GameObject container = _mesh.container.gameObject;
            if (!SystemInfo.supportsInstancing || !_detailMaterial || !_detailMaterial.enableInstancing
                || !container.activeInHierarchy)
            {
                return;
            }

            int layer = container.layer;
            Graphics.DrawMeshInstanced(_leafMesh, 0, _detailMaterial, _leaves, LeafCount, _detailColour,
                ShadowCastingMode.On, true, layer);
            Graphics.DrawMeshInstanced(_beadMesh, 0, _detailMaterial, _beads, LeafCount, _detailColour,
                ShadowCastingMode.On, true, layer);
        }
    }
}
