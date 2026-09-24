using System;
using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Deliveries
{
    // The arms a rig lends to its heals and to the shots its unit fires. A shot's delivery token maps to one lease
    // of the arms; the pool follows the projectile, keeps a chain shot's path and caps the swarm.
    public class ArmPool : IDisposable, IDeliverySource
    {
        public static readonly int MaxArms = 8;
        // Swarm shots take an arm each, never more than this many at once
        public static readonly int MaxSwarm = 4;
        // A root that jumps further than this many cells in one frame was placed again, its gestures end
        static readonly float replantCells = 0.75f;

        // A projectile the pool follows until its delivery ends
        class Delivery
        {
            public int lease;
            public DeliveryStyle style;
            public Vector3? contact;
            public Transform projectile;
            public bool hasFreshContact;
        }

        readonly ArmLeases _leases = new ArmLeases();
        readonly Dictionary<int, Delivery> _deliveries = new Dictionary<int, Delivery>();
        CreatureRig _rig;
        Vector3 _previousOrigin;
        bool _isPlaced;

        public void Init(CreatureRig rig, Material material, PrimitiveMeshes meshes, DeliveryVocabulary vocabulary)
        {
            _rig = rig;
            _leases.Init(rig, material, meshes, vocabulary);
        }

        public void Refresh()
        {
            _leases.Refresh();
        }

        // After the rig's own tick, which places the root and the sway the arms hang from
        public void Tick(float deltaTime)
        {
            if (_rig == null || !_rig.root)
            {
                return;
            }

            Vector3 origin = _rig.root.position;
            if (_isPlaced && Vector3.Distance(origin, _previousOrigin) > _rig.cellSize * replantCells)
            {
                CancelAll();
            }

            _previousOrigin = origin;
            _isPlaced = true;
            FollowProjectiles();
            _leases.Tick(deltaTime);
        }

        // A heal reaches out and lands at once
        public void HealContact(Vector3 point)
        {
            int lease = _leases.Begin(GestureKind.Heal, point);
            _leases.Contact(lease, point);
        }

        // The tips of a delivery's arms take its colour until they are back at rest
        public void SetAccent(int token, Color colour)
        {
            if (_deliveries.TryGetValue(token, out Delivery delivery))
            {
                _leases.SetAccent(delivery.lease, colour);
            }
        }

        public void CancelAll()
        {
            _deliveries.Clear();
            _leases.CancelAll();
        }

        public void Dispose()
        {
            _leases.Dispose();
        }

        // The pool reads the projectile itself, so no observer has to update before it
        void FollowProjectiles()
        {
            foreach (Delivery delivery in _deliveries.Values)
            {
                if (delivery.hasFreshContact)
                {
                    delivery.hasFreshContact = false;
                    continue;
                }

                if (delivery.projectile && !(delivery.style == DeliveryStyle.ChainSync && delivery.contact.HasValue))
                {
                    _leases.SetTipGoal(delivery.lease, delivery.projectile.position);
                }
            }
        }

        #region IDeliverySource

        public bool BeginDelivery(int token, DeliveryStyle style, Transform projectile, Vector3 intendedEnd)
        {
            if (token == 0 || style == DeliveryStyle.Thrown || _deliveries.ContainsKey(token))
            {
                return false;
            }

            if (style == DeliveryStyle.Swarm)
            {
                int count = 0;
                foreach (Delivery item in _deliveries.Values)
                {
                    if (item.style == style)
                    {
                        count++;
                    }
                }

                if (count >= MaxSwarm)
                {
                    return false;
                }
            }

            // The tip first reaches for the shot itself, or where it is headed when there is none yet
            Vector3 goal = intendedEnd;
            if (projectile)
            {
                goal = projectile.position;
            }

            int lease = _leases.Begin(GestureKind.Attack, goal);
            if (lease == 0)
            {
                return false;
            }

            _deliveries.Add(token, new Delivery { lease = lease, style = style, projectile = projectile });
            _leases.SetStyle(lease, style);
            return true;
        }

        // A chain shot keeps its path: every contact after the first branches from the one before
        public void ContactDelivery(int token, Vector3 contactPosition, GameObject target)
        {
            if (!_deliveries.TryGetValue(token, out Delivery delivery))
            {
                return;
            }

            Vector3? branchFrom = null;
            if (delivery.style == DeliveryStyle.ChainSync)
            {
                branchFrom = delivery.contact;
            }

            _leases.Contact(delivery.lease, contactPosition, branchFrom);
            delivery.contact = contactPosition;
            delivery.hasFreshContact = true;
        }

        public void EndDelivery(int token)
        {
            if (!_deliveries.TryGetValue(token, out Delivery delivery))
            {
                return;
            }

            _leases.End(delivery.lease);
            _deliveries.Remove(token);
        }

        #endregion
    }
}
