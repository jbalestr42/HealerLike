using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public class HLChainContactVisualTests
    {
        [Test] public void ThreadsUseContactOrderAndRebindClearsPreviousContact()
        {
            var host=new GameObject("HLContacts");var sinkHost=new GameObject("HLSink");
            var a=new GameObject("HLFirst");var b=new GameObject("HLSecond");
            HLChainContactVisual observer=null;HLSpellVisualSink sink=null;
            try
            {
                var projectile=host.AddComponent<Projectile>();observer=host.AddComponent<HLChainContactVisual>();
                sink=sinkHost.AddComponent<HLSpellVisualSink>();TestHelpers.InvokePrivate(sink,"OnEnable");
                observer.Bind(projectile,sink);a.transform.position=Vector3.left;b.transform.position=Vector3.right;
                projectile.OnHit.Invoke(new OnHitData{target=a});Assert.AreEqual(0,sink.ImpactCount);
                projectile.OnHit.Invoke(new OnHitData{target=b});Assert.AreEqual(1,sink.ImpactCount);
                var thread=sinkHost.GetComponentInChildren<HLSpellEffect>();Assert.IsTrue(thread.ContactThread);
                Assert.Less(Vector3.Distance(a.transform.position,thread.parts[0].position-thread.parts[0].up*thread.parts[0].localScale.y),.0001f);
                observer.Bind(projectile,sink);projectile.OnHit.Invoke(new OnHitData{target=a});Assert.AreEqual(1,sink.ImpactCount);
                TestHelpers.InvokePrivate(observer,"OnDisable");projectile.OnHit.Invoke(new OnHitData{target=b});Assert.AreEqual(1,sink.ImpactCount);
            }
            finally
            {
                if(observer)TestHelpers.InvokePrivate(observer,"OnDestroy");if(sink)TestHelpers.InvokePrivate(sink,"OnDestroy");
                Object.DestroyImmediate(host);Object.DestroyImmediate(sinkHost);Object.DestroyImmediate(a);Object.DestroyImmediate(b);
            }
        }
        [Test] public void InvalidContactsAndRepeatedInactiveSinkContactsAllocateNothing()
        {
            var host=new GameObject("HLContacts");var target=new GameObject("HLTarget");
            HLChainContactVisual observer=null;
            try
            {
                var projectile=host.AddComponent<Projectile>();observer=host.AddComponent<HLChainContactVisual>();observer.Bind(projectile,null);
                var hit=new OnHitData{target=target};
                projectile.OnHit.Invoke(null);projectile.OnHit.Invoke(new OnHitData());
                for(int i=0;i<32;i++)projectile.OnHit.Invoke(hit);
                long before=System.GC.GetAllocatedBytesForCurrentThread();
                for(int i=0;i<64;i++)projectile.OnHit.Invoke(hit);
                long allocated=System.GC.GetAllocatedBytesForCurrentThread()-before;
                Assert.AreEqual(0,allocated);
            }
            finally {if(observer)TestHelpers.InvokePrivate(observer,"OnDestroy");Object.DestroyImmediate(host);Object.DestroyImmediate(target);}
        }
    }
}
