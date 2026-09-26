using System;
using UnityEngine;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Deliveries
{
    // A render chain owned by its source: the pose it follows, the tube it draws, its leaves, beads and tip
    public class LianaArm : IDisposable
    {
        // Leaves along the chain, and as many beads plus the tip
        public static readonly int LeafCount = 5;
        // The width of one tip unit, in arm radii
        static readonly float tipWidthRadii = 4.5f;
        readonly LianaPose _pose = new LianaPose();
        readonly LianaMesh _mesh = new LianaMesh();
        readonly LianaDetails _details = new LianaDetails();
        float _radius;
        bool _isDisposed;
        bool _hasLoggedSolveError;
        bool _isVisible = true;
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

        // Without a parent the arm only solves its chain and draws nothing
        public bool Init(ArmDefinition definition, Transform parent, Material material, PrimitiveMeshes meshes,
            DeliveryVocabulary vocabulary, float cellSize = 1f, bool copyBorrowedMeshes = false)
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

            _details.Init(meshes, material, definition.colour, copyBorrowedMeshes);
            _mesh.Init(parent, material, definition.colour, definition.segmentCount + 1);
            return true;
        }

        public Matrix4x4 LeafMatrix(int index)
        {
            return _details.LeafMatrix(index);
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
                _details.Hide();
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _details.Dispose();
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
                _details.Hide();
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
                _details.Hide();
                return;
            }

            _details.Draw(_pose, _mesh.container, _radius, ArmStyleOf(_pose.style).leafWidth,
                tipWidth, _vocabulary, _tipColour);
            _mesh.Write(_pose, _radius, ArmStyleOf(_pose.style).width);
        }
    }
}
