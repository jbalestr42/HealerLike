using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public class HLGameplayReadoutTests
    {
        [Test]
        public void InstantHitArmorGrantCreatesPlatesWithoutAStartedHandler()
        {
            GameObject go = new GameObject("HLShieldRecipient");
            BuffHandlerFactory factory = ScriptableObject.CreateInstance<BuffHandlerFactory>();
            FlatModifierFactory modifier = ScriptableObject.CreateInstance<FlatModifierFactory>();
            try
            {
                AttributeManager attributes = TestHelpers.CreateAttributeManager(go, AttributeType.HitArmor, 0);
                BuffManager manager = go.AddComponent<BuffManager>();
                manager.isEnabled = true;
                int starts = 0;
                manager.OnBuffHandlerStarted.AddListener(_ => starts++);
                modifier.data = new FlatModifierData
                {
                    type = AttributeType.HitArmor,
                    modifierType = AttributeModifierType.Add,
                    value = 2f
                };
                factory.uniqueID = "HLInstantShieldFixture";
                factory.data = new BuffHandlerData
                {
                    durationType = DurationType.Instant,
                    buffFactoryList = new List<ABuffFactory> { modifier }
                };
                HLAttributeShieldView view = go.AddComponent<HLAttributeShieldView>();
                view.Bind(attributes, go.transform);
                Assert.IsNull(view.effect);
                TestHelpers.WithLoggingDisabled(() =>
                {
                    manager.AddHandler(factory, go, go);
                    TestHelpers.InvokePrivate(manager, "Update");
                    TestHelpers.InvokePrivate(attributes, "Update");
                });
                Assert.AreEqual(
                    0,
                    starts,
                    "The real instant gameplay path must not be replaced with a fabricated start event."
                );
                Assert.AreEqual(2, attributes.Get(AttributeType.HitArmor).Value);
                view.Refresh();
                Assert.NotNull(view.effect);
                int plates = 0;
                foreach (Transform plate in view.effect.parts)
                {
                    if (plate.gameObject.activeSelf)
                    {
                        plates++;
                    }
                }
                Assert.AreEqual(2, plates);
                attributes.Get(AttributeType.HitArmor).BaseValue = 0f;
                attributes.Get(AttributeType.HitArmor).Update();
                view.Refresh();
                Assert.IsNull(view.effect);
                Object.DestroyImmediate(view);
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(factory);
                Object.DestroyImmediate(modifier);
            }
        }

        [Test]
        public void ResourceBindingsNeverDoubleSubscribeAndManaIsNotHealth()
        {
            GameObject owner = new GameObject("HLResourceOwner");
            GameObject caster = new GameObject("HLCaster");
            try
            {
                ResourceAttribute health = TestHelpers.CreateResourceAttribute(owner, AttributeType.HealthMax, 100);
                GameObject manaGo = new GameObject("HLMana");
                manaGo.transform.SetParent(owner.transform);
                ResourceAttribute mana = TestHelpers.CreateResourceAttribute(manaGo, AttributeType.ManaMax, 100);
                sink spy = new sink();
                HLRenderRegistry registry = new HLRenderRegistry { spellSink = spy };
                HealSink healed = new HealSink();
                registry.Register(caster, healed);
                HLResourceOutcomeObserver observer = owner.AddComponent<HLResourceOutcomeObserver>();
                for (int i = 0; i < 5; i++)
                {
                    observer.Bind(health, mana, registry, true);
                }
                ResourceModifier modifier = new ResourceModifier { source = caster };
                health.OnAllConsumerProcessed.Invoke(owner, modifier, 7, false);
                mana.OnAllConsumerProcessed.Invoke(manaGo, modifier, 9, false);
                Assert.AreEqual(2, spy.impacts);
                Assert.AreEqual(HLResourceKind.Mana, spy.last);
                Assert.AreEqual(1, healed.count);
                observer.enabled = false;
                TestHelpers.InvokePrivate(observer, "OnDisable");
                health.OnAllConsumerProcessed.Invoke(owner, modifier, 7, false);
                Assert.AreEqual(2, spy.impacts);
                observer.enabled = true;
                TestHelpers.InvokePrivate(observer, "OnEnable");
                health.OnAllConsumerProcessed.Invoke(owner, modifier, -2, false);
                Assert.AreEqual(3, spy.impacts);
                Assert.AreEqual(1, healed.count);
            }
            finally
            {
                Object.DestroyImmediate(owner);
                Object.DestroyImmediate(caster);
            }
        }

        [Test]
        public void PoisonTintSurvivesAnotherStatusAndClearsWithSink()
        {
            GameObject host = new GameObject("HLTintSink");
            GameObject target = new GameObject("HLTintTarget");
            BuffHandlerFactory poison = UnityEditor.AssetDatabase.LoadAssetAtPath<BuffHandlerFactory>(
                "Assets/Render/Stage/Data/CharacterSkills/PoisonSingleTarget/"
                + "HLPoisonSingleTarget_BuffHandlerFactory.asset"
            );
            BuffHandlerFactory buff = UnityEditor.AssetDatabase.LoadAssetAtPath<BuffHandlerFactory>(
                "Assets/Render/Stage/Data/CharacterSkills/MultiTargetBuffAttackRate/HLBuffHandlerFactory.asset"
            );
            try
            {
                HLSpellVisualSink sink = host.AddComponent<HLSpellVisualSink>();
                sink.SetStatus(null, target, poison, 1, 1f, 5f, HLClockKind.Simulation);
                Assert.AreNotEqual(Color.white, HLBodyTintState.Read(target));
                sink.SetStatus(null, target, buff, 1, 1f, 5f, HLClockKind.Simulation);
                sink.RemoveStatus(null, target, buff);
                Assert.AreNotEqual(
                    Color.white,
                    HLBodyTintState.Read(target),
                    "Removing an unrelated status must not erase poison."
                );
                sink.Clear();
                Assert.AreEqual(Color.white, HLBodyTintState.Read(target));
                TestHelpers.InvokePrivate(sink, "OnDestroy");
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(target);
            }
        }

        class HealSink : IHLHealVisualSink
        {
            public int count;

            public void OnHealResolved(GameObject target, float value, bool critical)
            {
                count++;
            }
        }

        class sink : IHLSpellVisualSink
        {
            public int impacts;
            public HLResourceKind last;

            public void ShowImpact(
                GameObject source,
                GameObject target,
                HLResourceKind resource,
                float amount,
                bool critical
            )
            {
                impacts++;
                last = resource;
            }

            public void SetStatus(
                GameObject source,
                GameObject target,
                ABuffHandlerFactory factory,
                int stacks,
                float elapsed,
                float duration,
                HLClockKind clock
            ) { }

            public void RemoveStatus(GameObject source, GameObject target, ABuffHandlerFactory factory) { }

            public void PulseArea(
                Vector3 center,
                float radius,
                HealerLike.Render.Zones.HLZoneKind kind,
                float strength
            ) { }
        }
    }
}
