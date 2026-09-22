using System;
using UnityEngine;
namespace HealerLike.Render.Stage
{
    // Zone publication runs at -1000, grass submission at 10000. Borrow, never own, its buffer.
    [DefaultExecutionOrder(0)]
    public sealed class HLStageZoneBridge : MonoBehaviour
    {
        [SerializeField] Behaviour zoneRegistry;
        [SerializeField] Behaviour grassField;
        Func<GraphicsBuffer> buffer;
        Func<int> count;
        Action<GraphicsBuffer,int> publish;
        public void Configure(Func<GraphicsBuffer> getBuffer, Func<int> getCount, Action<GraphicsBuffer,int> setSnapshot)
        {
            buffer=getBuffer ?? throw new ArgumentNullException(nameof(getBuffer));
            count=getCount ?? throw new ArgumentNullException(nameof(getCount));
            publish=setSnapshot ?? throw new ArgumentNullException(nameof(setSnapshot));
        }
        void OnEnable()
        {
            if(!zoneRegistry || !grassField) return;
            var owner=zoneRegistry.GetType();
            Configure((Func<GraphicsBuffer>)Delegate.CreateDelegate(typeof(Func<GraphicsBuffer>),zoneRegistry,owner.GetProperty("Buffer").GetGetMethod()),
                (Func<int>)Delegate.CreateDelegate(typeof(Func<int>),zoneRegistry,owner.GetProperty("Count").GetGetMethod()),
                (Action<GraphicsBuffer,int>)Delegate.CreateDelegate(typeof(Action<GraphicsBuffer,int>),grassField,"SetZoneSnapshot"));
        }
        void LateUpdate() { if(publish!=null) publish(buffer(),count()); }
        void OnDisable() { publish?.Invoke(null,0); }
    }
}
