using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public readonly struct HLFootFrame
    {
        public readonly Vector3 origin, normal;
        public readonly float cellSize;
        public HLFootFrame(Vector3 origin, Vector3 normal, float cellSize) { this.origin = origin; this.normal = normal.normalized; this.cellSize = cellSize; }
    }

    public sealed class HLCreatureRig : IDisposable, IHLDeliverySource
    {
        public const int MaxArms = 8;
        readonly HLCreatureRecipe recipe;
        readonly Material material;
        readonly float cellSize;
        readonly Transform root, sway;
        readonly Transform[] pivots, geometry, roots;
        readonly Renderer[] bodyRenderers;
        readonly Color[] colours;
        readonly HLIdleDefinition idle;
        readonly HLLianaArm[] arms = new HLLianaArm[MaxArms];
        readonly int[] tokens = new int[MaxArms];
        readonly int[] definitions = new int[MaxArms];
        readonly Vector3?[] branchRoots = new Vector3?[MaxArms];
        int nextToken;
        bool disposed;
        float crownPulse, hitPulse;
        Quaternion aim = Quaternion.identity;
        Vector3? aimTarget;
        float healthFraction = 1, charge, budPower;
        Color statusTint = Color.white;
        public void SetStatusTint(Color tint) => statusTint = tint;
        readonly MaterialPropertyBlock colourBlock = new MaterialPropertyBlock();
        readonly Dictionary<int, (int lease, HLDeliveryStyle style, Vector3? contact)> deliveries = new Dictionary<int, (int, HLDeliveryStyle, Vector3?)>();
        public float Charge => charge;
        public float HealthFraction => healthFraction;
        public Quaternion Aim => aim;
        public Transform[] BudAnchors { get; private set; }
        public void SetReadout(Vector3? target, float health, float readiness, float glow)
        {
            aimTarget = target;
            healthFraction = HLChainSolver.Finite(health) ? Mathf.Clamp01(health) : 1;
            charge = HLChainSolver.Finite(readiness) ? Mathf.Clamp01(readiness) : 0;
            budPower = HLChainSolver.Finite(glow) ? Mathf.Clamp01(glow) : 0;
        }
        public void Hit() => hitPulse = 1;
        public bool BeginDelivery(int token, HLDeliveryStyle style, Transform projectile, Vector3 intendedEnd)
        {
            if (token == 0 || style == HLDeliveryStyle.Thrown || deliveries.ContainsKey(token)) return false;
            if (style == HLDeliveryStyle.Swarm)
            {
                int count = 0;
                foreach (var item in deliveries.Values) if (item.style == style) count++;
                if (count >= 4) return false;
            }
            int lease = Begin(HLGestureKind.Attack, projectile ? projectile.position : intendedEnd);
            if (lease == 0) return false;
            deliveries.Add(token, (lease, style, null));
            for (int i = 0; i < MaxArms; i++) if (tokens[i] == lease) { arms[i].Style = style; arms[i].DeliveryProfile = true; }
            charge = 0;
            return true;
        }
        public void UpdateDelivery(int token, Vector3 position)
        {
            if (!deliveries.TryGetValue(token, out var d)) return;
            if (d.style == HLDeliveryStyle.ChainSync && d.contact.HasValue) return;
            SetTipGoal(d.lease, position);
        }
        public void ContactDelivery(int token, Vector3 position, GameObject target) => ContactDeliveryPath(token, position, false);
        public void ContactDeliveryPath(int token, Vector3 position, bool preserve)
        {
            if (!deliveries.TryGetValue(token, out var d)) return;
            Contact(d.lease, position, (preserve || d.style == HLDeliveryStyle.ChainSync) ? d.contact : null);
            deliveries[token] = (d.lease, d.style, position);
        }
        public void EndDelivery(int token)
        {
            if (!deliveries.TryGetValue(token, out var d)) return;
            End(d.lease); deliveries.Remove(token);
        }
        Vector3 previousOrigin;
        bool placed;
        public Transform Root => root;
        public int ActiveArmCount { get { int count = 0; foreach (var arm in arms) if (arm != null && !arm.IsAvailable) count++; return count; } }

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
            HLPrimitiveMeshes.Retain();
            recipe = data; this.material = material; this.cellSize = cellSize;
            root = new GameObject("HLGeneratedCreature").transform; root.SetParent(parent, false); root.localScale = Vector3.one / parent.lossyScale.x;
            sway = new GameObject("HLSway").transform; sway.SetParent(root, false);
            pivots = new Transform[data.parts.Length]; geometry = new Transform[data.parts.Length];
            bodyRenderers = new Renderer[data.parts.Length];
            colours = new Color[data.parts.Length];
            idle = data.idle; idle.seed ^= parent.GetEntityId().GetHashCode();
            for (int i = 0; i < data.parts.Length; i++)
            {
                var part = data.parts[i];
                colours[i] = HLBeautyMotion.Vary(part.colour, idle.seed);
                pivots[i] = new GameObject(part.id).transform; pivots[i].SetParent(part.parent < 0 ? sway : pivots[part.parent], false);
                pivots[i].localPosition = part.localPosition * cellSize; pivots[i].localRotation = Quaternion.Euler(part.localEuler);
                geometry[i] = HLPrimitiveMeshes.Geometry("HLGeometry", pivots[i], part.primitive, material, colours[i],
                    part.primitive == HLPrimitive.Torus ? part.torusTubeRatio : .25f, part.glow);
                geometry[i].localScale = part.dimensions * cellSize;
                bodyRenderers[i] = geometry[i].GetComponent<Renderer>();
            }
            BudAnchors = Array.FindAll(pivots, p => p.name.StartsWith("HLBud", StringComparison.Ordinal));
            roots = new Transform[data.roots.count * 2];
            for (int i = 0; i < roots.Length; i++) roots[i] = HLPrimitiveMeshes.Geometry("HLRoot", root, HLPrimitive.Cone, material, HLBeautyMotion.Vary(data.roots.colour, idle.seed));
            for (int i = 0; i < data.arms.Length; i++) CreateArm(i, i);
        }

        void CreateArm(int slot, int definition)
        {
            definitions[slot] = definition;
            var d = recipe.arms[definition]; d.colour = HLBeautyMotion.Vary(d.colour, idle.seed);
            arms[slot] = new HLLianaArm(d, root, material, cellSize); arms[slot].Tick(0, pivots[d.bodyPart].TransformPoint(d.rootLocal * cellSize), root.rotation);
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
            arms[slot].DeliveryProfile = kind == HLGestureKind.Heal; arms[slot].Style = kind == HLGestureKind.Heal ? HLDeliveryStyle.Arc : HLDeliveryStyle.Direct; arms[slot].SetVisible(true); arms[slot].Begin(nextToken, kind, goal); return nextToken;
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
                arms[slot].Style = HLDeliveryStyle.ChainSync; arms[slot].DeliveryProfile = true;
                arms[slot].SetVisible(true); arms[slot].Begin(token, HLGestureKind.Attack, goal); arms[slot].Contact(token, goal);
            }
            else for (int i = 0; i < MaxArms; i++) if (tokens[i] == token && !branchRoots[i].HasValue) arms[i]?.Contact(token, goal);
        }
        void CoalesceContact(Vector3 goal)
        {
            // Saturated same-position contacts renew an existing visual contact, without sharing
            // its lease token. A dropped observer can never end somebody else's chain.
            for (int i = 0; i < MaxArms; i++)
                if (tokens[i] != 0 && arms[i] != null && !arms[i].IsAvailable && (arms[i].Goal - goal).sqrMagnitude < 1e-6f)
                { arms[i].Contact(arms[i].Token, goal); return; }
        }
        public void End(int token)
        { if (token == 0) return; for (int i = 0; i < MaxArms; i++) if (tokens[i] == token) arms[i]?.End(token); }
        public void CancelAll()
        {
            deliveries.Clear();
            for (int i = 0; i < MaxArms; i++)
                if (arms[i] != null) { arms[i].Cancel(tokens[i]); tokens[i] = 0; }
        }
        public void HealContact(Vector3 point)
        { crownPulse = 1; int token = Begin(HLGestureKind.Heal, point); Contact(token, point); }
        public void Tick(float time, float deltaTime, in HLFootFrame frame)
        {
            if (!root) return;
            if (placed && Vector3.Distance(frame.origin, previousOrigin) > cellSize * .75f) CancelAll();
            previousOrigin = frame.origin; placed = true;
            root.SetPositionAndRotation(frame.origin, Quaternion.FromToRotation(Vector3.up, frame.normal));
            root.localScale = Vector3.one / root.parent.lossyScale.x;
            float dt = Mathf.Max(0, deltaTime);
            Vector3 direction = aimTarget.HasValue ? root.InverseTransformDirection(aimTarget.Value - frame.origin) :
                new Vector3(Mathf.Sin(time * .3f) * .4f, 0, 1);
            direction.y = 0;
            if (direction.sqrMagnitude > .000001f)
                aim = Quaternion.Slerp(aim, Quaternion.LookRotation(direction), 1 - Mathf.Exp(-dt * 7));
            hitPulse = Mathf.Max(0, hitPulse - dt * 5);
            var idlePose = HLIdleMotion.Evaluate(idle, time);
            sway.localRotation = aim * idlePose.sway * Quaternion.Euler((1 - healthFraction) * 32, 0, Mathf.Sin(hitPulse * 24) * hitPulse * 9);
            sway.localPosition = Vector3.down * ((1 - healthFraction) * .08f * cellSize);
            crownPulse = Mathf.Max(0, crownPulse - dt / .2f);
            for (int i = 0; i < geometry.Length; i++)
            {
                var part = recipe.parts[i];
                bool head = part.primitive == HLPrimitive.Sphere || part.glow > 0 || part.id == "HLBulb";
                geometry[i].localScale = Vector3.Scale(part.dimensions, idlePose.bodyScale) * cellSize * (1 + crownPulse * .06f + (head ? charge * .24f : 0));
                if (part.id == "HLCrown") pivots[i].localRotation = Quaternion.Euler(part.localEuler) * Quaternion.AngleAxis(time * 18, Vector3.up);
                Color colour = Color.Lerp(new Color(.18f, .49f, .31f, colours[i].a), colours[i], healthFraction);
                float light = Mathf.Max(budPower, charge) * healthFraction;
                if (part.glow > 0) colour = Color.Lerp(new Color(colour.r * .55f, colour.g * .55f, colour.b * .55f, colour.a),
                    new Color(.78f, .95f, .29f, colour.a), light);
                colour = statusTint == Color.white ? colour : Color.Lerp(colour, statusTint, .42f);
                colourBlock.SetColor("_BaseColor", HLPrimitiveMeshes.Brighten(colour, part.glow * light));
                bodyRenderers[i].SetPropertyBlock(colourBlock);
            }
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
                arms[i].Tick(deltaTime, shoulder, root.rotation * sway.localRotation);
                if (arms[i].IsAvailable) { branchRoots[i] = null; tokens[i] = 0; arms[i].SetVisible(i < recipe.arms.Length); }
            }
        }

        public void SetVisible(bool visible) { if (root) root.gameObject.SetActive(visible); }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            foreach (var arm in arms) arm?.Dispose();
            if (root) { root.gameObject.SetActive(false); HLPrimitiveMeshes.DestroyOwned(root.gameObject); }
            HLPrimitiveMeshes.Release();
        }
    }
}
