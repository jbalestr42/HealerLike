using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Zones
{
    // Producers update their zones in Update, the RenderManager publishes the snapshot before the grass draws
    public class ZoneRegistry : MonoBehaviour, IZoneOwner
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

        class GraphicsUpload : IZoneUpload
        {
            static readonly int zonesId = Shader.PropertyToID("_HL_Zones");
            static readonly int countId = Shader.PropertyToID("_HL_ZoneCount");

            GraphicsBuffer _buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 64, Zone.Stride);
            public GraphicsBuffer buffer { get { return _buffer; } }

            public void Upload(Zone[] zones)
            {
                _buffer.SetData(zones);
            }

            public void Bind()
            {
                Shader.SetGlobalBuffer(zonesId, _buffer);
            }

            public void PublishCount(int count)
            {
                Shader.SetGlobalInt(countId, count);
            }

            public void Unbind()
            {
                Shader.SetGlobalBuffer(zonesId, (GraphicsBuffer)null);
            }

            public void Dispose()
            {
                _buffer.Dispose();
            }
        }

        readonly List<Entry> _entries = new List<Entry>();
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

        public GraphicsBuffer buffer { get { return _upload != null ? _upload.buffer : null; } }

        public ReadOnlySpan<Zone> snapshot { get { return new ReadOnlySpan<Zone>(_packed, 0, _count); } }

        public void Init(IZoneUpload upload = null)
        {
            if (_upload != null)
            {
                return;
            }

            _upload = upload != null ? upload : new GraphicsUpload();
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
            if (!(duration > 0) || float.IsInfinity(duration))
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

            int handle = AddPulse(ZoneKind.Heal, target.position, radius, strength, 0.45f);
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

        public int AddLaunch(Vector3 source, Vector3 target)
        {
            Vector3 direction = target - source;
            direction.y = 0;
            int handle = AddPulse(ZoneKind.Launch, source, direction.magnitude, 1, 0.4f);
            int i = Find(handle);
            if (i >= 0)
            {
                Entry entry = _entries[i];
                entry.zone.reserved = ZonePacker.EncodeDirection(direction);
                _entries[i] = entry;
            }

            return handle;
        }

        public void RefreshZone(int handle, ZoneKind kind, Vector3 position, float radius, float strength)
        {
            UpdateZone(handle, kind, position, radius, strength);
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

        public void PublishFrame(float deltaTime)
        {
            if (_upload == null)
            {
                return;
            }

            if (!float.IsFinite(deltaTime) || deltaTime < 0f)
            {
                Debug.LogError($"[HLZoneRegistry] PublishFrame needs a finite delta time of zero or more, got {deltaTime}.");
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

            // Not a sort: keep room for the gameplay feedback first, footprints get what is left
            int feedback = 0;
            for (int i = 0; i < written; i++)
            {
                if (_source[i].kind != (int)ZoneKind.Trample)
                {
                    feedback++;
                }
            }

            int footprints = Mathf.Max(0, ZonePacker.MaxZones - feedback);
            int selected = 0;
            for (int i = 0; i < written; i++)
            {
                if (_source[i].kind == (int)ZoneKind.Trample && footprints-- <= 0)
                {
                    continue;
                }

                _source[selected] = _source[i];
                selected++;
            }

            _count = ZonePacker.Pack(new ReadOnlySpan<Zone>(_source, 0, selected), _packed, out _,
                                       out int overflow);
            overflow += written - selected;
            _overflowCount = overflow;
            if (overflow > 0 && !_overflowing)
            {
                Debug.LogWarning("HLZoneRegistry: cosmetic zone capacity exceeded; "
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
