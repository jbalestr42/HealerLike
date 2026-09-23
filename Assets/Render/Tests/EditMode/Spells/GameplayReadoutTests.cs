using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public class GameplayReadoutTests
    {
        [Test]
        public void InstantHitArmorGrantCreatesPlatesWithoutAStartedHandler()
        {
            GameObject go = new GameObject("ShieldRecipient");
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
                factory.uniqueID = "InstantShieldFixture";
                factory.data = new BuffHandlerData
                {
                    durationType = DurationType.Instant,
                    buffFactoryList = new List<ABuffFactory> { modifier }
                };
                AttributeShieldView view = go.AddComponent<AttributeShieldView>();
                view.Bind(
                    attributes,
                    go.transform,
                    UnityEditor.AssetDatabase.LoadAssetAtPath<SpellLooks>("Assets/Render/Spells/Data/SpellLooks.asset")
                );
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
            GameObject owner = new GameObject("ResourceOwner");
            GameObject caster = new GameObject("Caster");
            try
            {
                ResourceAttribute health = TestHelpers.CreateResourceAttribute(owner, AttributeType.HealthMax, 100);
                GameObject manaGo = new GameObject("Mana");
                manaGo.transform.SetParent(owner.transform);
                ResourceAttribute mana = TestHelpers.CreateResourceAttribute(manaGo, AttributeType.ManaMax, 100);
                sink spy = new sink();
                RenderRegistry registry = new RenderRegistry { spellSink = spy };
                HealSink healed = new HealSink();
                registry.Register(caster, healed);
                ResourceOutcomeObserver observer = owner.AddComponent<ResourceOutcomeObserver>();
                for (int i = 0; i < 5; i++)
                {
                    observer.Bind(health, mana, spy, registry);
                }
                ResourceModifier modifier = new ResourceModifier { source = caster };
                health.OnAllConsumerProcessed.Invoke(owner, modifier, 7, false);
                mana.OnAllConsumerProcessed.Invoke(manaGo, modifier, 9, false);
                Assert.AreEqual(2, spy.impacts);
                Assert.AreEqual(ResourceKind.Mana, spy.last);
                Assert.AreEqual(1, healed.count);
                observer.enabled = false;
                TestHelpers.InvokePrivate(observer, "OnDisable");
                health.OnAllConsumerProcessed.Invoke(owner, modifier, 7, false);
                Assert.AreEqual(2, spy.impacts);
                observer.enabled = true;
                TestHelpers.InvokePrivate(observer, "OnEnable");
                health.OnAllConsumerProcessed.Invoke(owner, modifier, -2, false);
                Assert.AreEqual(3, spy.impacts);
                Assert.AreEqual(2, healed.count, "damage reaches the registry too, each sink filters by sign");
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
            GameObject host = new GameObject("TintSink");
            GameObject target = new GameObject("TintTarget");
            BuffHandlerFactory poison = UnityEditor.AssetDatabase.LoadAssetAtPath<BuffHandlerFactory>(
                "Assets/Data/CharacterSkills/PoisonSingleTarget/PoisonSingleTarget_BuffHandlerFactory.asset"
            );
            BuffHandlerFactory buff = UnityEditor.AssetDatabase.LoadAssetAtPath<BuffHandlerFactory>(
                "Assets/Data/CharacterSkills/MultiTargetBuffAttackRate/BuffHandlerFactory.asset"
            );
            try
            {
                SpellVisualSink sink = host.AddComponent<SpellVisualSink>();
                sink.looks = UnityEditor.AssetDatabase.LoadAssetAtPath<SpellLooks>(
                    "Assets/Render/Spells/Data/SpellLooks.asset"
                );
                sink.SetStatus(null, target, poison, 1, 1f, 5f, ClockKind.Simulation);
                Assert.AreNotEqual(Color.white, BodyTintState.Read(target));
                sink.SetStatus(null, target, buff, 1, 1f, 5f, ClockKind.Simulation);
                sink.RemoveStatus(null, target, buff);
                Assert.AreNotEqual(
                    Color.white,
                    BodyTintState.Read(target),
                    "Removing an unrelated status must not erase poison."
                );
                sink.Clear();
                Assert.AreEqual(Color.white, BodyTintState.Read(target));
                TestHelpers.InvokePrivate(sink, "OnDestroy");
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(target);
            }
        }

        class HealSink : IHealVisualSink
        {
            public int count;

            public void OnHealResolved(GameObject target, float value, bool critical)
            {
                count++;
            }
        }

        class sink : ISpellVisualSink
        {
            public int impacts;
            public ResourceKind last;

            public void ShowImpact(
                GameObject source,
                GameObject target,
                ResourceKind resource,
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
                ClockKind clock
            ) { }

            public void RemoveStatus(GameObject source, GameObject target, ABuffHandlerFactory factory) { }

            public void PulseArea(
                Vector3 center,
                float radius,
                HealerLike.Render.Zones.ZoneKind kind,
                float strength
            ) { }
        }
    }
}
