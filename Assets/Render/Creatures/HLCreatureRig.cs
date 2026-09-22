using System;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public readonly struct HLFootFrame
    {
        public readonly Vector3 origin, normal;
        public readonly float cellSize;
        public HLFootFrame(Vector3 origin, Vector3 normal, float cellSize) { this.origin = origin; this.normal = normal.normalized; this.cellSize = cellSize; }
    }

    public sealed class HLCreatureRig : IDisposable
    {
        public const int MaxArms = 8;
        readonly HLCreatureRecipe recipe;
        readonly Material material;
        readonly float cellSize;
        readonly Transform root, sway;
        readonly Transform[] pivots, geometry, roots;
        readonly HLLianaArm[] arms = new HLLianaArm[MaxArms];
        readonly int[] tokens = new int[MaxArms];
        readonly int[] definitions = new int[MaxArms];
        readonly Vector3?[] branchRoots = new Vector3?[MaxArms];
        readonly Transform[] motes = new Transform[12];
        readonly float[] moteBirth = new float[12];
        readonly Vector3[] moteOrigins = new Vector3[12];
        int nextToken, nextMote;
        float crownPulse;
        Vector3 previousOrigin;
        bool placed;
        public Transform Root => root;
        public int ActiveArmCount { get { int count = 0; foreach (var arm in arms) if (arm != null && !arm.IsAvailable) count++; return count; } }
        public int HealPulseCount { get; private set; }

        public static HLCreatureRig Build(HLCreatureRecipe data, Transform parent, Material material, float cellSize = 1)
        {
            if (!HLCreatureValidator.TryValidate(data, out var error)) throw new ArgumentException(error);
            if (!parent || !material || !HLChainSolver.Finite(cellSize) || cellSize <= 0) throw new ArgumentException("Rig requires parent, material and positive cell size.");
            Vector3 scale = parent.lossyScale;
            if (scale.x <= 0 || Mathf.Abs(scale.x - scale.y) > .0001f || Mathf.Abs(scale.x - scale.z) > .0001f)
                throw new ArgumentException("Creature rig ancestors must have positive uniform scale.");
            return new HLCreatureRig(data, parent, material, cellSize);
        }
        HLCreatureRig(HLCreatureRecipe data, Transform parent, Material material, float cellSize)
        {
            recipe = data; this.material = material; this.cellSize = cellSize;
            root = new GameObject("HLGeneratedCreature").transform; root.SetParent(parent, false); root.localScale = Vector3.one / parent.lossyScale.x;
            sway = new GameObject("HLSway").transform; sway.SetParent(root, false);
            pivots = new Transform[data.parts.Length]; geometry = new Transform[data.parts.Length];
            for (int i = 0; i < data.parts.Length; i++)
            {
                var part = data.parts[i];
                pivots[i] = new GameObject(part.id).transform; pivots[i].SetParent(part.parent < 0 ? sway : pivots[part.parent], false);
                pivots[i].localPosition = part.localPosition * cellSize; pivots[i].localRotation = Quaternion.Euler(part.localEuler);
                geometry[i] = HLPrimitiveMeshes.Geometry("HLGeometry", pivots[i], part.primitive, material, part.colour,
                    part.primitive == HLPrimitive.Torus ? part.torusTubeRatio : .25f, part.glow);
                geometry[i].localScale = part.dimensions * cellSize;
            }
            roots = new Transform[data.roots.count * 2];
            for (int i = 0; i < roots.Length; i++) roots[i] = HLPrimitiveMeshes.Geometry("HLRoot", root, HLPrimitive.CylinderSegment, material, data.roots.colour);
            for (int i = 0; i < data.arms.Length; i++) CreateArm(i, i);
            for (int i = 0; i < motes.Length; i++)
            {
                motes[i] = HLPrimitiveMeshes.Geometry("HLHealMote", root, HLPrimitive.Sphere, material, new Color(.78f, .95f, .29f), .25f, 1);
                motes[i].gameObject.SetActive(false); moteBirth[i] = float.NegativeInfinity;
            }
        }
        void CreateArm(int slot, int definition)
        {
            definitions[slot] = definition;
            arms[slot] = new HLLianaArm(recipe.arms[definition], root, material, cellSize);
            var d = recipe.arms[definition]; arms[slot].Tick(0, pivots[d.bodyPart].TransformPoint(d.rootLocal * cellSize), root.rotation);
        }
        int FreeSlot()
        {
            if (recipe.arms.Length == 0) return -1;
            for (int i = 0; i < MaxArms; i++)
                if (arms[i] == null || arms[i].IsAvailable) { if (arms[i] == null) CreateArm(i, i % recipe.arms.Length); return i; }
            return -1;
        }
        public int Begin(HLGestureKind kind, Vector3 goal)
        {
            int slot = FreeSlot(); if (slot < 0) return 0;
            if (++nextToken == 0) ++nextToken;
            tokens[slot] = nextToken; branchRoots[slot] = null;
            arms[slot].SetVisible(true); arms[slot].Begin(nextToken, kind, goal); return nextToken;
        }
        public void SetTipGoal(int token, Vector3 goal)
        { if (token == 0) return; for (int i = 0; i < MaxArms; i++) if (tokens[i] == token && !branchRoots[i].HasValue) arms[i]?.SetTipGoal(token, goal); }
        public void Contact(int token, Vector3 goal, Vector3? previousContact = null)
        {
            if (token == 0) { CoalesceContact(goal); return; }
            if (previousContact.HasValue)
            {
                int slot = FreeSlot(); if (slot < 0) { CoalesceContact(goal); return; }
                tokens[slot] = token; branchRoots[slot] = previousContact;
                arms[slot].SetVisible(true); arms[slot].Begin(token, HLGestureKind.Attack, goal); arms[slot].Contact(token, goal);
            }
            else for (int i = 0; i < MaxArms; i++) if (tokens[i] == token && !branchRoots[i].HasValue) arms[i]?.Contact(token, goal);
        }
        void CoalesceContact(Vector3 goal)
        {
            // Saturated same-position contacts renew an existing visual contact, without sharing
            // its lease token. A dropped observer can never end somebody else's chain.
            for (int i = 0; i < MaxArms; i++)
                if (arms[i] != null && !arms[i].IsAvailable && (arms[i].Goal - goal).sqrMagnitude < 1e-6f)
                { arms[i].Contact(arms[i].Token, goal); return; }
        }
        public void End(int token)
        { if (token == 0) return; for (int i = 0; i < MaxArms; i++) if (tokens[i] == token) arms[i]?.End(token); }
        public void CancelAll()
        {
            for (int i = 0; i < MaxArms; i++)
                if (arms[i] != null) { arms[i].Cancel(tokens[i]); tokens[i] = 0; }
        }
        public void HealContact(Vector3 point)
        { crownPulse = 1; int token = Begin(HLGestureKind.Heal, point); Contact(token, point); }
        public void EmitHeal(Vector3 point, float time)
        {
            HealPulseCount++;
            for (int j = 0; j < 3; j++)
            {
                int i = nextMote++ % motes.Length; moteBirth[i] = time;
                float angle = i * 2.39996f;
                moteOrigins[i] = point + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * .12f * cellSize;
                motes[i].gameObject.SetActive(true);
            }
        }
        public void Tick(float time, float deltaTime, in HLFootFrame frame)
        {
            if (!root) return;
            if (placed && Vector3.Distance(frame.origin, previousOrigin) > cellSize * .75f) CancelAll();
            previousOrigin = frame.origin; placed = true;
            root.SetPositionAndRotation(frame.origin, Quaternion.FromToRotation(Vector3.up, frame.normal));
            root.localScale = Vector3.one / root.parent.lossyScale.x;
            var idle = HLIdleMotion.Evaluate(recipe.idle, time);
            sway.localRotation = idle.sway; sway.localPosition = Vector3.up * idle.bodyLift * cellSize;
            crownPulse = Mathf.Max(0, crownPulse - Mathf.Max(0, deltaTime) / .2f);
            for (int i = 0; i < geometry.Length; i++) geometry[i].localScale = Vector3.Scale(recipe.parts[i].dimensions * cellSize, idle.bodyScale) * (1 + crownPulse * .06f);
            var r = recipe.roots;
            for (int i = 0; i < r.count; i++)
            {
                float angle = i * Mathf.PI * 2 / r.count + r.angularOffset * Mathf.Deg2Rad;
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                Vector3 hip = sway.TransformPoint((radial * .08f + Vector3.up * r.hipHeight) * cellSize);
                Vector3 knee = root.TransformPoint((radial * (r.footRadius * .6f) + Vector3.up * r.kneeHeight) * cellSize);
                Vector3 foot = root.TransformPoint(radial * r.footRadius * cellSize);
                HLPrimitiveMeshes.Segment(roots[i * 2], hip, knee, r.thickness * cellSize);
                HLPrimitiveMeshes.Segment(roots[i * 2 + 1], knee, foot, r.thickness * .65f * cellSize);
            }
            for (int i = 0; i < MaxArms; i++)
            {
                if (arms[i] == null) continue;
                var d = recipe.arms[definitions[i]];
                Vector3 shoulder = branchRoots[i] ?? pivots[d.bodyPart].TransformPoint(d.rootLocal * cellSize);
                arms[i].Tick(deltaTime, shoulder, root.rotation * idle.sway);
                if (arms[i].IsAvailable) { branchRoots[i] = null; tokens[i] = 0; arms[i].SetVisible(i < recipe.arms.Length); }
            }
            for (int i = 0; i < motes.Length; i++)
            {
                float age = time - moteBirth[i];
                bool active = age >= 0 && age < .45f; motes[i].gameObject.SetActive(active);
                if (!active) continue;
                motes[i].position = moteOrigins[i] + frame.normal * (age * .8f * cellSize);
                motes[i].localScale = Vector3.one * (.045f * cellSize * (1 - age / .45f));
            }
        }
        public void SetVisible(bool visible) { if (root) root.gameObject.SetActive(visible); }
        public void Dispose()
        {
            if (!root) return;
            root.gameObject.SetActive(false); HLPrimitiveMeshes.DestroyOwned(root.gameObject);
        }
    }
}
