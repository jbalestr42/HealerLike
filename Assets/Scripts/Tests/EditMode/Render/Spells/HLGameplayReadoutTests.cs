using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Spells
{
    public class HLGameplayReadoutTests
    {
        [Test] public void InstantHitArmorGrantCreatesPlatesWithoutAStartedHandler()
        {
            var go = new GameObject("HLShieldRecipient");
            var factory = ScriptableObject.CreateInstance<BuffHandlerFactory>();
            var modifier = ScriptableObject.CreateInstance<FlatModifierFactory>();
            try
            {
                var attributes = TestHelpers.CreateAttributeManager(go, AttributeType.HitArmor, 0);
                var manager = go.AddComponent<BuffManager>(); manager.isEnabled = true;
                int starts = 0; manager.OnBuffHandlerStarted.AddListener(_ => starts++);
                modifier.data = new FlatModifierData { type = AttributeType.HitArmor, modifierType = AttributeModifierType.Add, value = 2 };
                factory.uniqueID = "HLInstantShieldFixture";
                factory.data = new BuffHandlerData { durationType = DurationType.Instant, buffFactoryList = new List<ABuffFactory> { modifier } };
                var view = go.AddComponent<HLAttributeShieldView>(); view.Bind(attributes, go.transform);
                Assert.IsNull(view.Effect);
                TestHelpers.WithLoggingDisabled(() => { manager.AddHandler(factory, go, go); TestHelpers.InvokePrivate(manager, "Update"); TestHelpers.InvokePrivate(attributes, "Update"); });
                Assert.AreEqual(0, starts, "The real instant gameplay path must not be replaced with a fabricated start event.");
                Assert.AreEqual(2, attributes.Get(AttributeType.HitArmor).Value);
                view.Refresh(); Assert.NotNull(view.Effect);
                int plates = 0; foreach (var plate in view.Effect.parts) if (plate.gameObject.activeSelf) plates++;
                Assert.AreEqual(2, plates);
                attributes.Get(AttributeType.HitArmor).BaseValue = 0; attributes.Get(AttributeType.HitArmor).Update();
                view.Refresh(); Assert.IsNull(view.Effect);
                Object.DestroyImmediate(view);
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(factory); Object.DestroyImmediate(modifier); }
        }
        [Test] public void ResourceBindingsNeverDoubleSubscribeAndManaIsNotHealth()
        {
            var owner = new GameObject("HLResourceOwner"); var caster = new GameObject("HLCaster");
            try
            {
                var health = TestHelpers.CreateResourceAttribute(owner, AttributeType.HealthMax, 100);
                var manaGo = new GameObject("HLMana"); manaGo.transform.SetParent(owner.transform);
                var mana = TestHelpers.CreateResourceAttribute(manaGo, AttributeType.ManaMax, 100);
                var spy = new Sink(); var registry = new HLRenderRegistry { spellSink = spy };
                var healed = new HealSink(); registry.Register(caster, healed);
                var observer = owner.AddComponent<HLResourceOutcomeObserver>();
                for (int i=0;i<5;i++) observer.Bind(health, mana, registry, true);
                var modifier = new ResourceModifier { source = caster };
                health.OnAllConsumerProcessed.Invoke(owner, modifier, 7, false);
                mana.OnAllConsumerProcessed.Invoke(manaGo, modifier, 9, false);
                Assert.AreEqual(2, spy.impacts); Assert.AreEqual(HLResourceKind.Mana, spy.last); Assert.AreEqual(1, healed.count);
                observer.enabled = false; TestHelpers.InvokePrivate(observer,"OnDisable");
                health.OnAllConsumerProcessed.Invoke(owner, modifier, 7, false); Assert.AreEqual(2, spy.impacts);
                observer.enabled = true; TestHelpers.InvokePrivate(observer,"OnEnable");
                health.OnAllConsumerProcessed.Invoke(owner, modifier, -2, false); Assert.AreEqual(3, spy.impacts); Assert.AreEqual(1, healed.count);
            }
            finally { Object.DestroyImmediate(owner); Object.DestroyImmediate(caster); }
        }
        [Test] public void PoisonTintSurvivesAnotherStatusAndClearsWithSink()
        {
            var host=new GameObject("HLTintSink"); var target=new GameObject("HLTintTarget");
            var poison=UnityEditor.AssetDatabase.LoadAssetAtPath<BuffHandlerFactory>("Assets/Render/Stage/Data/CharacterSkills/PoisonSingleTarget/HLPoisonSingleTarget_BuffHandlerFactory.asset");
            var buff=UnityEditor.AssetDatabase.LoadAssetAtPath<BuffHandlerFactory>("Assets/Render/Stage/Data/CharacterSkills/MultiTargetBuffAttackRate/HLBuffHandlerFactory.asset");
            try
            {
                var sink=host.AddComponent<HLSpellVisualSink>();
                sink.SetStatus(null,target,poison,1,1,5,HLClockKind.Simulation);
                Assert.AreNotEqual(Color.white,HLBodyTintState.Read(target));
                sink.SetStatus(null,target,buff,1,1,5,HLClockKind.Simulation);
                sink.RemoveStatus(null,target,buff);
                Assert.AreNotEqual(Color.white,HLBodyTintState.Read(target), "Removing an unrelated status must not erase poison.");
                sink.Clear(); Assert.AreEqual(Color.white,HLBodyTintState.Read(target));
                TestHelpers.InvokePrivate(sink,"OnDestroy");
            }
            finally { Object.DestroyImmediate(host); Object.DestroyImmediate(target); }
        }
        sealed class HealSink : IHLHealVisualSink { public int count; public void OnHealResolved(GameObject target,float value,bool critical) => count++; }
        sealed class Sink : IHLSpellVisualSink
        {
            public int impacts; public HLResourceKind last;
            public void ShowImpact(GameObject source,GameObject target,HLResourceKind resource,float amount,bool critical) { impacts++; last=resource; }
            public void SetStatus(GameObject source,GameObject target,ABuffHandlerFactory factory,int stacks,float elapsed,float duration,HLClockKind clock) {}
            public void RemoveStatus(GameObject source,GameObject target,ABuffHandlerFactory factory) {}
            public void PulseArea(Vector3 center,float radius,HealerLike.Render.Zones.HLZoneKind kind,float strength) {}
        }
    }
}
