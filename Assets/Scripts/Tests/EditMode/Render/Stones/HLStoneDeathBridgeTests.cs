using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Stones
{
    public class HLStoneDeathBridgeTests
    {
        [Test] public void LivingRemovalDoesNothingAndLethalEventCollapsesSynchronouslyOnce()
        {
            var managerObject=new GameObject("HLManager"); var target=new GameObject("HLTarget"); var effectsObject=new GameObject("HLEffects");
            var bridgeObject=new GameObject("HLBridge");
            try {
                // Construct a local manager only; never read Singleton.instance or call gameplay destruction.
                var manager=managerObject.AddComponent<EntityManager>();
                var health=TestHelpers.CreateResourceAttribute(target,AttributeType.HealthMax,100); Entity entity=null;
                TestHelpers.WithLoggingDisabled(()=>entity=target.AddComponent<Entity>()); TestHelpers.SetPrivateField(entity,"_health",health);
                var visual=target.AddComponent<HLStoneEnemyVisual>(); var fx=effectsObject.AddComponent<HLStoneEffects>(); visual.Initialize(health,1,fx);
                var bridge=bridgeObject.AddComponent<HLStoneDeathBridge>(); bridge.Bind(manager,fx);
                manager.OnEntityKilled.Invoke(entity);Assert.AreEqual(0,fx.LiveCount);
                TestHelpers.SetPrivateField(health,"_value",0f); manager.OnEntityKilled.Invoke(entity);Assert.AreEqual(12,fx.LiveCount);
                manager.OnEntityKilled.Invoke(entity);Assert.AreEqual(12,fx.LiveCount);
                visual.Initialize(health,1,fx);bridge.enabled=false;TestHelpers.InvokePrivate(bridge,"OnDisable");manager.OnEntityKilled.Invoke(entity);Assert.AreEqual(12,fx.LiveCount);
            } finally {Object.DestroyImmediate(bridgeObject);Object.DestroyImmediate(target);Object.DestroyImmediate(effectsObject);Object.DestroyImmediate(managerObject);}
        }
    }
}
