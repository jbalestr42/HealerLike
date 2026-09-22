using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Stage
{
    public class HLStageZoneBridgeTests
    {
        [Test] public void ForwardsEverySnapshotIncludingZeroAndClearsBorrowBeforeOwnerRelease()
        {
            var go=new GameObject("HLBridgeTest"); go.SetActive(false);
            try {
                var bridge=go.AddComponent<HLStageZoneBridge>(); int count=5, observed=-1, writes=0;
                bridge.Configure(()=>null,()=>count,(buffer,n)=>{ observed=n; writes++; });
                TestHelpers.InvokePrivate(bridge,"LateUpdate"); Assert.That(observed,Is.EqualTo(5));
                count=0; TestHelpers.InvokePrivate(bridge,"LateUpdate"); Assert.That(observed,Is.Zero);
                TestHelpers.InvokePrivate(bridge,"OnDisable"); Assert.That(observed,Is.Zero); Assert.That(writes,Is.EqualTo(3));
            } finally { Object.DestroyImmediate(go); }
        }
        [Test] public void MissingOptionalTracksAreSafe()
        {
            var go=new GameObject("HLBridgeTest"); go.SetActive(false);
            try { var bridge=go.AddComponent<HLStageZoneBridge>(); Assert.DoesNotThrow(()=> { TestHelpers.InvokePrivate(bridge,"OnEnable"); TestHelpers.InvokePrivate(bridge,"LateUpdate"); TestHelpers.InvokePrivate(bridge,"OnDisable"); }); }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
