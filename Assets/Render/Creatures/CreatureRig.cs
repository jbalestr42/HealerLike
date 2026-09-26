using System.Collections.Generic;
using System;
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

        // A dying unit leans this many degrees and sinks this many cells
        static readonly float wiltLean = 32f;
        static readonly float wiltSink = 0.08f;
        readonly CreatureAssembly _assembly = new CreatureAssembly();
        readonly CreatureFacing _facing = new CreatureFacing();
        CreatureRecipe _recipe;
        float _cellSize;
        IdleDefinition _idle;
        float _crownPulse;
        float _hitPulse;
        Vector3? _aimTarget;
        float _budPower;
        bool _isDisposed;
        float _charge;
        float _healthFraction = 1f;
        float _appearanceElapsed = CreatureAppearance.Duration;
        int _revision;
        public float appearanceElapsed => _appearanceElapsed;

        public bool isAppearing => _appearanceElapsed < CreatureAppearance.Duration;

        public Transform[] budAnchors => _assembly.buds;

        public Transform root => _assembly.root;

        public CreatureRecipe recipe => _recipe;

        public int revision => _revision;

        public float cellSize => _cellSize;

        public IReadOnlyList<Transform> partTransforms => _assembly.geometry;

        public IReadOnlyList<CreaturePart> parts
        {
            get { return _assembly.data != null ? _assembly.data.parts : Array.Empty<CreaturePart>(); }
        }

        public int armCount => _assembly.data != null ? _assembly.data.arms.Length : 0;

        public RootDefinition roots => _assembly.data != null ? _assembly.data.roots : default;

        public Quaternion armRotation => root.rotation * _assembly.sway.localRotation;

        public bool Init(
            CreatureRecipe data,
            Transform parent,
            Material material,
            PrimitiveMeshes meshes,
            float cellSize = 1f
        )
        {
            return Init(data, parent, material, material, meshes, cellSize);
        }

        // A recipe that fails validation logs and leaves the view empty. Body, Head and Stem surfaces use the
        // body material; tips and roots retain the shared material. The caller gives stones one material
        // for both slots, so surface shading does not infer a side from colour, geometry or an object name.
        public bool Init(
            CreatureRecipe data,
            Transform parent,
            Material material,
            Material bodyMaterial,
            PrimitiveMeshes meshes,
            float cellSize,
            Transform seedSource = null
        )
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
            if (
                !RenderMath.IsFinite(scale)
                || scale.x <= 0f
                || Mathf.Abs(scale.x - scale.y) > 0.0001f
                || Mathf.Abs(scale.x - scale.z) > 0.0001f
            )
            {
                Debug.LogError("[CreatureRig] Creature rig ancestors must have positive uniform scale.");
                return false;
            }

            if (_isDisposed || root != null)
            {
                return false;
            }

            _cellSize = cellSize;
            _assembly.Init(parent, seedSource);
            if (Recompose(data, material, bodyMaterial, meshes))
            {
                return true;
            }

            _assembly.Dispose();
            return false;
        }

        // Keep the live root and pivots: held effects and projectiles can still resolve their source while the
        // vocabulary changes. Spare pivots are reused after a topology edit, bounded by the validated part cap.
        public bool Recompose(CreatureRecipe data, Material material, Material bodyMaterial, PrimitiveMeshes meshes)
        {
            if (_isDisposed || !root || !material || !meshes || !CreatureValidator.TryValidate(data, out _))
            {
                return false;
            }

            if (bodyMaterial == null)
            {
                bodyMaterial = material;
            }

            if (!_assembly.Recompose(data, material, bodyMaterial, meshes, _cellSize, _healthFraction))
            {
                return false;
            }

            _recipe = data;
            _idle = _assembly.data.idle;
            _idle.seed = _assembly.seed;
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
            _facing.SetForward(forward);
        }

        // Default rigs stay grown for Studio and existing callers. Only a new live view or placement opts in.
        public void BeginAppearance()
        {
            _appearanceElapsed = 0f;
        }

        public void AdvanceAppearance(float deltaTime)
        {
            if (float.IsFinite(deltaTime) && deltaTime > 0f)
            {
                _appearanceElapsed = Mathf.Min(CreatureAppearance.Duration, _appearanceElapsed + deltaTime);
            }
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
            if (!root)
            {
                return;
            }

            root.SetPositionAndRotation(frame.origin, Quaternion.FromToRotation(Vector3.up, frame.normal));
            root.localScale = Vector3.one / root.parent.lossyScale.x;
            float dt = Mathf.Max(0f, deltaTime);
            Quaternion aim = _facing.Evaluate(time, dt, frame.origin, root, _aimTarget);
            _hitPulse = Mathf.Max(0f, _hitPulse - dt * 5f);
            IdlePose idlePose = IdleMotion.Evaluate(_idle, time);
            float shake = Mathf.Sin(_hitPulse * 24f) * _hitPulse * 9f;
            Quaternion wilt = Quaternion.Euler((1f - _healthFraction) * wiltLean, 0f, shake);
            _assembly.sway.localRotation = aim * idlePose.sway * wilt;
            _assembly.sway.localPosition = Vector3.down * ((1f - _healthFraction) * wiltSink * _cellSize);
            _crownPulse = Mathf.Max(0f, _crownPulse - dt / 0.2f);
            float light = Mathf.Max(_budPower, _charge) * _healthFraction;
            _assembly.Tick(time, idlePose, _cellSize, _crownPulse, _charge, _healthFraction, light, _appearanceElapsed);
        }

        public void SetSelection(CreatureSelection selection)
        {
            _assembly.selection = selection;
        }

        // Where arm index leaves the body now, in world space
        public Vector3 ArmSocket(int index)
        {
            if (_assembly.data.arms.Length == 0)
            {
                return _assembly.sway.TransformPoint(_assembly.data.neckLocal * _cellSize);
            }

            index %= _assembly.data.arms.Length;
            ArmDefinition definition = _assembly.data.arms[index];
            return _assembly.Pivot(definition.bodyPart).TransformPoint(definition.rootLocal * _cellSize);
        }

        // Arm index as the recipe authors it, its colour varied like the rest of the unit
        public ArmDefinition GetArm(int index)
        {
            ArmDefinition definition = _assembly.data.arms[index];
            definition.restJoints = (Vector3[])definition.restJoints.Clone();
            definition.colour = ColourJitter.Vary(definition.colour, _idle.seed);
            return definition;
        }

        // The body, neck, head and foot as they stand now, read from the recipe's roles and the live transforms
        public bool TryGetAnchors(out EffectAnchors anchors)
        {
            return _assembly.TryGetAnchors(_cellSize, out anchors);
        }

        public void SetVisible(bool visible)
        {
            if (root)
            {
                root.gameObject.SetActive(visible);
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _assembly.Dispose();
        }
    }
}
