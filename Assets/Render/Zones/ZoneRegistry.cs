using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Zones
{
    // Producers update their zones in Update, the RenderManager publishes the snapshot before the grass draws
    public class ZoneRegistry : MonoBehaviour
    {
        struct Entry
        {
            public int handle;
            public Zone zone;
            public float duration;
            public float initialStrength;
            public Transform follow;
            public bool followsTarget;
        }

        public static readonly float HealPulseSeconds = 0.45f;
        public static readonly float ShockSeconds = 0.6f;
        public static readonly float ScorchSeconds = 0.6f;

        readonly List<Entry> _entries = new List<Entry>();
        readonly List<IZoneBody> _bodies = new List<IZoneBody>();
        readonly Zone[] _packed = new Zone[ZonePacker.MaxZones];
        Zone[] _source = new Zone[ZonePacker.MaxZones];
        IZoneUpload _upload;
        int _nextHandle;
        bool _overflowing;

        int _count;
        public int count { get { return _count; } }

        int _overflowCount;
        public int overflowCount { get { return _overflowCount; } }

        public int liveCount { get { return _entries.Count; } }

        public int bodyCount { get { return _bodies.Count; } }

        public GraphicsBuffer buffer { get { return _upload != null ? _upload.buffer : null; } }

        public ReadOnlySpan<Zone> snapshot { get { return new ReadOnlySpan<Zone>(_packed, 0, _count); } }

        public void Init(IZoneUpload upload = null)
        {
            if (_upload != null)
            {
                return;
            }

            _upload = upload != null ? upload : new ZoneGraphicsUpload();
            _upload.PublishCount(0);
        }

        public int Add(ZoneKind kind, Vector3 position, float radius, float strength)
        {
            if (_upload == null || _nextHandle == int.MaxValue)
            {
                return 0;
            }

            if (!ZonePacker.TryCreate(position, radius, kind, strength, 0, out Zone zone) || zone.strength <= 0)
            {
                return 0;
            }

            int handle = ++_nextHandle;
            _entries.Add(new Entry { handle = handle, zone = zone });
            if (_source.Length < _entries.Count)
            {
                Array.Resize(ref _source, _source.Length * 2);
            }

            return handle;
        }

        // Fades linearly on scaled time then removes itself
        public int AddPulse(ZoneKind kind, Vector3 position, float radius, float strength, float duration)
        {
            if (!RenderMath.IsPositive(duration))
            {
                return 0;
            }

            int handle = Add(kind, position, radius, strength);
            if (handle == 0)
            {
                return 0;
            }

            int i = _entries.Count - 1;
            Entry entry = _entries[i];
            entry.duration = duration;
            entry.initialStrength = entry.zone.strength;
            _entries[i] = entry;
            return handle;
        }

        public int AddHealPulse(Transform target, float radius, float strength = 1)
        {
            if (!target)
            {
                return 0;
            }

            int handle = AddPulse(ZoneKind.Heal, target.position, radius, strength, HealPulseSeconds);
            int i = Find(handle);
            if (i >= 0)
            {
                Entry entry = _entries[i];
                entry.follow = target;
                entry.followsTarget = true;
                _entries[i] = entry;
            }

            return handle;
        }

        // A lightning bolt's burn along the ground from one point to another, fading over ScorchSeconds
        public int AddScorch(Vector3 from, Vector3 to)
        {
            Vector3 path = to - from;
            path.y = 0f;
            int handle = AddPulse(ZoneKind.Scorch, from, path.magnitude, 1f, ScorchSeconds);
            SetDirection(handle, path);
            return handle;
        }

        // The XZ heading a zone carries, kept through later updates; a flat or invalid direction leaves it
        public void SetDirection(int handle, Vector3 direction)
        {
            int i = Find(handle);
            direction.y = 0f;
            if (i < 0 || !RenderMath.IsFinite(direction) || direction.sqrMagnitude < 1e-10f)
            {
                return;
            }

            Entry entry = _entries[i];
            entry.zone.reserved = ZonePacker.EncodeDirection(direction);
            _entries[i] = entry;
        }

        // A blast ring out from position to radius, fading over ShockSeconds
        public int AddShock(Vector3 position, float radius, float strength)
        {
            return AddPulse(ZoneKind.Shock, position, radius, strength, ShockSeconds);
        }

        // Keeps the order and the age, an invalid value removes the zone
        public void UpdateZone(int handle, ZoneKind kind, Vector3 position, float radius, float strength)
        {
            int i = Find(handle);
            if (i < 0)
            {
                return;
            }

            Entry entry = _entries[i];
            if (!ZonePacker.TryCreate(position, radius, kind, strength, entry.zone.age, out Zone zone)
                || zone.strength <= 0)
            {
                _entries.RemoveAt(i);
                return;
            }

            zone.reserved = entry.zone.reserved;
            entry.initialStrength = zone.strength;
            if (entry.duration > 0)
            {
                zone.strength *= Mathf.Clamp01(1 - zone.age / entry.duration);
            }

            entry.zone = zone;
            _entries[i] = entry;
        }

        public void Remove(int handle)
        {
            int i = Find(handle);
            if (i >= 0)
            {
                _entries.RemoveAt(i);
            }
        }

        public bool Contains(int handle)
        {
            return Find(handle) >= 0;
        }

        // Bodies stay registered until removed; each is asked for its capsules when the grass gathers them
        public void AddBody(IZoneBody body)
        {
            if (body != null && !_bodies.Contains(body))
            {
                _bodies.Add(body);
            }
        }

        public void RemoveBody(IZoneBody body)
        {
            _bodies.Remove(body);
        }

        // Every body's capsules for this frame, in registration order, as many as fit
        public int GatherBodies(BodyCapsule[] into)
        {
            if (into == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < _bodies.Count && count < into.Length; i++)
            {
                count += Mathf.Clamp(_bodies[i].AppendCapsules(into, count), 0, into.Length - count);
            }

            return count;
        }

        public void PublishFrame(float deltaTime)
        {
            if (_upload == null)
            {
                return;
            }

            if (!float.IsFinite(deltaTime) || deltaTime < 0f)
            {
                string message = "[ZoneRegistry] PublishFrame needs a finite delta time of zero or more, got "
                                 + deltaTime;
                Debug.LogError(message);
                return;
            }

            int written = 0;
            for (int i = 0; i < _entries.Count; i++)
            {
                Entry entry = _entries[i];
                if (entry.followsTarget)
                {
                    if (!entry.follow || !entry.follow.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    entry.zone.position = entry.follow.position;
                }

                entry.zone.age = Mathf.Min(float.MaxValue, entry.zone.age + deltaTime);
                if (entry.duration > 0)
                {
                    entry.zone.strength = entry.initialStrength * Mathf.Clamp01(1 - entry.zone.age / entry.duration);
                    if (entry.zone.strength <= 0)
                    {
                        continue;
                    }
                }

                _entries[written] = entry;
                _source[written] = entry.zone;
                written++;
            }

            if (written < _entries.Count)
            {
                _entries.RemoveRange(written, _entries.Count - written);
            }

            int selected = ZonePacker.ReserveFeedback(_source, written);
            _count = ZonePacker.Pack(new ReadOnlySpan<Zone>(_source, 0, selected), _packed, out _, out int overflow);
            overflow += written - selected;
            _overflowCount = overflow;
            if (overflow > 0 && !_overflowing)
            {
                Debug.LogError("[ZoneRegistry] Cosmetic zone capacity exceeded; "
                                 + "feedback reserved before decorative footprints; "
                                 + "first registered wins within each kind.", this);
            }

            _overflowing = overflow > 0;
            _upload.Upload(_packed);
            _upload.Bind();
            _upload.PublishCount(_count);
        }

        void OnDisable()
        {
            Release();
        }

        void OnDestroy()
        {
            Release();
        }

        // Publishes zero before unbinding, safe to call twice
        public void Release()
        {
            if (_upload != null)
            {
                _upload.PublishCount(0);
                _upload.Unbind();
                _upload.Dispose();
                _upload = null;
            }

            _entries.Clear();
            _bodies.Clear();
            Array.Clear(_packed, 0, _packed.Length);
            _count = 0;
            _overflowCount = 0;
            _overflowing = false;
        }

        int Find(int handle)
        {
            if (handle == 0)
            {
                return -1;
            }

            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].handle == handle)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
