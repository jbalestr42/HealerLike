using System;
using System.Collections.Generic;
using HealerLike.Render.Grammar;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // Presentation-only observation. Attribute callbacks mark dirty; composition happens once after simulation.
    public sealed class CreatureEvolution : IDisposable
    {
        static readonly AttributeType[] types =
        {
            AttributeType.HealthMax, AttributeType.AttackRate, AttributeType.Range,
            AttributeType.Damage, AttributeType.HealPower, AttributeType.FlatArmor,
            AttributeType.PercentArmor, AttributeType.HitArmor, AttributeType.Speed,
            AttributeType.CriticalChance, AttributeType.CriticalMultiplier, AttributeType.CriticalChanceResist,
            AttributeType.Vulnerability
        };
        readonly Dictionary<AttributeType, Attribute> _attributes = new Dictionary<AttributeType, Attribute>();
        AttributeManager _manager;
        EntityData _data;
        Entity.EntityType _side;
        UnitChannels _accepted;
        bool _hasAccepted;
        bool _dirty;

        public void Init(EntityData data, Entity.EntityType side, AttributeManager manager)
        {
            Dispose();
            _data = data;
            _side = side;
            _manager = manager;
            _dirty = true;
            RefreshBindings();
        }

        // New item attributes may be added after spawn. Binding discovery is cheap and never builds geometry.
        void RefreshBindings()
        {
            if (!_manager) return;
            foreach (AttributeType type in types)
            {
                if (_attributes.ContainsKey(type) || !_manager.Has(type)) continue;
                Attribute attribute = _manager.Get(type);
                _attributes.Add(type, attribute);
                attribute.AddOnValueChangedListener(OnChanged);
                _dirty = true;
            }
        }

        void OnChanged(Attribute attribute) => _dirty = true;

        public bool TryRead(out UnitChannels channels)
        {
            RefreshBindings();
            channels = _accepted;
            if (!_dirty || !_data) return false;
            _dirty = false;
            channels = Read(_data, _side, _attributes);
            return !_hasAccepted || !channels.Equals(_accepted);
        }

        public UnitChannels ReadCurrent()
        {
            RefreshBindings();
            return Read(_data, _side, _attributes);
        }

        // Accept only after a successful build. A rejected candidate can retry on the next actual change.
        public void Accept(UnitChannels channels)
        {
            _accepted = channels;
            _hasAccepted = true;
        }

        public static UnitChannels Read(EntityData data, Entity.EntityType side,
            IReadOnlyDictionary<AttributeType, Attribute> attributes)
        {
            UnitChannels channels = LookDerivation.Channels(data, side);
            if (TryValue(attributes, AttributeType.HealthMax, out float health))
                channels.mass = LookDerivation.Mass(health);
            if (TryValue(attributes, AttributeType.Range, out float range))
                channels.reach = range <= LookDerivation.ShortRange ? ReachBand.Short
                    : range <= LookDerivation.MidRange ? ReachBand.Mid : ReachBand.Long;
            ASkillFactory primary = LookDerivation.Primary(data);
            // Both runtime skills use AttackRate as seconds per trigger, despite the attribute's name.
            if ((primary is ShootProjectileSkillFactory || primary is AreaOfEffectSkillFactory)
                && TryValue(attributes, AttributeType.AttackRate, out float cadence))
                channels.stem = LookDerivation.Stem(cadence);

            // Preserve structural accessories such as a second delivery. Free slots express live stat changes.
            if (channels.accessory == AccessoryKind.None)
            {
                int direction = UpgradeDirection(attributes);
                if (direction != 0)
                    channels.accessory = direction > 0 ? AccessoryKind.SmallTorus : AccessoryKind.ConeCrown;
            }
            return channels;
        }

        static int UpgradeDirection(IReadOnlyDictionary<AttributeType, Attribute> attributes)
        {
            bool improved = false;
            foreach (AttributeType type in types)
            {
                if (!attributes.TryGetValue(type, out Attribute attribute) || !float.IsFinite(attribute.Value)
                    || !float.IsFinite(attribute.BaseValue)) continue;
                float delta = attribute.Value - attribute.BaseValue;
                float threshold = Mathf.Max(0.001f, Mathf.Abs(attribute.BaseValue) * 0.02f);
                if (Mathf.Abs(delta) <= threshold) continue;
                if (type == AttributeType.AttackRate || type == AttributeType.Vulnerability) delta = -delta;
                if (delta < 0f) return -1;
                improved = true;
            }
            return improved ? 1 : 0;
        }

        static bool TryValue(IReadOnlyDictionary<AttributeType, Attribute> attributes, AttributeType type,
            out float value)
        {
            value = 0f;
            if (!attributes.TryGetValue(type, out Attribute attribute)) return false;
            value = attribute.Value;
            return float.IsFinite(value) && value >= 0f;
        }

        public void Dispose()
        {
            foreach (Attribute attribute in _attributes.Values)
                attribute.RemoveOnValueChangedListener(OnChanged);
            _attributes.Clear();
            _manager = null;
            _data = null;
            _hasAccepted = false;
            _dirty = false;
        }
    }
}
