using UnityEngine;
using HealerLike.Render.Deliveries;
using HealerLike.Render.Stones;

namespace HealerLike.Render.Creatures
{
    // What the two views of a unit share: the rig, the arms it lends to gestures and shots, the registry entry that
    // routes the source's health outcomes here, and the contracts projectiles and effects reach it through
    public abstract class ARigHost : MonoBehaviour, IHealthVisualSink, IDeliverySource, IEffectAnchors, IDeliveryAccent
    {
        ArmPool _pool;
        CreatureAttachment _attachment;
        public Transform presentation => _attachment != null ? _attachment.root : null;
        RenderRegistry _registeredRegistry;
        GameObject _registeredSource;
        CreatureRig _rig;
        public CreatureRig rig
        {
            get { return _rig; }
        }

        // The arm slots the rig lends to gestures and shots, resting ones included; null where none is made yet
        public int armCount { get { return _pool != null ? _pool.armCount : 0; } }

        public LianaArm GetArm(int index)
        {
            return _pool != null ? _pool.GetArm(index) : null;
        }

        // Builds the rig and its arms once, a later call keeps them
        protected bool BuildRig(
            CreatureRecipe recipe,
            Transform parent,
            Material material,
            Material bodyMaterial,
            PrimitiveMeshes meshes,
            DeliveryVocabulary vocabulary,
            float cellSize
        )
        {
            if (_rig != null)
            {
                return true;
            }

            if (!parent)
            {
                return false;
            }

            _attachment = new CreatureAttachment(parent);
            CreatureRig created = new CreatureRig();
            if (!created.Init(recipe, _attachment.root, material, bodyMaterial, meshes, cellSize, parent))
            {
                _attachment.Dispose();
                _attachment = null;
                return false;
            }

            foreach (StoneGroundDisc disc in GetComponentsInChildren<StoneGroundDisc>(true))
            {
                disc.Attach(_attachment);
            }

            _rig = created;
            _pool = new ArmPool();
            _pool.Init(_rig, material, meshes, vocabulary);
            return true;
        }

        // The body first, the arms hang from where it now stands
        protected void TickRig(float time, float deltaTime, FootFrame frame)
        {
            if (_rig == null)
            {
                return;
            }

            SyncGeometry();
            _rig.Tick(time, deltaTime, frame);
            _pool.Tick(deltaTime);
        }

        public void SyncGeometry()
        {
            if (_attachment != null)
            {
                _attachment.Sync();
            }
        }

        protected void SetRigVisible(bool visible)
        {
            if (_attachment != null)
            {
                _attachment.SetVisible(visible);
            }
            if (_rig != null)
            {
                _rig.SetVisible(visible);
            }
        }

        protected void CancelGestures()
        {
            if (_pool != null)
            {
                _pool.CancelAll();
            }
        }

        protected void RefreshArms()
        {
            if (_pool != null)
            {
                _pool.Refresh();
            }
        }

        // Every gesture ends and the body hides, as when the view is switched off
        protected void HideRig()
        {
            if (_rig == null)
            {
                return;
            }

            _pool.CancelAll();
            SetRigVisible(false);
        }

        protected void ReleaseRig()
        {
            if (_rig != null)
            {
                _pool.Dispose();
                _rig.Dispose();
            }

            _rig = null;
            _pool = null;
            if (_attachment != null)
            {
                _attachment.Dispose();
                _attachment = null;
            }
        }

        // A heal pulses the crown and reaches an arm to the target
        protected void HealContact(GameObject target)
        {
            if (_rig == null)
            {
                return;
            }

            SyncGeometry();
            _rig.Heal();
            _pool.HealContact(RenderTargets.Point(target));
        }

        // The registry routes the source's health outcomes here, a new registry moves the entry
        protected void Register(RenderRegistry registry, GameObject source)
        {
            if (_registeredRegistry == registry)
            {
                return;
            }

            Unregister();
            _registeredRegistry = registry;
            _registeredSource = source;
            if (_registeredRegistry != null)
            {
                _registeredRegistry.Register(_registeredSource, this);
            }
        }

        protected void Unregister()
        {
            if (_registeredRegistry != null)
            {
                _registeredRegistry.Unregister(_registeredSource, this);
            }

            _registeredRegistry = null;
            _registeredSource = null;
        }

        #region IHealthVisualSink
        public abstract void OnHealthResolved(GameObject target, float value, bool critical);
        #endregion
        #region IDeliverySource
        public bool BeginDelivery(int token, DeliveryStyle style, Transform projectile, Vector3 intendedEnd)
        {
            SyncGeometry();
            return isActiveAndEnabled && _rig != null && _pool.BeginDelivery(token, style, projectile, intendedEnd);
        }

        public void ContactDelivery(int token, Vector3 contactPosition, GameObject target)
        {
            if (_pool != null)
            {
                _pool.ContactDelivery(token, contactPosition, target);
            }
        }

        public void EndDelivery(int token)
        {
            if (_pool != null)
            {
                _pool.EndDelivery(token);
            }
        }

        #endregion
        #region IDeliveryAccent
        public void SetDeliveryAccent(int token, Color colour)
        {
            if (_pool != null)
            {
                _pool.SetAccent(token, colour);
            }
        }

        #endregion
        #region IEffectAnchors
        public virtual bool TryGetAnchors(out EffectAnchors anchors)
        {
            if (_rig == null)
            {
                anchors = new EffectAnchors();
                return false;
            }

            SyncGeometry();
            return _rig.TryGetAnchors(out anchors);
        }
        #endregion
    }
}
