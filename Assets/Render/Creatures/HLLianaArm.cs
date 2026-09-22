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
        readonly Transform[] segments, beads;
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
                container = new GameObject("HLLianaArm").transform; container.SetParent(parent, false);
                segments = new Transform[lengths.Length]; beads = new Transform[rest.Length];
                for (int i = 0; i < segments.Length; i++) segments[i] = HLPrimitiveMeshes.Geometry("HLLink", container, HLPrimitive.CylinderSegment, material, definition.colour);
                for (int i = 0; i < beads.Length; i++) beads[i] = HLPrimitiveMeshes.Geometry("HLJoint", container, HLPrimitive.Sphere, material,
                    i == beads.Length - 1 ? new Color(.78f, .95f, .3f) : definition.colour);
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
            for (int i = 0; i < segments.Length; i++)
                HLPrimitiveMeshes.Segment(segments[i], joints[i], joints[i + 1], Mathf.Lerp(radius, radius * .56f, (float)i / segments.Length) * (Style == HLDeliveryStyle.Swarm ? .45f : 1));
            for (int i = 0; i < beads.Length; i++)
            {
                beads[i].position = joints[i];
                beads[i].localScale = Vector3.one * (Mathf.Lerp(radius, radius * .56f, (float)i / segments.Length) * (Style == HLDeliveryStyle.Swarm ? .45f : 1) * (i == segments.Length ? 3 : 2.1f));
            }
        }
        public void SetVisible(bool visible) { if (container) container.gameObject.SetActive(visible); }
        public void Dispose() { if (container) { container.gameObject.SetActive(false); HLPrimitiveMeshes.DestroyOwned(container.gameObject); } }
    }
}
