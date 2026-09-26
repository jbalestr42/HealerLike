using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    // The shard a stone throws for every shot. The armless stone rig refuses every delivery, so this claims them all
    public class StoneThrow : MonoBehaviour, IDeliverySource
    {
        class Delivery
        {
            public StoneFragmentPool.ShardLease shardLease;
            public Transform projectile;
            public StoneMeshCache.Lease lease;
        }

        // Seed salts, apart from the body's so a shard never shares a random stream with a hit
        static readonly uint shardMeshSalt = 701;
        static readonly uint contactSalt = 801;
        static readonly StoneSettings shardShape = StonePresets.Shape(0.15f, 1.7f, 0.65f, 0.18f, 0);

        readonly Dictionary<int, Delivery> _deliveries = new Dictionary<int, Delivery>();
        readonly List<int> _endedDeliveries = new List<int>();
        CreatureBuilder _builder;
        StoneEffects _effects;
        LookPalette _palette;
        uint _seed;
        uint _contactIndex;
        bool _isEnabled;

        public void Init(CreatureBuilder builder, StoneEffects effects, LookPalette palette, uint seed)
        {
            EndAll();
            _builder = builder;
            _effects = effects;
            _palette = palette;
            _seed = seed;
            _contactIndex = 0;
            _isEnabled = true;
        }

        // A collapsed stone throws nothing until it is set up again
        public void Enable(bool isEnabled)
        {
            _isEnabled = isEnabled;
            if (!isEnabled)
            {
                EndAll();
            }
        }

        // After the projectile's own Update has moved it, so the shard follows where it is this frame
        void LateUpdate()
        {
            _endedDeliveries.Clear();
            foreach (KeyValuePair<int, Delivery> pair in _deliveries)
            {
                Transform projectile = pair.Value.projectile;
                if (projectile == null || !projectile.gameObject.activeInHierarchy || !pair.Value.shardLease.shard)
                {
                    _endedDeliveries.Add(pair.Key);
                }
                else
                {
                    Follow(pair.Value, projectile.position);
                }
            }

            foreach (int token in _endedDeliveries)
            {
                EndDelivery(token);
            }
        }

        // The highest head throws, or the highest part of a stone that has no head
        Transform ThrowingPart()
        {
            CreatureRig rig = _builder != null ? _builder.rig : null;
            if (rig == null)
            {
                return null;
            }

            IReadOnlyList<Transform> partTransforms = rig.partTransforms;
            Transform best = null;
            bool isBestHead = false;
            for (int i = 0; i < partTransforms.Count; i++)
            {
                Transform part = partTransforms[i];
                if (!part.gameObject.activeInHierarchy)
                {
                    continue;
                }

                bool isHead = rig.parts[i].role == PartRole.Head;
                bool isHigher = best == null || part.position.y > best.position.y;
                if ((isHead && !isBestHead) || (isHead == isBestHead && isHigher))
                {
                    best = part;
                    isBestHead = isHead;
                }
            }
            return best;
        }

        void Follow(Delivery delivery, Vector3 projectilePosition)
        {
            Transform shard = delivery.shardLease.shard;
            Vector3 travel = projectilePosition - shard.position;
            if (travel.sqrMagnitude > 0.00000001f)
            {
                shard.rotation = Quaternion.FromToRotation(Vector3.up, travel.normalized);
            }
            shard.position = projectilePosition;
        }

        // A thrown shard is a piece of the stone's body
        Color ShardColour()
        {
            if (_palette == null)
            {
                Debug.LogError("[StoneThrow] No palette.");
                return Color.magenta;
            }
            return _palette.Colour(ColourRole.Body, EffectFamily.Damage, LookSide.Stone);
        }

        void EndAll()
        {
            _endedDeliveries.Clear();
            _endedDeliveries.AddRange(_deliveries.Keys);
            foreach (int token in _endedDeliveries)
            {
                EndDelivery(token);
            }
        }

        void OnDisable()
        {
            EndAll();
        }

        void OnDestroy()
        {
            EndAll();
        }

        #region IDeliverySource

        // A stone throws a shard whatever the style and the family: the shard shape and colour are the stone's own
        public bool BeginDelivery(int token, DeliveryStyle style, Transform projectile, Vector3 intendedEnd)
        {
            if (!isActiveAndEnabled || !_isEnabled || projectile == null || _effects == null
                || _deliveries.ContainsKey(token))
            {
                return false;
            }

            Transform thrower = ThrowingPart();
            if (thrower == null)
            {
                return false;
            }

            uint shardSeed = SeededRandom.ForPart(_seed, shardMeshSalt);
            StoneMeshCache.Lease lease = _effects.stoneMeshes.Acquire(shardSeed, shardShape);
            if (lease == null)
            {
                return false;
            }

            StoneFragmentPool.ShardLease shardLease = _effects.BorrowShard(lease.mesh, ShardColour());
            if (shardLease == null)
            {
                lease.Dispose();
                return false;
            }

            Transform shard = shardLease.shard;
            shard.position = thrower.GetComponent<Renderer>().bounds.center;
            shard.rotation = Quaternion.identity;
            Delivery delivery = new Delivery();
            delivery.shardLease = shardLease;
            delivery.projectile = projectile;
            delivery.lease = lease;
            _deliveries.Add(token, delivery);
            return true;
        }

        public void ContactDelivery(int token, Vector3 contactPosition, GameObject target)
        {
            if (!_deliveries.ContainsKey(token))
            {
                return;
            }

            if (_effects != null)
            {
                _contactIndex++;
                uint seed = SeededRandom.ForPart(_seed, _contactIndex + contactSalt);
                StoneEmitters.ThrownContact(_effects, contactPosition, seed);
            }
            EndDelivery(token);
        }

        public void EndDelivery(int token)
        {
            if (!_deliveries.TryGetValue(token, out Delivery delivery))
            {
                return;
            }

            _deliveries.Remove(token);
            delivery.shardLease.Dispose();
            delivery.lease.Dispose();
        }

        #endregion
    }
}
