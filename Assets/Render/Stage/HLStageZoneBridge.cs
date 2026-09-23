using System;
using HealerLike.Render.Grass;
using HealerLike.Render.Zones;
using UnityEngine;
namespace HealerLike.Render.Stage
{
    // Zone publication runs at -1000, grass submission at 10000. Borrow, never own, its buffer.
    [DefaultExecutionOrder(0)]
    public sealed class HLStageZoneBridge : MonoBehaviour
    {
        [SerializeField] HLZoneRegistry zoneRegistry;
        [SerializeField] HLGrassField grassField;
        Func<GraphicsBuffer> buffer;
        Func<int> count;
        Action<GraphicsBuffer,int> publish;
        public HLZoneRegistry ZoneRegistry => zoneRegistry;
        public HLGrassField GrassField => grassField;
        public void Configure(Func<GraphicsBuffer> getBuffer, Func<int> getCount, Action<GraphicsBuffer,int> setSnapshot)
        {
            buffer=getBuffer ?? throw new ArgumentNullException(nameof(getBuffer));
            count=getCount ?? throw new ArgumentNullException(nameof(getCount));
            publish=setSnapshot ?? throw new ArgumentNullException(nameof(setSnapshot));
        }
        // Concrete track types called directly: a rename is a compile error, not a runtime reflection failure.
        public void Configure(HLZoneRegistry zones, HLGrassField field)
        {
            zoneRegistry=zones; grassField=field;
            if(zones && field) Configure(()=>zones.buffer,()=>zones.count,field.SetZoneSnapshot);
        }
        void OnEnable() { if(zoneRegistry && grassField) Configure(zoneRegistry,grassField); }
        void LateUpdate() { if(publish!=null) publish(buffer(),count()); }
        void OnDisable() { publish?.Invoke(null,0); }
    }
}
