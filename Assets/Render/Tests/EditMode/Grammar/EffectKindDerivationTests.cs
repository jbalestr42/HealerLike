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
            { "PlayerItems/ManaOnRoundEndItem/ManaOnRoundEndItem_BuffHandlerFactory", EffectKind.Plain }
        };

        readonly List<Object> _owned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object owned in _owned) if (owned) Object.DestroyImmediate(owned);
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
            var expected = new HashSet<string>();
            foreach (SpellChannelAssetPinningTests.HandlerRow row in SpellChannelAssetPinningTests.HandlerRows)
                expected.Add(row.path);
            CollectionAssert.AreEquivalent(expected, Rows.Keys);
        }

        [Test]
        public void Kind_BoonOffenceHandlers_AllCarryAKind()
        {
            foreach (SpellChannelAssetPinningTests.HandlerRow row in SpellChannelAssetPinningTests.HandlerRows)
            {
                if (row.operation != EffectOperation.Boon || row.aspect != EffectAspect.Offence) continue;
                Assert.AreNotEqual(EffectKind.Plain, Rows[row.path], row.path);
            }
        }

        [Test]
        public void Kind_EachFactory_ReadsItsKind()
        {
            Assert.AreEqual(EffectKind.Projectile, EffectDerivation.Kind(Make<ProjectileBehaviourBuffFactory>()));
            Assert.AreEqual(EffectKind.Volume, EffectDerivation.Kind(Make<MultipleShootBuffFactory>()));
            Assert.AreEqual(EffectKind.Rate, EffectDerivation.Kind(Make<TimeModifierFactory>()));
            Assert.AreEqual(EffectKind.Conditional, EffectDerivation.Kind(Make<HPBasedModifierFactory>()));
            Assert.AreEqual(EffectKind.Positional, EffectDerivation.Kind(Make<BoostEntitiesOnRelativeCellBuffFactory>()));
            Assert.AreEqual(EffectKind.Flat, EffectDerivation.Kind(Make<FlatModifierFactory>()));
            Assert.AreEqual(EffectKind.Plain, EffectDerivation.Kind(Make<InvincibilityBuffFactory>()));
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
                Assert.AreEqual(EffectKind.Conditional, layer.kind);
        }

        T Make<T>() where T : ScriptableObject
        {
            T instance = ScriptableObject.CreateInstance<T>();
            _owned.Add(instance);
            return instance;
        }
    }
}
