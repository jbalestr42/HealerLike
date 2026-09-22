using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Stones
{
    public class HLStoneDeathBridgeTests
    {
        [Test] public void LivingRemovalDoesNothingAndLethalDepartureCollapsesSynchronouslyOnce()
        {
            HLStoneEnemyVisual visual=null; HLStoneEffects fx=null;
            var target=new GameObject("HLTarget"); var effectsObject=new GameObject("HLEffects"); var bridgeObject=new GameObject("HLBridge");
            try {
                var health=TestHelpers.CreateResourceAttribute(target,AttributeType.HealthMax,100);
                visual=target.AddComponent<HLStoneEnemyVisual>(); fx=effectsObject.AddComponent<HLStoneEffects>(); visual.Initialize(health,1,fx);
                var bridge=bridgeObject.AddComponent<HLStoneDeathBridge>(); bridge.Bind(null,fx);
                bridge.HandleDeparture(health,visual); Assert.AreEqual(0,fx.LiveCount);
                TestHelpers.SetPrivateField(health,"_value",0f); bridge.HandleDeparture(health,visual); Assert.AreEqual(17,fx.LiveCount);
                bridge.HandleDeparture(health,visual); Assert.AreEqual(17,fx.LiveCount);
                visual.Initialize(health,1,fx); bridge.enabled=false; bridge.HandleDeparture(health,visual); Assert.AreEqual(17,fx.LiveCount);
            } finally {if(visual!=null)TestHelpers.InvokePrivate(visual,"OnDestroy");if(fx!=null)TestHelpers.InvokePrivate(fx,"OnDestroy");Object.DestroyImmediate(bridgeObject);Object.DestroyImmediate(target);Object.DestroyImmediate(effectsObject);}
        }
    }
}
