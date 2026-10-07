using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Grammar
{
    // The kind is the buff factory a handler is built from, the one channel that still separates the boons that
    // agree on operation, aspect, tempo and trigger
    public class EffectKindDerivationTests
    {
        // Every shipped handler, read from the factory class of its buffs (see SpellChannelAssetPinningTests)
        static readonly Dictionary<string, EffectKind> Rows = new Dictionary<string, EffectKind> {
            { "CharacterSkills/MultiTargetBuffAttackRate/BuffHandlerFactory", EffectKind.Flat },
            { "CharacterSkills/MultiTargetReduceDamage/BuffHandlerFactory", EffectKind.Flat },
            { "CharacterSkills/PoisonSingleTarget/PoisonSingleTarget_BuffHandlerFactory", EffectKind.Plain },
            { "CharacterSkills/SingleTargetBuffAttackRate/BuffHandlerFactory", EffectKind.Flat },
            { "Entities/GuardianEntity/BuffHandlerFactory", EffectKind.Plain },
            { "Entities/HexerEntity/BuffHandlerFactory", EffectKind.Flat },
            { "Entities/HitArmorBufferEntityEntity/BuffHandlerFactory", EffectKind.Flat },
            { "Entities/ShamanEntity/BuffHandlerFactory", EffectKind.Plain },
            { "Entities/WarDrumEntity/BuffHandlerFactory", EffectKind.Rate },
            { "EntityItems/ArcItem/BuffHandlerFactory", EffectKind.Projectile },
            { "EntityItems/BoostCellItem/BoostBuffHandlerFactory", EffectKind.Flat },
            { "EntityItems/BoostCellItem/BuffHandlerFactory", EffectKind.Positional },
            { "EntityItems/BounceItem/BuffHandlerFactory", EffectKind.Projectile },
            { "EntityItems/CarrionItem/BuffHandlerFactory", EffectKind.Plain },
            { "EntityItems/ConclaveItem/New Buff Handler Factory 1", EffectKind.Flat },
            { "EntityItems/ConclaveItem/New Buff Handler Factory", EffectKind.Flat },
            { "EntityItems/ExplodeOnHitItem/BuffHandlerFactory 1", EffectKind.Flat },
            { "EntityItems/ExplodeOnHitItem/BuffHandlerFactory", EffectKind.Projectile },
            { "EntityItems/FrostItem/BuffHandlerFactory", EffectKind.Plain },
            { "EntityItems/IncreaseDamagePerHitItem/BuffHandlerFactory", EffectKind.Flat },
            { "EntityItems/IncreaseDamageWithProjectileDistanceItem/BuffHandlerFactory", EffectKind.Projectile },
            { "EntityItems/MortarShellItem/BuffHandlerFactory", EffectKind.Projectile },
            { "EntityItems/MultipleShootItem/BuffHandlerFactory", EffectKind.Volume },
            { "EntityItems/PlagueItem/BuffHandlerFactory", EffectKind.Flat },
            { "EntityItems/PoisonItem/BuffHandlerFactory", EffectKind.Plain },
            { "EntityItems/PunchingBagRegenItem/BuffHandlerFactory", EffectKind.Plain },
            { "EntityItems/RageItem/BuffHandlerFactory", EffectKind.Conditional },
            { "EntityItems/RegenHpItem/RegenHpItem_BuffHandlerFactory", EffectKind.Plain },
            { "EntityItems/SelfDestructItem/BuffHandlerFactory", EffectKind.Plain },
            { "EntityItems/SiphonItem/BuffHandlerFactory", EffectKind.Plain },
            { "EntityItems/SlowItem/BuffHandlerFactory", EffectKind.Plain },
            { "EntityItems/TrinityItem/BuffHandlerFactory", EffectKind.Flat },
            { "EntityItems/VenomItem/BuffHandlerFactory", EffectKind.Plain },
            { "EntityItems/VolleyItem/BuffHandlerFactory", EffectKind.Volume },
            { "PlayerItems/DamageAllEnemyItem/BuffHandlerFactory", EffectKind.Plain },
            { "PlayerItems/HealAllEntitiesOnRoundEndItem/HealAllEntitiesOnRoundEndItem_BuffHandlerFactory", EffectKind.Plain },
            { "PlayerItems/ManaOnRoundEndItem/ManaOnRoundEndItem_BuffHandlerFactory", EffectKind.Plain },
            { "CharacterSkills/Curse/BuffHandlerFactory", EffectKind.Plain },
            { "CharacterSkills/DivineIntervention/BuffHandlerFactory", EffectKind.Plain },
            { "CharacterSkills/Rejuvenation/BuffHandlerFactory", EffectKind.Plain },
            { "CharacterSkills/Shield/BuffHandlerFactory", EffectKind.Flat },
            { "CharacterSkills/WildGrowth/BuffHandlerFactory", EffectKind.Plain },
            { "Entities/GroveKeeperEntity/BuffHandlerFactory", EffectKind.Plain },
            { "EntityItems/BloodBondItem/BuffHandlerFactory", EffectKind.Plain },
            { "EntityItems/SapItem/BuffHandlerFactory", EffectKind.Plain },
            { "EntityItems/ZealItem/BuffHandlerFactory", EffectKind.Conditional },
            { "PlayerItems/HealPowerItem/BuffHandlerFactory", EffectKind.Flat },
            { "PlayerItems/VerdantItem/BuffHandlerFactory", EffectKind.Plain },
            { "CharacterSkills/SoulLink/BuffHandlerFactory", EffectKind.Link },
            { "CharacterSkills/ThickBark/BuffHandlerFactory", EffectKind.Flat },
            { "EntityItems/AdrenalineItem/BuffHandlerFactory", EffectKind.Conditional },
            { "EntityItems/ArmorBreakerItem/BuffHandlerFactory", EffectKind.Flat },
            { "EntityItems/BlessedCharmItem/BuffHandlerFactory", EffectKind.Flat },
            { "EntityItems/BloodPriceItem/BuffHandlerFactory", EffectKind.Flat },
            { "EntityItems/BloodPriceItem/PriceBuffHandlerFactory", EffectKind.Reactive },
            { "EntityItems/BoneCharmItem/BuffHandlerFactory", EffectKind.Summon },
            { "EntityItems/BriarThornsItem/BuffHandlerFactory", EffectKind.Reactive },
            { "EntityItems/CursedIdolItem/BuffHandlerFactory", EffectKind.Flat },
            { "EntityItems/CursedIdolItem/CurseBuffHandlerFactory", EffectKind.Plain },
            { "EntityItems/EchoItem/BuffHandlerFactory", EffectKind.Echo },
            { "EntityItems/ExecutionersEdgeItem/BuffHandlerFactory", EffectKind.Flat },
            { "EntityItems/GlassCannonItem/BuffHandlerFactory", EffectKind.Flat },
            { "EntityItems/GratitudeItem/BuffHandlerFactory", EffectKind.Reactive },
            { "EntityItems/GrowingSeedItem/GrowthBuffHandlerFactory", EffectKind.Flat },
            { "EntityItems/HairTriggerItem/BuffHandlerFactory", EffectKind.Flat },
            { "EntityItems/HeartStoneItem/BuffHandlerFactory", EffectKind.Flat },
            { "EntityItems/HuntersMarkItem/BuffHandlerFactory", EffectKind.Flat },
            { "EntityItems/IronPlatingItem/BuffHandlerFactory", EffectKind.Flat },
            { "EntityItems/LoneWolfItem/BuffHandlerFactory", EffectKind.Positional },
            { "EntityItems/LuckyCoinItem/BuffHandlerFactory", EffectKind.Flat },
            { "EntityItems/MartyrsHeartItem/BuffHandlerFactory", EffectKind.Positional },
            { "EntityItems/PhalanxItem/BuffHandlerFactory", EffectKind.Positional },
            { "EntityItems/PhylacteryItem/BuffHandlerFactory", EffectKind.Summon },
            { "EntityItems/SecondWindItem/BuffHandlerFactory", EffectKind.Conditional },
            { "EntityItems/SecondWindItem/InvincibilityBuffHandlerFactory", EffectKind.Plain },
            { "EntityItems/TauntTotemItem/BuffHandlerFactory", EffectKind.Flat },
            { "EntityItems/ThornsOfLifeItem/BuffHandlerFactory", EffectKind.Reactive },
            { "EntityItems/TitanFuryItem/BuffHandlerFactory", EffectKind.Conditional },
            { "EntityItems/TowerShieldItem/BuffHandlerFactory", EffectKind.Flat },
            { "EntityItems/WarBannerItem/BoostBuffHandlerFactory", EffectKind.Flat },
            { "EntityItems/WarBannerItem/BuffHandlerFactory", EffectKind.Positional },
            { "EntityItems/WhetstoneItem/BuffHandlerFactory", EffectKind.Flat },
            { "EventItems/AbyssalWellItem/BuffHandlerFactory", EffectKind.Flat },
            { "EventItems/BerserkersBrandItem/BuffHandlerFactory", EffectKind.Flat },
            { "EventItems/BlackCodexItem/BuffHandlerFactory", EffectKind.Flat },
            { "EventItems/BloodLedgerItem/BuffHandlerFactory", EffectKind.Flat },
            { "EventItems/BloodthirstBladeItem/BuffHandlerFactory", EffectKind.Flat },
            { "EventItems/CodexOfMendingItem/BuffHandlerFactory", EffectKind.Flat },
            { "EventItems/GluttonsCharmItem/BuffHandlerFactory", EffectKind.Flat },
            { "EventItems/HastyGrimoireItem/BuffHandlerFactory", EffectKind.Flat },
            { "EventItems/HungeringMaskItem/BattleGrowthBuffHandlerFactory", EffectKind.Plain },
            { "EventItems/HungeringMaskItem/KillGrowthBuffHandlerFactory", EffectKind.Plain },
            { "EventItems/LeechFangItem/BuffHandlerFactory", EffectKind.Flat },
            { "EventItems/ReapersCoinItem/BuffHandlerFactory", EffectKind.Flat },
            { "EventItems/ScrollOfThriftItem/BuffHandlerFactory", EffectKind.Flat },
            { "EventItems/ThornedCrownItem/BuffHandlerFactory", EffectKind.Flat },
            { "EventItems/TomeOfHasteItem/BuffHandlerFactory", EffectKind.Flat },
            { "EventItems/WellspringManuscriptItem/BuffHandlerFactory", EffectKind.Flat },
            { "PlayerItems/ChaliceOfPlentyItem/BuffHandlerFactory", EffectKind.Flat },
            { "PlayerItems/EmergencyBeaconItem/BeaconBuffHandlerFactory", EffectKind.Conditional },
            { "PlayerItems/EmergencyBeaconItem/BuffHandlerFactory", EffectKind.Reactive },
            { "PlayerItems/EmergencyBeaconItem/HealBuffHandlerFactory", EffectKind.Plain },
            { "PlayerItems/ManaCrystalItem/BuffHandlerFactory", EffectKind.Flat },
            { "PlayerItems/MerchantsLedgerItem/BuffHandlerFactory", EffectKind.Flat },
            { "PlayerItems/NecronomiconItem/BuffHandlerFactory", EffectKind.Reactive },
            { "PlayerItems/NecronomiconItem/SummonBuffHandlerFactory", EffectKind.Flat },
            { "PlayerItems/OverflowingFontItem/BuffHandlerFactory", EffectKind.Reactive },
            { "PlayerItems/PrayerBeadsItem/BuffHandlerFactory", EffectKind.Flat },
            { "PlayerItems/TitheItem/BuffHandlerFactory", EffectKind.Reactive },
            { "PlayerItems/WarDrumsItem/BuffHandlerFactory", EffectKind.Reactive },
            { "PlayerItems/WarDrumsItem/DrumsBuffHandlerFactory", EffectKind.Flat }
        };

        readonly List<Object> _owned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object owned in _owned)
            {
                if (owned)
                {
                    Object.DestroyImmediate(owned);
                }
            }

            _owned.Clear();
        }

        [Test]
        public void Kind_LiveHandlers_MatchesAssetRows()
        {
            foreach (var row in Rows)
            {
                ABuffHandlerFactory handler = AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>(
                    "Assets/Data/" + row.Key + ".asset");
                Assert.IsNotNull(handler, row.Key);
                Assert.AreEqual(row.Value, EffectDerivation.Kind(handler), row.Key);
                Assert.AreEqual(row.Value, EffectDerivation.Channels(handler, true).kind, row.Key);
            }
        }

        [Test]
        public void Rows_CoverEveryLiveHandlerAsset()
        {
            HashSet<string> expected = new HashSet<string>();
            foreach (SpellChannelAssetPinningTests.HandlerRow row in SpellChannelAssetPinningTests.HandlerRows)
            {
                expected.Add(row.path);
            }

            CollectionAssert.AreEquivalent(expected, Rows.Keys);
        }

        // Read on the holder of each item: a growing item's handler carries no kind of its own buffs
        [Test]
        public void Kind_BoonOffenceHandlers_AllCarryAKind()
        {
            EffectContext context = GrowthContext();
            foreach (SpellChannelAssetPinningTests.HandlerRow row in SpellChannelAssetPinningTests.HandlerRows)
            {
                if (row.operation != EffectOperation.Boon || row.aspect != EffectAspect.Offence)
                {
                    continue;
                }

                ABuffHandlerFactory handler = AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>(
                    "Assets/Data/" + row.path + ".asset");
                Assert.AreNotEqual(EffectKind.Plain, EffectDerivation.Kind(handler, context), row.path);
            }
        }

        // Every live growing item's handlers, as on the holder of all of them
        public static EffectContext GrowthContext()
        {
            EffectContext context = EffectContext.Default;
            context.growthHandlers = EffectDerivation.GrowthHandlers(GrowingItems());
            return context;
        }

        [Test]
        public void Kind_EachFactory_ReadsItsKind()
        {
            Assert.AreEqual(EffectKind.Projectile, EffectDerivation.Kind(Make<ProjectileBehaviourBuffFactory>()));
            Assert.AreEqual(EffectKind.Volume, EffectDerivation.Kind(Make<MultipleShootBuffFactory>()));
            Assert.AreEqual(EffectKind.Rate, EffectDerivation.Kind(Make<TimeModifierFactory>()));
            Assert.AreEqual(EffectKind.Conditional, EffectDerivation.Kind(Make<HPBasedModifierFactory>()));
            Assert.AreEqual(EffectKind.Conditional, EffectDerivation.Kind(Make<HealthThresholdModifierFactory>()));
            Assert.AreEqual(EffectKind.Positional, EffectDerivation.Kind(Make<BoostEntitiesOnRelativeCellBuffFactory>()));
            Assert.AreEqual(EffectKind.Flat, EffectDerivation.Kind(Make<FlatModifierFactory>()));
            Assert.AreEqual(EffectKind.Plain, EffectDerivation.Kind(Make<InvincibilityBuffFactory>()));
        }

        [Test]
        public void Kind_OctoberFactories_ReadTheirKind()
        {
            Assert.AreEqual(EffectKind.Positional, EffectDerivation.Kind(Make<ShareHealOnRelativeCellBuffFactory>()));
            Assert.AreEqual(EffectKind.Positional, EffectDerivation.Kind(Make<AlliesOnRelativeCellModifierFactory>()));
            Assert.AreEqual(EffectKind.Conditional, EffectDerivation.Kind(Make<ApplyBuffBelowHealthBuffFactory>()));
            Assert.AreEqual(EffectKind.Reactive, EffectDerivation.Kind(Make<ApplyBuffOnEventBuffFactory>()));
            Assert.AreEqual(EffectKind.Reactive, EffectDerivation.Kind(Make<ConsumerOnAttackBuffFactory>()));
            Assert.AreEqual(EffectKind.Reactive, EffectDerivation.Kind(Make<DamageEnemyOnHealBuffFactory>()));
            Assert.AreEqual(EffectKind.Reactive, EffectDerivation.Kind(Make<EmpowerNextAttackOnHealBuffFactory>()));
            Assert.AreEqual(EffectKind.Reactive, EffectDerivation.Kind(Make<ManaOnKillBuffFactory>()));
            Assert.AreEqual(EffectKind.Reactive, EffectDerivation.Kind(Make<ManaOnOverhealBuffFactory>()));
            Assert.AreEqual(EffectKind.Echo, EffectDerivation.Kind(Make<EchoAttackBuffFactory>()));
            Assert.AreEqual(EffectKind.Link, EffectDerivation.Kind(Make<SoulLinkBuffFactory>()));
            Assert.AreEqual(EffectKind.Summon, EffectDerivation.Kind(Make<ReviveOnDeathBuffFactory>()));
            Assert.AreEqual(EffectKind.Summon, EffectDerivation.Kind(Make<SummonOnKillBuffFactory>()));
            // A tag is metadata: nothing happens to the unit when it lands, so it has no kind of its own
            Assert.AreEqual(EffectKind.Plain, EffectDerivation.Kind(Make<AddTagBuffFactory>()));
        }

        // Soul Link is the one link: its kind and its delivery both read Link, and it has no projectile
        [Test]
        public void Delivery_SoulLink_IsLink()
        {
            ABuffHandlerFactory handler = AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>(
                "Assets/Data/CharacterSkills/SoulLink/BuffHandlerFactory.asset");
            Assert.AreEqual(EffectDelivery.Link, EffectDerivation.Channels(handler, true).delivery);
            foreach (EffectChannels layer in EffectDerivation.Layers(handler, true))
            {
                Assert.AreEqual(EffectDelivery.Link, layer.delivery);
            }

            ABuffHandlerFactory flat = AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>(
                "Assets/Data/CharacterSkills/Shield/BuffHandlerFactory.asset");
            Assert.AreEqual(EffectDelivery.Instant, EffectDerivation.Channels(flat, true).delivery);
        }

        // A growing item's handlers are a Flat and an Upgrade modifier alone; on the holder of the item they grow
        [TestCase("EntityItems/GrowingSeedItem/GrowthBuffHandlerFactory")]
        [TestCase("EventItems/HungeringMaskItem/BattleGrowthBuffHandlerFactory")]
        [TestCase("EventItems/HungeringMaskItem/KillGrowthBuffHandlerFactory")]
        public void Kind_GrowingItemHandler_OnItsHolder_IsGrowth(string path)
        {
            ABuffHandlerFactory handler = AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>("Assets/Data/" + path + ".asset");
            Assert.IsNotNull(handler, path);
            EffectContext context = GrowthContext();
            Assert.AreEqual(EffectKind.Growth, EffectDerivation.Kind(handler, context), path);
            Assert.AreEqual(EffectKind.Growth, EffectDerivation.Channels(handler, true, context).kind, path);
            foreach (EffectChannels layer in EffectDerivation.Layers(handler, true, context))
            {
                Assert.AreEqual(EffectKind.Growth, layer.kind, path);
            }

            Assert.AreNotEqual(EffectKind.Growth, EffectDerivation.Kind(handler, EffectContext.Default), path);
        }

        [Test]
        public void GrowthHandlers_LiveGrowingItems_AreTheirThreeHandlers()
        {
            Assert.AreEqual(3, EffectDerivation.GrowthHandlers(GrowingItems()).Count);
            Assert.AreEqual(0, EffectDerivation.GrowthHandlers(null).Count);
        }

        static List<object> GrowingItems()
        {
            List<object> items = new List<object>();
            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/Data/EntityItems",
                "Assets/Data/EventItems" }))
            {
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset is IGameDataSource source && source.sourceData is GrowingItemData)
                {
                    items.Add(source.sourceData);
                }
            }
            Assert.AreEqual(2, items.Count);
            return items;
        }

        [Test]
        public void Kind_NoHandler_IsPlain()
        {
            Assert.AreEqual(EffectKind.Plain, EffectDerivation.Kind((ABuffHandlerFactory)null));
            Assert.AreEqual(EffectKind.Plain, default(EffectChannels).kind);
        }

        [Test]
        public void Layers_LiveHandler_CarryTheirBuffsKind()
        {
            ABuffHandlerFactory handler = AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>(
                "Assets/Data/EntityItems/RageItem/BuffHandlerFactory.asset");
            foreach (EffectChannels layer in EffectDerivation.Layers(handler, true))
            {
                Assert.AreEqual(EffectKind.Conditional, layer.kind);
            }
        }

        T Make<T>() where T : ScriptableObject
        {
            T instance = ScriptableObject.CreateInstance<T>();
            _owned.Add(instance);
            return instance;
        }
    }
}
