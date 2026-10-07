using System;
using System.Collections.Generic;
using HealerLike.Render.Grammar;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // Presentation-only observation. Attribute callbacks mark dirty; composition happens once after simulation.
    public sealed class CreatureEvolution : IDisposable
    {
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
            if (!_manager)
            {
                return;
            }

            foreach (AttributeType type in LiveUnitDerivation.observedAttributes)
            {
                if (_attributes.ContainsKey(type) || !_manager.Has(type))
                {
                    continue;
                }

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
            if (!_dirty || !_data)
            {
                return false;
            }

            _dirty = false;
            channels = LiveUnitDerivation.Read(_data, _side, _attributes);
            return !_hasAccepted || !channels.Equals(_accepted);
        }

        public UnitChannels ReadCurrent()
        {
            RefreshBindings();
            return LiveUnitDerivation.Read(_data, _side, _attributes);
        }

        // Accept only after a successful build. A rejected candidate can retry on the next actual change.
        public void Accept(UnitChannels channels)
        {
            _accepted = channels;
            _hasAccepted = true;
        }

        public void Dispose()
        {
            foreach (Attribute attribute in _attributes.Values)
            {
                attribute.RemoveOnValueChangedListener(OnChanged);
            }

            _attributes.Clear();
            _manager = null;
            _data = null;
            _hasAccepted = false;
            _dirty = false;
        }
    }
}
