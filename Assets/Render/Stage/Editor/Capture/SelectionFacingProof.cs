using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HealerLike.Render.Creatures;
using HealerLike.Render.Deliveries;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Stage
{
    [Serializable]
    public class SelectionFacingProof
    {
        public string revision = StagePlay.ReadRevision();
        public string baseline = "431af58a0c7ad020e8b5c9de2db45662e324acb4";
        public string condition = "Actual RenderStage Main+HUD. ChainLightningEntity spawned via EntityManager for "
            + "both sides in planning. Selection uses explicit SelectableEntity.Select/UnSelect, not physical input. "
            + "Facing uses synthetic target objects assigned to the real TargetProvider list with acquisition disabled; "
            + "CreatureBuilder reads it in production LateUpdate. Plant synthetic held delivery uses the normal source API; armless mineral keeps an anatomical lease only. "
            + "Camera fitted once per creature and fixed throughout. Paused PNG samples are not performance evidence.";
        public bool passed;
        public List<Frame> frames = new List<Frame>();
        [Serializable]
        public class Frame
        {
            public string subject, file, sourceId, targetIdentity;
            public int turn, gameFrame;
            public bool hasDeliveryArm;
            public float elapsed, gameTime, targetAngle, attachmentError;
            public Vector3 target, forward, outlet, deliveryRoot, ownerPosition, cameraPosition;
            public Quaternion ownerRotation, cameraRotation;
        }
    }

    // Capture-only controlled inputs. No production targeting code or entity rotation is changed.
    public sealed class SelectionFacingControl : IDisposable
    {
        static readonly FieldInfo targets = typeof(TargetProvider).GetField("_targets", BindingFlags.Instance | BindingFlags.NonPublic);
        readonly Entity _entity;
        readonly CreatureBuilder _host;
        readonly StagePresentationOutput _output;
        readonly object _previousTargets;
        readonly bool _wasEnabled;
        readonly Quaternion _ownerRotation;
        readonly Vector3 _ownerPosition;
        readonly GameObject[] _targets = { new GameObject("Synthetic target A"), new GameObject("Synthetic target B") };
        readonly int _token = 73491;
        readonly string _sourceId;
        readonly LianaArm _arm;
        readonly CastSourceLease _lease;
        GameObject _target;

        public SelectionFacingControl(Entity entity, CreatureBuilder host, StagePresentationOutput output)
        {
            _entity = entity;
            _host = host;
            _output = output;
            _previousTargets = targets.GetValue(entity.targetProvider);
            _wasEnabled = entity.targetProvider.isEnabled;
            entity.targetProvider.isEnabled = false;
            _ownerRotation = entity.transform.rotation;
            _ownerPosition = entity.transform.position;
            _lease = new CastSourceLease(host.rig, 0);
            _sourceId = _lease.sourceId;
            if (host.rig.armCount > 0)
            {
                _output.Check(host.BeginDelivery(_token, DeliveryStyle.Direct, null, _ownerPosition + Vector3.up * 2f),
                    "Synthetic delivery acquired through real host API");
                _arm = host.GetDeliveryArm(_token);
                _output.Check(_arm != null, "Held source arm exists");
            }

        }

        public void Target(Vector3 direction, int turn)
        {
            _target = _targets[turn % 2];
            _target.transform.position = _ownerPosition + direction.normalized * 5f;
            targets.SetValue(_entity.targetProvider, new List<GameObject> { _target });
        }

        public void ClearTarget()
        {
            _target = null;
            targets.SetValue(_entity.targetProvider, new List<GameObject>());
        }

        public SelectionFacingProof.Frame Sample(string subject, int turn, float elapsed, string file, Camera camera)
        {
            _output.Check(_entity.transform.rotation.Equals(_ownerRotation)
                && _entity.transform.position.Equals(_ownerPosition), "Gameplay owner pose remains exact");
            _output.Check(ReferenceEquals(_arm, _host.GetDeliveryArm(_token)), "Retargeting keeps the same delivery lease");
            _output.Check(CreatureSources.Resolve(_host.rig, _sourceId, out Vector3 outlet), "Original source identity resolves");
            _output.Check(_lease.TryGet(out Vector3 leased) && leased.Equals(outlet), "Held anatomical identity tracks geometry");
            float error = _arm != null ? Vector3.Distance(outlet, _arm.Joint(0)) : Vector3.Distance(outlet, leased);
            _output.Check(error < 0.0001f, "Active delivery follows rotating anatomical outlet");
            Vector3 forward = _host.rig.root.Find("Sway").forward;
            forward.y = 0f;
            Vector3 direction = _target ? _target.transform.position - _ownerPosition : -camera.transform.forward;
            direction.y = 0f;
            return new SelectionFacingProof.Frame
            {
                subject = subject, file = file != null ? file + ".png" : null, turn = turn, elapsed = elapsed,
                gameTime = Time.time, gameFrame = Time.frameCount, targetAngle = Vector3.Angle(forward, direction),
                target = _target ? _target.transform.position : Vector3.zero,
                targetIdentity = _target ? _target.name : "none", forward = forward,
                sourceId = _sourceId, outlet = outlet, deliveryRoot = _arm != null ? _arm.Joint(0) : leased,
                hasDeliveryArm = _arm != null, attachmentError = error,
                ownerPosition = _entity.transform.position, ownerRotation = _entity.transform.rotation,
                cameraPosition = camera.transform.position, cameraRotation = camera.transform.rotation
            };
        }

        public void Dispose()
        {
            _host.EndDelivery(_token);
            _lease.Dispose();
            targets.SetValue(_entity.targetProvider, _previousTargets);
            _entity.targetProvider.isEnabled = _wasEnabled;
            foreach (GameObject target in _targets) Object.Destroy(target);
        }
    }
}
