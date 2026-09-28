using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Grammar
{
    public class SpellChannelAssetPinningTests
    {
        public struct HandlerRow
        {
            public string path;
            public EffectOperation operation;
            public EffectAspect aspect;
            public EffectTempo tempo;
            public EffectMagnitude magnitude;
            public EffectTrigger trigger;

            public HandlerRow(string path, EffectOperation operation, EffectAspect aspect,
                              EffectTempo tempo, EffectMagnitude magnitude, EffectTrigger trigger)
            {
                this.path = path;
                this.operation = operation;
                this.aspect = aspect;
                this.tempo = tempo;
                this.magnitude = magnitude;
                this.trigger = trigger;
            }
        }

        public static readonly HandlerRow[] HandlerRows = {
            new HandlerRow("Entities/GuardianEntity/BuffHandlerFactory", EffectOperation.Ward,
                EffectAspect.Prevention, EffectTempo.ForDuration, EffectMagnitude.Light, EffectTrigger.Cast),
            new HandlerRow("Entities/HexerEntity/BuffHandlerFactory", EffectOperation.Bane,
                EffectAspect.Defence, EffectTempo.ForDuration, EffectMagnitude.Light, EffectTrigger.Cast),
            new HandlerRow("Entities/ShamanEntity/BuffHandlerFactory", EffectOperation.Heal,
                EffectAspect.Offence, EffectTempo.Once, EffectMagnitude.Solid, EffectTrigger.Cast),
            new HandlerRow("Entities/WarDrumEntity/BuffHandlerFactory", EffectOperation.Boon,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Heavy, EffectTrigger.Cast),
            new HandlerRow("EntityItems/ArcItem/BuffHandlerFactory", EffectOperation.Boon,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Light, EffectTrigger.Cast),
            new HandlerRow("EntityItems/CarrionItem/BuffHandlerFactory", EffectOperation.Damage,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Light, EffectTrigger.OnDeath),
            new HandlerRow("EntityItems/FrostItem/BuffHandlerFactory", EffectOperation.Bane,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Heavy, EffectTrigger.Cast),
            new HandlerRow("EntityItems/MortarShellItem/BuffHandlerFactory", EffectOperation.Boon,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Light, EffectTrigger.Cast),
            new HandlerRow("EntityItems/PlagueItem/BuffHandlerFactory", EffectOperation.Bane,
                EffectAspect.Defence, EffectTempo.ForDuration, EffectMagnitude.Heavy, EffectTrigger.Cast),
            new HandlerRow("EntityItems/PunchingBagRegenItem/BuffHandlerFactory", EffectOperation.Heal,
                EffectAspect.Offence, EffectTempo.PerPeriod, EffectMagnitude.Light, EffectTrigger.Cast),
            new HandlerRow("EntityItems/RageItem/BuffHandlerFactory", EffectOperation.Boon,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Heavy, EffectTrigger.Cast),
            new HandlerRow("EntityItems/SelfDestructItem/BuffHandlerFactory", EffectOperation.Damage,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Solid, EffectTrigger.OnDeath),
            new HandlerRow("EntityItems/SiphonItem/BuffHandlerFactory", EffectOperation.ManaDrain,
                EffectAspect.Offence, EffectTempo.Once, EffectMagnitude.Light, EffectTrigger.Cast),
            new HandlerRow("EntityItems/VenomItem/BuffHandlerFactory", EffectOperation.Damage,
                EffectAspect.Offence, EffectTempo.PerPeriod, EffectMagnitude.Light, EffectTrigger.Cast),
            new HandlerRow("EntityItems/VolleyItem/BuffHandlerFactory", EffectOperation.Boon,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Light, EffectTrigger.Cast),

            new HandlerRow("CharacterSkills/MultiTargetBuffAttackRate/BuffHandlerFactory", EffectOperation.Boon,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Heavy, EffectTrigger.Cast),
            new HandlerRow("CharacterSkills/MultiTargetReduceDamage/BuffHandlerFactory", EffectOperation.Bane,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Heavy, EffectTrigger.Cast),
            new HandlerRow("CharacterSkills/PoisonSingleTarget/PoisonSingleTarget_BuffHandlerFactory",
                EffectOperation.Damage, EffectAspect.Offence, EffectTempo.PerPeriod, EffectMagnitude.Light, EffectTrigger.Cast),
            new HandlerRow("CharacterSkills/SingleTargetBuffAttackRate/BuffHandlerFactory", EffectOperation.Bane,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Heavy, EffectTrigger.Cast),
            new HandlerRow("Entities/HitArmorBufferEntityEntity/BuffHandlerFactory", EffectOperation.Boon,
                EffectAspect.Defence, EffectTempo.Once, EffectMagnitude.Light, EffectTrigger.Cast),
            new HandlerRow("EntityItems/BoostCellItem/BoostBuffHandlerFactory", EffectOperation.Boon,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Light, EffectTrigger.Cast),
            new HandlerRow("EntityItems/BoostCellItem/BuffHandlerFactory", EffectOperation.Boon,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Light, EffectTrigger.Cast),
            new HandlerRow("EntityItems/BounceItem/BuffHandlerFactory", EffectOperation.Boon,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Light, EffectTrigger.Cast),
            // Conclave applies +20% damage and +20% attack interval through separate timed handlers.
            new HandlerRow("EntityItems/ConclaveItem/New Buff Handler Factory 1", EffectOperation.Boon,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Solid, EffectTrigger.Cast),
            new HandlerRow("EntityItems/ConclaveItem/New Buff Handler Factory", EffectOperation.Bane,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Solid, EffectTrigger.Cast),
            new HandlerRow("EntityItems/ExplodeOnHitItem/BuffHandlerFactory 1", EffectOperation.Boon,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Heavy, EffectTrigger.Cast),
            new HandlerRow("EntityItems/ExplodeOnHitItem/BuffHandlerFactory", EffectOperation.Boon,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Light, EffectTrigger.Cast),
            new HandlerRow("EntityItems/IncreaseDamagePerHitItem/BuffHandlerFactory", EffectOperation.Bane,
                EffectAspect.Defence, EffectTempo.ForDuration, EffectMagnitude.Light, EffectTrigger.Cast),
            new HandlerRow("EntityItems/IncreaseDamageWithProjectileDistanceItem/BuffHandlerFactory", EffectOperation.Boon,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Light, EffectTrigger.Cast),
            new HandlerRow("EntityItems/MultipleShootItem/BuffHandlerFactory", EffectOperation.Boon,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Light, EffectTrigger.Cast),
            new HandlerRow("EntityItems/PoisonItem/BuffHandlerFactory", EffectOperation.Damage,
                EffectAspect.Offence, EffectTempo.PerPeriod, EffectMagnitude.Light, EffectTrigger.Cast),
            new HandlerRow("EntityItems/RegenHpItem/RegenHpItem_BuffHandlerFactory", EffectOperation.Heal,
                EffectAspect.Offence, EffectTempo.PerPeriod, EffectMagnitude.Light, EffectTrigger.Cast),
            new HandlerRow("EntityItems/SlowItem/BuffHandlerFactory", EffectOperation.Bane,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Heavy, EffectTrigger.Cast),
            new HandlerRow("EntityItems/TrinityItem/BuffHandlerFactory", EffectOperation.Boon,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Solid, EffectTrigger.Cast),
            new HandlerRow("PlayerItems/DamageAllEnemyItem/BuffHandlerFactory", EffectOperation.Damage,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Light, EffectTrigger.OnDeath),
            new HandlerRow("PlayerItems/HealAllEntitiesOnRoundEndItem/HealAllEntitiesOnRoundEndItem_BuffHandlerFactory",
                EffectOperation.Heal, EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Light, EffectTrigger.RoundEnd),
            new HandlerRow("PlayerItems/ManaOnRoundEndItem/ManaOnRoundEndItem_BuffHandlerFactory", EffectOperation.Mana,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Light, EffectTrigger.RoundEnd)
        };

        // Pinned from handler duration fields and referenced buff/consumer assets. Cast is the
        // no-owner-context default, not a claim that an equipped/on-hit item is cast by the healer.
        // Multipliers use their fractional deltas directly; additive/consumer values retain DefaultHealth=100
        // and expression base=1 until live target context is supplied.
        [Test]
        public void Operation_LiveHandlers_MatchesAssetRowsWithSameSideFallback()
        {
            AssertRows((row, handler) => Assert.AreEqual(row.operation, EffectDerivation.Operation(handler, true), row.path));
        }

        [Test]
        public void Aspect_LiveHandlers_MatchesAssetRows()
        {
            AssertRows((row, handler) => Assert.AreEqual(row.aspect, EffectDerivation.Aspect(handler), row.path));
        }

        [Test]
        public void Tempo_LiveHandlers_MatchesAssetRows()
        {
            AssertRows((row, handler) => Assert.AreEqual(row.tempo, EffectDerivation.Tempo(handler), row.path));
        }

        [Test]
        public void Magnitude_LiveHandlers_WithDefaultReference_MatchesAssetRows()
        {
            AssertRows((row, handler) => Assert.AreEqual(row.magnitude, EffectDerivation.Magnitude(handler), row.path));
        }

        [Test]
        public void Trigger_LiveHandlers_WithoutOwnerContext_UsesListenersOrCastDefault()
        {
            AssertRows((row, handler) => Assert.AreEqual(row.trigger, EffectDerivation.Trigger(handler), row.path));
        }

        static void AssertRows(Action<HandlerRow, ABuffHandlerFactory> assertion)
        {
            foreach (HandlerRow row in HandlerRows)
            {
                ABuffHandlerFactory handler = AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>(
                    "Assets/Data/" + row.path + ".asset");
                Assert.IsNotNull(handler, row.path);
                assertion(row, handler);
            }
        }

        [Test]
        public void HandlerRows_CoverEveryLiveHandlerAsset()
        {
            HashSet<string> expected = new HashSet<string>();
            foreach (HandlerRow row in HandlerRows) expected.Add("Assets/Data/" + row.path + ".asset");
            string[] guids = AssetDatabase.FindAssets("t:ABuffHandlerFactory", new[] { "Assets/Data" });
            HashSet<string> actual = new HashSet<string>();
            foreach (string guid in guids) actual.Add(AssetDatabase.GUIDToAssetPath(guid));
            CollectionAssert.AreEquivalent(expected, actual, "Every live BuffHandlerFactory must have a pinned row.");
        }

        [Test]
        public void DeliveryRows_CoverEveryProjectilePrefab()
        {
            string[] names = { "BulletSpeed", "ChainLightning", "ChannelingLightning", "CurveBullet",
                "CurveBullet2", "CurveSphereBullet", "LaserBullet", "MortarShell", "StraightLaserBullet", "SwarmBullet" };
            HashSet<string> expected = new HashSet<string>();
            foreach (string name in names) expected.Add("Assets/Prefabs/Projectiles/" + name + ".prefab");
            HashSet<string> actual = new HashSet<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Projectiles" }))
                actual.Add(AssetDatabase.GUIDToAssetPath(guid));
            CollectionAssert.AreEquivalent(expected, actual);
        }

        [TestCase("BulletSpeed", EffectDelivery.Rigid)]
        [TestCase("ChainLightning", EffectDelivery.ChainSync)]
        [TestCase("ChannelingLightning", EffectDelivery.ChainSync)]
        [TestCase("CurveBullet", EffectDelivery.Arc)]
        [TestCase("CurveBullet2", EffectDelivery.Arc)]
        [TestCase("CurveSphereBullet", EffectDelivery.Arc)]
        [TestCase("LaserBullet", EffectDelivery.Arc)]
        [TestCase("MortarShell", EffectDelivery.Arc)]
        // Speed 20 exceeds SpearSpeed=18: the shared reader returns Rigid.
        [TestCase("StraightLaserBullet", EffectDelivery.Rigid)]
        [TestCase("SwarmBullet", EffectDelivery.Swarm)]
        public void Delivery_LiveProjectilePrefabs_MatchesAssetRow(string name, EffectDelivery expected)
        {
            Assert.AreEqual(expected, EffectDerivation.DeliveryChannel(RenderTestAssets.LoadProjectile(name)));
        }

        [Test]
        public void Origin_LiveSourceKinds_MatchDocumentedDefaults()
        {
            GameObject character = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Character.prefab");
            GameObject entity = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BaseEntity.prefab");
            GameObject item = new GameObject("Item source");
            try
            {
                Assert.AreEqual(EffectOrigin.Healer, EffectDerivation.Origin(character));
                Assert.AreEqual(EffectOrigin.Creature, EffectDerivation.Origin(entity));
                Assert.AreEqual(EffectOrigin.Item, EffectDerivation.Origin(item));
            }
            finally
            {
                Object.DestroyImmediate(item);
            }
        }

        [TestCase(1, EffectReach.Single)]
        [TestCase(3, EffectReach.Group)]
        [TestCase(int.MaxValue, EffectReach.All)]
        public void Reach_TargetCount_UsesDocumentedDefaultBands(int targetCount, EffectReach expected)
        {
            Assert.AreEqual(expected, EffectDerivation.Reach(null, targetCount));
        }
    }
}
