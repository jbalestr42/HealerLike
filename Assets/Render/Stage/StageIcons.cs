using System;
using HealerLike.Render.Creatures;
using HealerLike.Render.Spells;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // The Toolkit knows only its optional provider contracts, never Render's grammar or capture implementation.
    public class StageIcons : IToolkitIconProvider, IToolkitDataIconProvider, IDisposable
    {
        readonly CreaturePortraits _creatures;
        readonly SpellIcons _spells;
        bool _disposed;
        bool _invalidating;
        public event Action Changed;

        public StageIcons(CreaturePortraits creatures, SpellIcons spells)
        {
            _creatures = creatures;
            _spells = spells;
            _creatures.Changed += OnChanged;
            _spells.Changed += OnChanged;
        }

        public Texture2D GetCreatureIcon(EntityData data, Entity.EntityType side)
        {
            return _disposed ? null : _creatures.GetCreatureIcon(data, side);
        }

        public Texture2D GetDataIcon(object source)
        {
            return _disposed ? null : _spells.GetIcon(source);
        }

        public void Invalidate()
        {
            if (!_disposed)
            {
                _invalidating = true;
                try
                {
                    _creatures.Invalidate();
                    _spells.Invalidate();
                }
                finally
                {
                    _invalidating = false;
                }
                OnChanged();
            }
        }

        void OnChanged()
        {
            if (!_invalidating)
            {
                Changed?.Invoke();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            _creatures.Changed -= OnChanged;
            _spells.Changed -= OnChanged;
            _creatures.Dispose();
            _spells.Dispose();
            Changed = null;
        }
    }
}
