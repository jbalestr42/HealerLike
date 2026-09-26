using System;
using System.Collections.Generic;
using HealerLike.Render.Zones;
using UnityEngine;

namespace HealerLike.Render.Grass
{
    // What everything in the game tells the grass, and the one place it tells it. A one-shot plays an effect
    // once at a point or along a line and fades on its own; a held effect follows its source through a handle
    // for as long as the source keeps it; a body presses the grass with its own shape. The zones the tuft compute
    // reads also move the grass through the vocabulary. Each frame the board's grass collects it all as stamps.
    public class Ground : IDisposable
    {
        // The most one-shots playing at once; a new one past it replaces the oldest
        public static readonly int OneShotCapacity = 256;

        struct OneShot
        {
            public GroundEffect effect;
            public Vector2 from;
            public Vector2 to;
            public float radius;
            public float strength;
            public float age;
        }

        readonly OneShot[] _oneShots = new OneShot[OneShotCapacity];
        readonly List<GroundHandle> _held = new List<GroundHandle>();
        readonly List<IGroundBody> _bodies = new List<IGroundBody>();
        int _oneShotCount;

        readonly bool _ownsVocabulary;
        bool _isDisposed;
        GroundVocabulary _vocabulary;
        public GroundVocabulary vocabulary { get { return _vocabulary; } }

        public int oneShotCount { get { return _oneShotCount; } }
        public int heldCount { get { return _held.Count; } }
        public int bodyCount { get { return _bodies.Count; } }

        // Without a vocabulary the defaults stand in
        public Ground(GroundVocabulary vocabulary = null)
        {
            _ownsVocabulary = vocabulary == null;
            _vocabulary = vocabulary != null ? vocabulary : GroundVocabulary.CreateDefault();
        }

        // Once at a point, at a radius; a ring travels out to it
        public void Play(GroundEffect effect, Vector3 position, float radius, float strength = 1f)
        {
            Vector2 at = new Vector2(position.x, position.z);
            Add(effect, at, at, radius, strength);
        }

        // Once along the ground from one point to another
        public void Play(GroundEffect effect, Vector3 from, Vector3 to, float strength = 1f)
        {
            Add(effect, new Vector2(from.x, from.z), new Vector2(to.x, to.z), 0f, strength);
        }

        void Add(GroundEffect effect, Vector2 from, Vector2 to, float radius, float strength)
        {
            bool isValid = !_isDisposed && effect != null && float.IsFinite(from.x) && float.IsFinite(from.y)
                           && float.IsFinite(to.x) && float.IsFinite(to.y) && float.IsFinite(radius)
                           && RenderMath.IsPositive(strength);
            if (!isValid)
            {
                return;
            }

            OneShot shot = new OneShot { effect = effect, from = from, to = to, radius = radius, strength = strength };
            if (_oneShotCount < OneShotCapacity)
            {
                _oneShots[_oneShotCount] = shot;
                _oneShotCount++;
                return;
            }

            int oldest = 0;
            for (int i = 1; i < _oneShotCount; i++)
            {
                if (_oneShots[i].age > _oneShots[oldest].age)
                {
                    oldest = i;
                }
            }

            _oneShots[oldest] = shot;
        }

        // An effect that follows its source; hidden until the source shows it
        public GroundHandle Hold(GroundEffect effect)
        {
            GroundHandle handle = new GroundHandle(this, effect);
            if (_isDisposed)
            {
                handle.Release();
                return handle;
            }
            _held.Add(handle);
            return handle;
        }

        internal void Forget(GroundHandle handle)
        {
            _held.Remove(handle);
        }

        public void AddBody(IGroundBody body)
        {
            if (!_isDisposed && body != null && !_bodies.Contains(body))
            {
                _bodies.Add(body);
            }
        }

        public void RemoveBody(IGroundBody body)
        {
            _bodies.Remove(body);
        }

        // One-shots age and the ones played out go; shown held effects age
        public void Advance(float deltaTime)
        {
            if (_isDisposed || !float.IsFinite(deltaTime) || deltaTime <= 0f)
            {
                return;
            }

            int kept = 0;
            for (int i = 0; i < _oneShotCount; i++)
            {
                OneShot shot = _oneShots[i];
                shot.age += deltaTime;
                if (shot.effect.IsOver(shot.age))
                {
                    continue;
                }

                _oneShots[kept] = shot;
                kept++;
            }

            Array.Clear(_oneShots, kept, _oneShotCount - kept);
            _oneShotCount = kept;
            foreach (GroundHandle handle in _held)
            {
                if (handle.isShown)
                {
                    handle.age += deltaTime;
                }
            }
        }

        // Every stamp this frame from start on, as many as fit: the zones' effects, the one-shots, the shown held
        // effects and the bodies. capsules is scratch room for the bodies; heights are measured from surfaceY.
        public int Collect(GroundStamp[] into, int start, ReadOnlySpan<Zone> zones, BodyCapsule[] capsules,
                           float surfaceY, float cellSize)
        {
            if (_isDisposed || into == null)
            {
                return 0;
            }

            int count = 0;
            foreach (Zone zone in zones)
            {
                GroundEffect effect = _vocabulary.ForZone((ZoneKind)zone.kind);
                if (effect != null)
                {
                    // The registry already fades a zone's strength over its life
                    Vector2 at = new Vector2(zone.position.x, zone.position.z);
                    count += effect.Write(into, start + count, at, at, zone.radius, zone.strength, zone.age, false);
                }
            }

            for (int i = 0; i < _oneShotCount; i++)
            {
                OneShot shot = _oneShots[i];
                count += shot.effect.Write(into, start + count, shot.from, shot.to, shot.radius, shot.strength,
                                           shot.age, true);
            }

            foreach (GroundHandle handle in _held)
            {
                if (handle.isShown && handle.effect != null)
                {
                    count += handle.effect.Write(into, start + count, handle.from, handle.to, handle.radius,
                                                 handle.strength, handle.age, false);
                }
            }

            if (capsules != null)
            {
                int gathered = 0;
                int bodyIndex = 0;
                while (bodyIndex < _bodies.Count && gathered < capsules.Length)
                {
                    IGroundBody body = _bodies[bodyIndex];
                    gathered += Mathf.Clamp(body.AppendCapsules(capsules, gathered), 0, capsules.Length - gathered);
                    if (bodyIndex < _bodies.Count && ReferenceEquals(_bodies[bodyIndex], body))
                    {
                        bodyIndex++;
                    }
                }

                count += BodyStamps.Append(capsules, gathered, surfaceY, cellSize, into, start + count);
            }

            return count;
        }

        // Every one-shot gone and every held effect hidden; bodies stay, they re-register only on enable
        public void Clear()
        {
            Array.Clear(_oneShots, 0, _oneShotCount);
            _oneShotCount = 0;
            foreach (GroundHandle handle in _held)
            {
                handle.Hide();
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            Clear();
            while (_held.Count > 0)
            {
                _held[_held.Count - 1].Release();
            }
            _bodies.Clear();
            if (_ownsVocabulary)
            {
                RenderObjects.Release(_vocabulary);
            }
        }

        // The first one-shot of this effect still playing: where, how wide and how strong it was played
        public bool Find(GroundEffect effect, out Vector2 from, out Vector2 to, out float radius, out float strength)
        {
            for (int i = 0; i < _oneShotCount; i++)
            {
                if (_oneShots[i].effect == effect)
                {
                    from = _oneShots[i].from;
                    to = _oneShots[i].to;
                    radius = _oneShots[i].radius;
                    strength = _oneShots[i].strength;
                    return true;
                }
            }

            from = to = Vector2.zero;
            radius = strength = 0f;
            return false;
        }

        // How many one-shots of this effect are playing, for tools and tests
        public int Playing(GroundEffect effect)
        {
            int count = 0;
            for (int i = 0; i < _oneShotCount; i++)
            {
                count += _oneShots[i].effect == effect ? 1 : 0;
            }

            return count;
        }
    }
}
