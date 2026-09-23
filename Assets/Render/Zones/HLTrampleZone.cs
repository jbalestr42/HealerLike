using UnityEngine;

namespace HealerLike.Render.Zones
{

        public class HLTrampleZone : MonoBehaviour
    {
        [Min(0)] public float Radius = 0.65f;
        [Range(0, 1)] public float Strength = 1;
        HLZoneRegistry owner;
        int handle;

        void Update()
        {
            Refresh();
        }
        public void Refresh()
        {
            if (owner != HLZoneRegistry.Current) Clear();
            if (!isActiveAndEnabled) { Clear(); return; }
            owner = HLZoneRegistry.Current;
            if (!owner) return;
            if (!owner.Contains(handle)) handle = owner.Add(HLZoneKind.Trample, transform.position, Radius, Strength);
            else owner.RefreshZone(handle, HLZoneKind.Trample, transform.position, Radius, Strength);
        }
        void Clear() { if (owner) owner.Remove(handle); owner = null; handle = 0; }
        void OnDisable()
        {
            Clear();
        }

        void OnDestroy()
        {
            Clear();
        }
    }
}
