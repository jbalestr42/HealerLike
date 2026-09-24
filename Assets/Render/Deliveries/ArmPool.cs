using System;
using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Deliveries
{
    // The lianas a rig lends to its gestures and to the shots its unit fires. A lease takes one free arm; a chain
    // contact branches more arms from the last contact under the same lease, and they retract together.
    public class ArmPool : IDisposable, IDeliverySource
    {
        public static readonly int MaxArms = 8;
        // Swarm shots take an arm each, never more than this many at once
        public static readonly int MaxSwarm = 4;
        // A root that jumps further than this many cells in one frame was placed again, its gestures end
        static readonly float replantCells = 0.75f;
        // Two contacts closer than this land on the same point
        static readonly float samePointSquared = 0.000001f;

        // A projectile the pool follows until its delivery ends
        class Delivery
        {
            public int lease;
            public DeliveryStyle style;
            public Vector3? contact;
            public Transform projectile;
            public bool hasFreshContact;
        }

        readonly LianaArm[] _arms = new LianaArm[MaxArms];
        readonly int[] _tokens = new int[MaxArms];
        readonly int[] _definitions = new int[MaxArms];
        readonly Vector3?[] _branchRoots = new Vector3?[MaxArms];
        readonly Dictionary<int, Delivery> _deliveries = new Dictionary<int, Delivery>();
        CreatureRig _rig;
        Material _material;
        PrimitiveMeshes _meshes;
        int _nextToken;
        Vector3 _previousOrigin;
        bool _isPlaced;
        bool _isDisposed;

        // One arm per arm of the rig's recipe, more are made on demand up to the cap
        public void Init(CreatureRig rig, Material material, PrimitiveMeshes meshes)
        {
            _rig = rig;
            _material = material;
            _meshes = meshes;
            if (_rig == null || !_rig.root)
            {
                return;
            }

            for (int i = 0; i < _rig.recipe.arms.Length; i++)
            {
                CreateArm(i, i);
            }
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
            Quaternion rest = _rig.armRotation;
            for (int i = 0; i < MaxArms; i++)
            {
                if (_arms[i] == null)
                {
                    continue;
                }

                Vector3 shoulder = _rig.ArmSocket(_definitions[i]);
                if (_branchRoots[i].HasValue)
                {
                    shoulder = _branchRoots[i].Value;
                }

                _arms[i].Tick(deltaTime, shoulder, rest);
                if (_arms[i].isAvailable)
                {
                    _branchRoots[i] = null;
                    _tokens[i] = 0;
                    _arms[i].SetVisible(i < _rig.recipe.arms.Length);
                }
            }
        }

        // A heal reaches out and lands at once
        public void HealContact(Vector3 point)
        {
            int token = Begin(GestureKind.Heal, point);
            Contact(token, point);
        }

        public void CancelAll()
        {
            _deliveries.Clear();
            for (int i = 0; i < MaxArms; i++)
            {
                if (_arms[i] != null)
                {
                    _arms[i].End(_tokens[i]);
                    _tokens[i] = 0;
                }
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            foreach (LianaArm arm in _arms)
            {
                if (arm != null)
                {
                    arm.Dispose();
                }
            }
        }

        int Begin(GestureKind kind, Vector3 goal)
        {
            int slot = FreeSlot();
            if (slot < 0)
            {
                return 0;
            }

            if (++_nextToken == 0)
            {
                ++_nextToken;
            }

            // A heal lifts its arm in an arc, any other gesture reaches straight
            DeliveryStyle style = DeliveryStyle.Direct;
            if (kind == GestureKind.Heal)
            {
                style = DeliveryStyle.Arc;
            }

            _tokens[slot] = _nextToken;
            _branchRoots[slot] = null;
            _arms[slot].isDeliveryProfile = kind == GestureKind.Heal;
            _arms[slot].style = style;
            _arms[slot].SetVisible(true);
            _arms[slot].Begin(_nextToken, kind, goal);
            return _nextToken;
        }

        void SetTipGoal(int token, Vector3 goal)
        {
            if (token == 0)
            {
                return;
            }

            for (int i = 0; i < MaxArms; i++)
            {
                if (_tokens[i] == token && !_branchRoots[i].HasValue && _arms[i] != null)
                {
                    _arms[i].SetTipGoal(token, goal);
                }
            }
        }

        // A contact with a previous one branches a new arm from it under the same lease
        void Contact(int token, Vector3 goal, Vector3? previousContact = null)
        {
            if (token == 0)
            {
                CoalesceContact(goal);
                return;
            }

            if (previousContact.HasValue)
            {
                int slot = FreeSlot();
                if (slot < 0)
                {
                    CoalesceContact(goal);
                    return;
                }

                _tokens[slot] = token;
                _branchRoots[slot] = previousContact;
                _arms[slot].style = DeliveryStyle.ChainSync;
                _arms[slot].isDeliveryProfile = true;
                _arms[slot].SetVisible(true);
                _arms[slot].Begin(token, GestureKind.Attack, goal);
                _arms[slot].Contact(token, goal);
            }
            else
            {
                for (int i = 0; i < MaxArms; i++)
                {
                    if (_tokens[i] == token && !_branchRoots[i].HasValue && _arms[i] != null)
                    {
                        _arms[i].Contact(token, goal);
                    }
                }
            }
        }

        void End(int token)
        {
            if (token == 0)
            {
                return;
            }

            for (int i = 0; i < MaxArms; i++)
            {
                if (_tokens[i] == token && _arms[i] != null)
                {
                    _arms[i].End(token);
                }
            }
        }

        void CreateArm(int slot, int definitionIndex)
        {
            _definitions[slot] = definitionIndex;
            _arms[slot] = new LianaArm();
            _arms[slot].Init(_rig.GetArm(definitionIndex), _rig.root, _material, _meshes, _rig.cellSize);
            _arms[slot].Tick(0f, _rig.ArmSocket(definitionIndex), _rig.armRotation);
        }

        int FreeSlot()
        {
            if (_rig == null || _rig.recipe.arms.Length == 0)
            {
                return -1;
            }

            for (int i = 0; i < MaxArms; i++)
            {
                if (_arms[i] == null || _arms[i].isAvailable)
                {
                    if (_arms[i] == null)
                    {
                        CreateArm(i, i % _rig.recipe.arms.Length);
                    }

                    return i;
                }
            }

            return -1;
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
                    SetTipGoal(delivery.lease, delivery.projectile.position);
                }
            }
        }

        void CoalesceContact(Vector3 goal)
        {
            // Saturated same-position contacts renew an existing visual contact, without sharing
            // its lease token. A dropped observer can never end somebody else's chain.
            for (int i = 0; i < MaxArms; i++)
            {
                if (_tokens[i] != 0 && _arms[i] != null && !_arms[i].isAvailable
                    && (_arms[i].goal - goal).sqrMagnitude < samePointSquared)
                {
                    _arms[i].Contact(_arms[i].token, goal);
                    return;
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

            int leaseToken = Begin(GestureKind.Attack, goal);
            if (leaseToken == 0)
            {
                return false;
            }

            _deliveries.Add(token, new Delivery { lease = leaseToken, style = style, projectile = projectile });
            for (int i = 0; i < MaxArms; i++)
            {
                if (_tokens[i] == leaseToken)
                {
                    _arms[i].style = style;
                    _arms[i].isDeliveryProfile = true;
                }
            }

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

            Contact(delivery.lease, contactPosition, branchFrom);
            delivery.contact = contactPosition;
            delivery.hasFreshContact = true;
        }

        public void EndDelivery(int token)
        {
            if (!_deliveries.TryGetValue(token, out Delivery delivery))
            {
                return;
            }

            End(delivery.lease);
            _deliveries.Remove(token);
        }

        #endregion
    }
}
