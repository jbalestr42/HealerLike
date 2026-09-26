using System;
using UnityEngine;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Deliveries
{
    // The arms of one rig, each lent under a lease token while a gesture has it out. A chain contact branches more
    // arms from the last contact under the same lease, and they all retract together.
    public class ArmLeases : IDisposable
    {
        // Two contacts closer than this land on the same point
        static readonly float samePointSquared = 0.000001f;

        readonly LianaArm[] _arms = new LianaArm[ArmPool.MaxArms];
        readonly int[] _tokens = new int[ArmPool.MaxArms];
        readonly int[] _definitions = new int[ArmPool.MaxArms];
        readonly Vector3?[] _branchRoots = new Vector3?[ArmPool.MaxArms];
        readonly bool[] _refresh = new bool[ArmPool.MaxArms];
        CreatureRig _rig;
        Material _material;
        PrimitiveMeshes _meshes;
        DeliveryVocabulary _vocabulary;
        int _nextToken;
        bool _isDisposed;

        // Every arm slot, made or not: Get returns null for a slot that has no arm yet
        public int count { get { return _arms.Length; } }

        public LianaArm Get(int index)
        {
            return index >= 0 && index < _arms.Length ? _arms[index] : null;
        }

        // A held delivery keeps its arm and lease. Its next rest uses the edited recipe.
        public void Refresh()
        {
            for (int i = 0; i < ArmPool.MaxArms; i++)
            {
                _refresh[i] = true;
            }
        }

        // One arm per arm of the rig's recipe, more are made on demand up to the cap
        public void Init(CreatureRig rig, Material material, PrimitiveMeshes meshes, DeliveryVocabulary vocabulary)
        {
            _rig = rig;
            _material = material;
            _meshes = meshes;
            _vocabulary = vocabulary;
            for (int i = 0; i < _rig.recipe.arms.Length; i++)
            {
                CreateArm(i, i);
            }
        }

        // An arm back at rest is free again, beyond the recipe's own arms it hides
        public void Tick(float deltaTime)
        {
            Quaternion rest = _rig.armRotation;
            for (int i = 0; i < ArmPool.MaxArms; i++)
            {
                if (_refresh[i] && (_arms[i] == null || _arms[i].isAvailable))
                {
                    if (_arms[i] != null)
                    {
                        _arms[i].Dispose();
                    }
                    _arms[i] = null;
                    _tokens[i] = 0;
                    _branchRoots[i] = null;
                    _refresh[i] = false;
                    if (i < _rig.recipe.arms.Length)
                    {
                        CreateArm(i, i);
                    }
                }
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

        // The lease token, zero when every arm is out
        public int Begin(GestureKind kind, Vector3 goal)
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

        // A shot draws its lease as its style, a rod or an arc to the projectile
        public void SetStyle(int lease, DeliveryStyle style)
        {
            for (int i = 0; i < ArmPool.MaxArms; i++)
            {
                if (_tokens[i] == lease && _arms[i] != null)
                {
                    _arms[i].style = style;
                    _arms[i].isDeliveryProfile = true;
                }
            }
        }

        public void SetTipGoal(int lease, Vector3 goal)
        {
            if (lease == 0)
            {
                return;
            }

            for (int i = 0; i < ArmPool.MaxArms; i++)
            {
                if (_tokens[i] == lease && !_branchRoots[i].HasValue && _arms[i] != null)
                {
                    _arms[i].SetTipGoal(lease, goal);
                }
            }
        }

        // A contact with a previous one branches a new arm from it under the same lease
        public void Contact(int lease, Vector3 goal, Vector3? previousContact = null)
        {
            if (lease == 0)
            {
                CoalesceContact(goal);
                return;
            }

            if (!previousContact.HasValue)
            {
                for (int i = 0; i < ArmPool.MaxArms; i++)
                {
                    if (_tokens[i] == lease && !_branchRoots[i].HasValue && _arms[i] != null)
                    {
                        _arms[i].Contact(lease, goal);
                    }
                }
                return;
            }

            int slot = FreeSlot();
            if (slot < 0)
            {
                CoalesceContact(goal);
                return;
            }

            _tokens[slot] = lease;
            _branchRoots[slot] = previousContact;
            _arms[slot].style = DeliveryStyle.ChainSync;
            _arms[slot].isDeliveryProfile = true;
            _arms[slot].SetVisible(true);
            _arms[slot].Begin(lease, GestureKind.Attack, goal);
            _arms[slot].Contact(lease, goal);
        }

        public void End(int lease)
        {
            if (lease == 0)
            {
                return;
            }

            for (int i = 0; i < ArmPool.MaxArms; i++)
            {
                if (_tokens[i] == lease && _arms[i] != null)
                {
                    _arms[i].End(lease);
                }
            }
        }

        // The tips of a lease's arms, chain branches included, take the colour until they are back at rest
        public void SetAccent(int lease, Color colour)
        {
            for (int i = 0; i < ArmPool.MaxArms; i++)
            {
                if (_tokens[i] == lease && _arms[i] != null)
                {
                    _arms[i].SetTipAccent(lease, colour);
                }
            }
        }

        public void CancelAll()
        {
            for (int i = 0; i < ArmPool.MaxArms; i++)
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

        void CreateArm(int slot, int definitionIndex)
        {
            _definitions[slot] = definitionIndex;
            _arms[slot] = new LianaArm();
            _arms[slot].Init(_rig.GetArm(definitionIndex), _rig.root, _material, _meshes, _vocabulary, _rig.cellSize);
            _arms[slot].Tick(0f, _rig.ArmSocket(definitionIndex), _rig.armRotation);
        }

        int FreeSlot()
        {
            if (_rig.recipe.arms.Length == 0)
            {
                return -1;
            }

            for (int i = 0; i < ArmPool.MaxArms; i++)
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

        void CoalesceContact(Vector3 goal)
        {
            // Saturated same-position contacts renew an existing visual contact, without sharing
            // its lease token. A dropped observer can never end somebody else's chain.
            for (int i = 0; i < ArmPool.MaxArms; i++)
            {
                if (_tokens[i] != 0 && _arms[i] != null && !_arms[i].isAvailable
                    && (_arms[i].goal - goal).sqrMagnitude < samePointSquared)
                {
                    _arms[i].Contact(_arms[i].token, goal);
                    return;
                }
            }
        }
    }
}
