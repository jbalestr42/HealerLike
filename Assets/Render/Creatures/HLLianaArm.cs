using System;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public enum HLGestureKind { Attack, Heal, Channel }
    public enum HLGesturePhase { Rest, Extend, Contact, Hold, Retract }

    /// <summary>A source-owned render chain; gestures change goals, never link lengths or gameplay.</summary>
    public sealed class HLLianaArm : IDisposable
    {
        readonly HLChainSolver solver = new HLChainSolver();
        readonly Vector3[] rest, joints;
        readonly float[] lengths;
        readonly Transform container;
        readonly Mesh mesh;
        readonly MeshRenderer renderer;
        readonly Vector3[] vertices, normals;
        const int Sides = 6;
        const int LeafCount = 5;
        readonly Matrix4x4[] leaves = new Matrix4x4[LeafCount];
        readonly Matrix4x4[] beads = new Matrix4x4[LeafCount + 1];
        readonly Mesh leafMesh, beadMesh;
        readonly Material detailMaterial;
        readonly MaterialPropertyBlock detailColour;
        bool disposed;
        public int ActiveLeafCount => visible && Phase != HLGesturePhase.Rest ? LeafCount : 0;
        public Matrix4x4 LeafMatrix(int index) => leaves[index];
        public Matrix4x4 TipMatrix => beads[LeafCount];
        bool visible = true;
        public int MeshRevision { get; private set; }
        readonly float radius;
        readonly Vector3 pole;
        int token;
        float elapsed;
        Vector3 goal, startGoal, returnGoal;
        bool pendingEnd;
        HLGestureKind kind;
        public HLDeliveryStyle Style { get; set; }
        public bool DeliveryProfile { get; set; }
        public HLGesturePhase Phase { get; private set; }
        public HLChainResult LastResult { get; private set; }
        public Vector3 Tip => joints[joints.Length - 1];
        public Vector3 Goal => goal;
        public int Token => token;
        public Vector3 Joint(int index) => joints[index];
        public int SegmentCount => lengths.Length;
        public bool IsAvailable => Phase == HLGesturePhase.Rest;

        public HLLianaArm(in HLArmDefinition definition, Transform parent, Material material, float cellSize = 1)
        {
            if (definition.restJoints == null || definition.restJoints.Length != definition.segmentCount + 1 || definition.segmentCount < 2
                || !HLChainSolver.Finite(cellSize) || cellSize <= 0) throw new ArgumentException("Invalid arm definition.");
            rest = new Vector3[definition.restJoints.Length]; joints = new Vector3[rest.Length]; lengths = new float[definition.segmentCount];
            for (int i = 0; i < rest.Length; i++) rest[i] = definition.restJoints[i] * cellSize;
            for (int i = 0; i < lengths.Length; i++) lengths[i] = definition.segmentLength * cellSize;
            radius = definition.radius * cellSize; pole = definition.bendPole;
            if (parent)
            {
                HLPrimitiveMeshes.Retain();
                leafMesh = HLPrimitiveMeshes.Get(HLPrimitive.Cone);
                beadMesh = HLPrimitiveMeshes.Get(HLPrimitive.Sphere);
                detailMaterial = material;
                detailColour = new MaterialPropertyBlock();
                var detailColours = new Vector4[LeafCount + 1];
                for (int i = 0; i < detailColours.Length; i++) detailColours[i] = definition.colour;
                detailColour.SetVectorArray("_BaseColor", detailColours);
                container = new GameObject("HLLianaArm").transform; container.SetParent(parent, false);
                mesh = new Mesh { name = "HLLianaChain", hideFlags = HideFlags.DontSave };
                mesh.MarkDynamic();
                vertices = new Vector3[joints.Length * Sides + 2]; normals = new Vector3[vertices.Length];
                var triangles = new int[lengths.Length * Sides * 6 + Sides * 6];
                int index = 0;
                for (int j = 0; j < lengths.Length; j++) for (int side = 0; side < Sides; side++)
                {
                    int a = j * Sides + side, next = j * Sides + (side + 1) % Sides;
                    int b = a + Sides, nextB = next + Sides;
                    triangles[index++] = a; triangles[index++] = next; triangles[index++] = b;
                    triangles[index++] = next; triangles[index++] = nextB; triangles[index++] = b;
                }
                for (int side = 0; side < Sides; side++)
                {
                    int next = (side + 1) % Sides, end = lengths.Length * Sides;
                    triangles[index++] = vertices.Length - 2; triangles[index++] = next; triangles[index++] = side;
                    triangles[index++] = vertices.Length - 1; triangles[index++] = end + side; triangles[index++] = end + next;
                }
                mesh.vertices = vertices; mesh.triangles = triangles;
                container.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                renderer = container.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
                var block = new MaterialPropertyBlock(); block.SetColor("_BaseColor", definition.colour); renderer.SetPropertyBlock(block);
                renderer.enabled = false;
            }
        }
        public void Begin(int gestureToken, HLGestureKind gestureKind, Vector3 worldTarget)
        {
            token = gestureToken; kind = gestureKind; goal = worldTarget; startGoal = Tip; pendingEnd = false;
            Phase = HLGesturePhase.Extend; elapsed = 0;
        }
        public void SetTipGoal(int gestureToken, Vector3 worldPosition)
        {
            if (token != gestureToken || Phase == HLGesturePhase.Rest || Phase == HLGesturePhase.Retract) return;
            goal = worldPosition;
            // Projectile travel is already the authoritative extension timing.
            if (Phase != HLGesturePhase.Contact || Style == HLDeliveryStyle.Bounce) Phase = HLGesturePhase.Hold;
        }
        public void Contact(int gestureToken, Vector3 worldPosition)
        {
            if (token != gestureToken || Phase == HLGesturePhase.Rest) return;
            goal = worldPosition; Phase = HLGesturePhase.Contact; elapsed = 0;
        }
        public void End(int gestureToken)
        {
            if (token != gestureToken || Phase == HLGesturePhase.Rest || Phase == HLGesturePhase.Retract) return;
            if (Phase == HLGesturePhase.Contact && elapsed < .04f) { pendingEnd = true; return; }
            StartReturn();
        }
        public void Cancel(int gestureToken) => End(gestureToken);
        void StartReturn() { returnGoal = Tip; Phase = HLGesturePhase.Retract; elapsed = 0; pendingEnd = false; }
        public void Tick(float deltaTime, Vector3 rootWorld, Quaternion restOrientation)
        {
            float dt = Mathf.Max(0, deltaTime);
            float retractSeconds = DeliveryProfile && Style == HLDeliveryStyle.Rigid ? .045f : .20f;
            Vector3 restTip = rootWorld + restOrientation * rest[rest.Length - 1];
            elapsed += dt;
            if (Phase == HLGesturePhase.Rest || (Phase == HLGesturePhase.Retract && elapsed >= retractSeconds))
            {
                Phase = HLGesturePhase.Rest;
                for (int i = 0; i < joints.Length; i++) joints[i] = rootWorld + restOrientation * rest[i];
            }
            else
            {
                Vector3 target = goal;
                if (Phase == HLGesturePhase.Extend)
                {
                    target = Vector3.Lerp(startGoal, goal, Mathf.Clamp01(elapsed / .10f));
                    if (elapsed >= .10f) { Phase = HLGesturePhase.Hold; elapsed = 0; }
                }
                if (Phase == HLGesturePhase.Retract)
                {
                    float blend = Mathf.Clamp01(elapsed / retractSeconds);
                    target = Vector3.Lerp(returnGoal, restTip, blend);
                    // This is only an initial guess; FABRIK projects every link immediately below.
                    for (int i = 0; i < joints.Length; i++) joints[i] = Vector3.Lerp(joints[i], rootWorld + restOrientation * rest[i], blend);
                }
                if (!DeliveryProfile) LastResult = solver.Solve(joints, lengths, rootWorld, target, restOrientation * pole, 64);
                else if (Style == HLDeliveryStyle.Rigid || Style == HLDeliveryStyle.Direct || Style == HLDeliveryStyle.Swarm || Style == HLDeliveryStyle.Bounce || Style == HLDeliveryStyle.ChainSync)
                {
                    // Delivery rods telescope visually; the projectile endpoint remains authoritative.
                    if (Style == HLDeliveryStyle.Rigid && Phase != HLGesturePhase.Retract) target = goal;
                    for (int i = 0; i < joints.Length; i++) joints[i] = Vector3.Lerp(rootWorld, target, (float)i / (joints.Length - 1));
                }
                else
                {
                    for (int i = 0; i < joints.Length; i++)
                    {
                        float t = (float)i / (joints.Length - 1);
                        joints[i] = Vector3.Lerp(rootWorld, target, t) + Vector3.up * (4 * t * (1 - t) * Mathf.Min(2, Vector3.Distance(rootWorld, target) * .4f));
                    }
                }
                if (Phase == HLGesturePhase.Contact && elapsed >= .04f)
                {
                    if (pendingEnd || kind == HLGestureKind.Heal) StartReturn(); else { Phase = HLGesturePhase.Hold; elapsed = 0; }
                }
            }
            Draw();
        }
        void Draw()
        {
            if (!container) return;
            renderer.enabled = visible && Phase != HLGesturePhase.Rest;
            if (!renderer.enabled) return;
            UpdateDetails();
            for (int j = 0; j < joints.Length; j++)
            {
                Vector3 tangent = joints[Mathf.Min(j + 1, joints.Length - 1)] - joints[Mathf.Max(j - 1, 0)];
                if (tangent.sqrMagnitude < 1e-12f) tangent = Vector3.up;
                tangent.Normalize();
                Vector3 axis = Mathf.Abs(tangent.y) < .9f ? Vector3.up : Vector3.right;
                Vector3 u = Vector3.Cross(tangent, axis).normalized, v = Vector3.Cross(tangent, u);
                float width = Mathf.Lerp(radius, radius * .56f, (float)j / lengths.Length) * (Style == HLDeliveryStyle.Swarm ? .45f : 1);
                for (int side = 0; side < Sides; side++)
                {
                    float angle = side * Mathf.PI * 2 / Sides;
                    Vector3 normal = u * Mathf.Cos(angle) + v * Mathf.Sin(angle);
                    int index = j * Sides + side;
                    vertices[index] = container.InverseTransformPoint(joints[j] + normal * width);
                    normals[index] = container.InverseTransformDirection(normal);
                }
            }
            vertices[vertices.Length - 2] = container.InverseTransformPoint(joints[0]);
            vertices[vertices.Length - 1] = container.InverseTransformPoint(Tip);
            normals[normals.Length - 2] = container.InverseTransformDirection((joints[0] - joints[1]).normalized);
            normals[normals.Length - 1] = container.InverseTransformDirection((Tip - joints[joints.Length - 2]).normalized);
            mesh.vertices = vertices; mesh.normals = normals; mesh.RecalculateBounds(); MeshRevision++;
        }
        void UpdateDetails()
        {
            float width = Style == HLDeliveryStyle.Swarm ? .45f : 1;
            for (int i = 0; i < LeafCount; i++)
            {
                int j = Mathf.Clamp((i + 1) * lengths.Length / (LeafCount + 1), 1, lengths.Length - 1);
                Vector3 tangent = (joints[j + 1] - joints[j - 1]).normalized;
                if (tangent.sqrMagnitude < .001f) tangent = Vector3.up;
                Vector3 side = Vector3.Cross(tangent, Mathf.Abs(tangent.y) < .9f ? Vector3.up : Vector3.right).normalized;
                Vector3 direction = (side * (i % 2 == 0 ? 1 : -1) + tangent * .45f).normalized;
                float length = radius * 9 * width;
                leaves[i] = Matrix4x4.TRS(joints[j] + direction * length * .45f,
                    Quaternion.FromToRotation(Vector3.up, direction), new Vector3(length * .38f, length, length * .22f));
                beads[i] = Matrix4x4.TRS(joints[j], Quaternion.identity, Vector3.one * radius * 2.4f * width);
            }
            beads[LeafCount] = Matrix4x4.TRS(Tip, Quaternion.identity, Vector3.one * radius * 4.5f * width);
            if (!SystemInfo.supportsInstancing || !detailMaterial || !detailMaterial.enableInstancing || !container.gameObject.activeInHierarchy) return;
            Graphics.DrawMeshInstanced(leafMesh, 0, detailMaterial, leaves, LeafCount, detailColour,
                UnityEngine.Rendering.ShadowCastingMode.On, true, container.gameObject.layer);
            Graphics.DrawMeshInstanced(beadMesh, 0, detailMaterial, beads, LeafCount + 1, detailColour,
                UnityEngine.Rendering.ShadowCastingMode.On, true, container.gameObject.layer);
        }
        public void SetVisible(bool value)
        {
            visible = value;
            if (renderer && (!visible || Phase == HLGesturePhase.Rest)) renderer.enabled = false;
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (leafMesh) HLPrimitiveMeshes.Release();
            if (container) { container.gameObject.SetActive(false); HLPrimitiveMeshes.DestroyOwned(container.gameObject); }
            HLPrimitiveMeshes.DestroyOwned(mesh);
        }
    }
}
