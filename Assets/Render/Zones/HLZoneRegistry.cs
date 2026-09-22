using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Zones
{
    /// <summary>Upload boundary for the cosmetic zone owner; tests supply a CPU-only fake.</summary>
    public interface IHLZoneUpload : IDisposable
    {
        GraphicsBuffer Buffer { get; }
        void Upload(HLZone[] zones);
        void Bind();
        void PublishCount(int count);
        void Unbind();
    }

    /// <summary>
    /// Single owner of cosmetic zones. Gameplay feedback reserves capacity before decorative Trample.
    /// Within the selected set, registration order is preserved and the frozen packer is unchanged.
    /// Trample fills remaining slots, first registered first; dropped footprints remain live.
    /// Overflow registrations remain live and can enter the snapshot when earlier zones disappear.
    /// Handles are never reused during this component's lifetime; zero denotes a rejected add.
    /// Producers update in Update; the snapshot is published in early LateUpdate before grass draws.
    /// Consumers borrow Buffer and Count, bind both explicitly for compute, and never dispose Buffer.
    /// Stop consumer submissions before disabling this owner.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class HLZoneRegistry : MonoBehaviour, IHLZoneOwner
    {
        struct Entry
        {
            public int handle;
            public HLZone zone;
            public float duration;
            public float initialStrength;
            public Transform follow;
            public bool followsTarget;
        }

        public static HLZoneRegistry Current { get; private set; }
        readonly List<Entry> _entries = new List<Entry>();
        readonly HLZone[] _packed = new HLZone[HLZonePacker.MaxZones];
        HLZone[] _source = new HLZone[HLZonePacker.MaxZones];
        IHLZoneUpload _upload;
        int _nextHandle;
        bool _overflowing;
        public int Count { get; private set; }
        public int LiveCount => _entries.Count;
        public int OverflowCount { get; private set; }
        public GraphicsBuffer Buffer => _upload?.Buffer;
        public ReadOnlySpan<HLZone> Snapshot => new ReadOnlySpan<HLZone>(_packed, 0, Count);

        void OnEnable()
        {
            if (Application.isPlaying) Initialize();
        }

        /// <summary>Claims sole ownership. An injected uploader is owned and disposed by this registry.</summary>
        public void Initialize(IHLZoneUpload upload = null)
        {
            if (_upload != null) return;
            if (Current != null && Current != this)
                throw new InvalidOperationException("Only one HLZoneRegistry may publish zones.");
            _upload = upload ?? new GraphicsUpload();
            Current = this;
            _upload.PublishCount(0);
        }

        public int Add(HLZoneKind kind, Vector3 position, float radius, float strength)
        {
            if (_upload == null || _nextHandle == int.MaxValue ||
                !HLZonePacker.TryCreate(position, radius, kind, strength, 0, out var zone) || zone.strength <= 0)
                return 0;
            int handle = ++_nextHandle;
            _entries.Add(new Entry { handle = handle, zone = zone });
            if (_source.Length < _entries.Count) Array.Resize(ref _source, _source.Length * 2);
            return handle;
        }

        /// <summary>Adds a cosmetic pulse with linear decay on scaled game time, then auto-removal.</summary>
        public int AddPulse(HLZoneKind kind, Vector3 position, float radius, float strength, float duration)
        {
            if (!(duration > 0) || float.IsInfinity(duration)) return 0;
            int handle = Add(kind, position, radius, strength);
            if (handle == 0) return 0;
            int i = _entries.Count - 1;
            Entry entry = _entries[i];
            entry.duration = duration;
            entry.initialStrength = entry.zone.strength;
            _entries[i] = entry;
            return handle;
        }

        /// <summary>Target-following heal pulse. Its radius blooms in the shared GPU loader.</summary>
        public int AddHealPulse(Transform target, float radius, float strength = 1)
        {
            if (!target) return 0;
            int handle = AddPulse(HLZoneKind.Heal, target.position, radius, strength, 0.45f);
            int i = Find(handle);
            if (i >= 0)
            {
                Entry entry = _entries[i]; entry.follow = target; entry.followsTarget = true;
                _entries[i] = entry;
            }
            return handle;
        }

        public int AddLaunch(Vector3 source, Vector3 target)
        {
            Vector3 direction = target - source; direction.y = 0;
            int handle = AddPulse(HLZoneKind.Launch, source, direction.magnitude, 1, 0.4f);
            int i = Find(handle);
            if (i >= 0)
            {
                Entry entry = _entries[i]; entry.zone.reserved = HLZonePacker.EncodeDirection(direction);
                _entries[i] = entry;
            }
            return handle;
        }

        // Wave 4 owns the Update -> UpdateZone rename; this is the only producer call site.
        public void RefreshZone(int handle, HLZoneKind kind, Vector3 position, float radius, float strength)
            => UpdateZone(handle, kind, position, radius, strength);

        /// <summary>Updates in place without reordering or resetting age. Invalid/zero values remove it.
        /// Updating a pulse's strength changes its initial strength, retaining its fade clock.</summary>
        public void UpdateZone(int handle, HLZoneKind kind, Vector3 position, float radius, float strength)
        {
            int i = Find(handle);
            if (i < 0) return;
            Entry entry = _entries[i];
            if (!HLZonePacker.TryCreate(position, radius, kind, strength, entry.zone.age, out var zone) || zone.strength <= 0)
            {
                _entries.RemoveAt(i);
                return;
            }
            entry.initialStrength = zone.strength;
            if (entry.duration > 0) zone.strength *= Mathf.Clamp01(1 - zone.age / entry.duration);
            entry.zone = zone;
            _entries[i] = entry;
        }

        public void Remove(int handle)
        {
            int i = Find(handle);
            if (i >= 0) _entries.RemoveAt(i);
        }

        public bool Contains(int handle) => Find(handle) >= 0;
        int Find(int handle)
        {
            if (handle == 0) return -1;
            for (int i = 0; i < _entries.Count; i++) if (_entries[i].handle == handle) return i;
            return -1;
        }

        void LateUpdate() => PublishFrame(Time.deltaTime);

        /// <summary>Advances scaled seconds and publishes one stable packed snapshot, including count zero.</summary>
        public void PublishFrame(float deltaTime)
        {
            if (_upload == null) return;
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            int written = 0;
            for (int i = 0; i < _entries.Count; i++)
            {
                Entry entry = _entries[i];
                if (entry.followsTarget)
                {
                    if (!entry.follow || !entry.follow.gameObject.activeInHierarchy) continue;
                    entry.zone.position = entry.follow.position;
                }
                entry.zone.age = Mathf.Min(float.MaxValue, entry.zone.age + deltaTime);
                if (entry.duration > 0)
                {
                    entry.zone.strength = entry.initialStrength * Mathf.Clamp01(1 - entry.zone.age / entry.duration);
                    if (entry.zone.strength <= 0) continue;
                }
                _entries[written] = entry;
                _source[written++] = entry.zone;
            }
            if (written < _entries.Count) _entries.RemoveRange(written, _entries.Count - written);
            // Explicit admission policy, not a sort: reserve space for non-Trample records,
            // then scan the original order. Persistent footprints cannot starve short feedback.
            int feedback = 0;
            for (int i = 0; i < written; i++) if (_source[i].kind != (int)HLZoneKind.Trample) feedback++;
            int footprints = Mathf.Max(0, HLZonePacker.MaxZones - feedback);
            int selected = 0;
            for (int i = 0; i < written; i++)
            {
                if (_source[i].kind == (int)HLZoneKind.Trample && footprints-- <= 0) continue;
                _source[selected++] = _source[i];
            }
            Count = HLZonePacker.Pack(new ReadOnlySpan<HLZone>(_source, 0, selected), _packed, out _, out int overflow);
            overflow += written - selected;
            OverflowCount = overflow;
            if (overflow > 0 && !_overflowing) Debug.LogWarning("HLZoneRegistry: cosmetic zone capacity exceeded; feedback reserved before decorative footprints; first registered wins within each kind.", this);
            _overflowing = overflow > 0;
            _upload.Upload(_packed);
            _upload.Bind();
            _upload.PublishCount(Count);
        }

        void OnDisable() => Release();
        void OnDestroy() => Release();

        /// <summary>Publishes zero, unbinds, then disposes. Safe to call repeatedly.</summary>
        public void Release()
        {
            if (_upload != null)
            {
                _upload.PublishCount(0);
                _upload.Unbind();
                _upload.Dispose();
                _upload = null;
            }
            if (Current == this) Current = null;
            _entries.Clear();
            Array.Clear(_packed, 0, _packed.Length);
            Count = OverflowCount = 0;
            _overflowing = false;
        }

        sealed class GraphicsUpload : IHLZoneUpload
        {
            static readonly int ZonesId = Shader.PropertyToID("_HL_Zones");
            static readonly int CountId = Shader.PropertyToID("_HL_ZoneCount");
            public GraphicsBuffer Buffer { get; } = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 64, HLZone.Stride);
            public void Upload(HLZone[] zones) => Buffer.SetData(zones);
            public void Bind() => Shader.SetGlobalBuffer(ZonesId, Buffer);
            public void PublishCount(int count) => Shader.SetGlobalInt(CountId, count);
            public void Unbind() => Shader.SetGlobalBuffer(ZonesId, (GraphicsBuffer)null);
            public void Dispose() => Buffer.Dispose();
        }
    }
}
