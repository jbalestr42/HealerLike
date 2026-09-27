using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Grammar
{
    public class SpellChannelAssetPinningTests
    {
        struct HandlerRow
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

        static readonly HandlerRow[] HandlerRows = {
            new HandlerRow("CharacterSkills/MultiTargetBuffAttackRate/BuffHandlerFactory", EffectOperation.Boon,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Light, EffectTrigger.Cast),
            new HandlerRow("CharacterSkills/MultiTargetReduceDamage/BuffHandlerFactory", EffectOperation.Bane,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Light, EffectTrigger.Cast),
            new HandlerRow("CharacterSkills/PoisonSingleTarget/PoisonSingleTarget_BuffHandlerFactory",
                EffectOperation.Damage, EffectAspect.Offence, EffectTempo.PerPeriod, EffectMagnitude.Light, EffectTrigger.Cast),
            new HandlerRow("CharacterSkills/SingleTargetBuffAttackRate/BuffHandlerFactory", EffectOperation.Bane,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Light, EffectTrigger.Cast),
            new HandlerRow("Entities/HitArmorBufferEntityEntity/BuffHandlerFactory", EffectOperation.Ward,
                EffectAspect.Defence, EffectTempo.Once, EffectMagnitude.Light, EffectTrigger.Cast),
            new HandlerRow("EntityItems/BoostCellItem/BoostBuffHandlerFactory", EffectOperation.Boon,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Light, EffectTrigger.Cast),
            new HandlerRow("EntityItems/BoostCellItem/BuffHandlerFactory", EffectOperation.Boon,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Light, EffectTrigger.Cast),
            new HandlerRow("EntityItems/BounceItem/BuffHandlerFactory", EffectOperation.Boon,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Light, EffectTrigger.Cast),
            new HandlerRow("EntityItems/ExplodeOnHitItem/BuffHandlerFactory 1", EffectOperation.Boon,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Light, EffectTrigger.Cast),
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
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Light, EffectTrigger.Cast),
            new HandlerRow("EntityItems/TrinityItem/BuffHandlerFactory", EffectOperation.Boon,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Light, EffectTrigger.Cast),
            new HandlerRow("PlayerItems/DamageAllEnemyItem/BuffHandlerFactory", EffectOperation.Damage,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Light, EffectTrigger.Cast),
            new HandlerRow("PlayerItems/HealAllEntitiesOnRoundEndItem/HealAllEntitiesOnRoundEndItem_BuffHandlerFactory",
                EffectOperation.Heal, EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Light, EffectTrigger.RoundEnd),
            new HandlerRow("PlayerItems/ManaOnRoundEndItem/ManaOnRoundEndItem_BuffHandlerFactory", EffectOperation.Mana,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Light, EffectTrigger.RoundEnd)
        };

        [Test]
        public void Channels_LiveHandlers_MatchAssetAuthoredRows()
        {
            HashSet<string> expected = new HashSet<string>();
            foreach (HandlerRow row in HandlerRows)
            {
                expected.Add("Assets/Data/" + row.path + ".asset");
                ABuffHandlerFactory handler = AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>(
                    "Assets/Data/" + row.path + ".asset");
                Assert.IsNotNull(handler, row.path);

                EffectChannels channels = EffectDerivation.Channels(handler, true);
                Assert.AreEqual(row.operation, channels.operation, row.path);
                Assert.AreEqual(row.aspect, channels.aspect, row.path);
                Assert.AreEqual(row.tempo, channels.tempo, row.path);
                Assert.AreEqual(row.magnitude, channels.magnitude, row.path);
                Assert.AreEqual(row.trigger, channels.trigger, row.path);
            }

            string[] guids = AssetDatabase.FindAssets("t:ABuffHandlerFactory", new[] { "Assets/Data" });
            HashSet<string> actual = new HashSet<string>();
            foreach (string guid in guids) actual.Add(AssetDatabase.GUIDToAssetPath(guid));
            CollectionAssert.AreEquivalent(expected, actual, "Every live BuffHandlerFactory must have a pinned row.");
        }

        [TestCase("BulletSpeed", EffectDelivery.Rigid)]
        [TestCase("ChainLightning", EffectDelivery.ChainSync)]
        [TestCase("ChannelingLightning", EffectDelivery.ChainSync)]
        [TestCase("CurveBullet", EffectDelivery.Arc)]
        [TestCase("CurveBullet2", EffectDelivery.Arc)]
        [TestCase("CurveSphereBullet", EffectDelivery.Arc)]
        [TestCase("LaserBullet", EffectDelivery.Arc)]
        [TestCase("StraightLaserBullet", EffectDelivery.Rigid)]
        [TestCase("SwarmBullet", EffectDelivery.Swarm)]
        public void Delivery_LiveProjectilePrefabs_MatchesAssetRow(string name, EffectDelivery expected)
        {
            Assert.AreEqual(expected, EffectDerivation.DeliveryChannel(RenderTestAssets.LoadProjectile(name)));
        }

        [Test]
        public void Origin_LiveSourceKinds_MatchDocumentedDefaults()
        {
            GameObject character = new GameObject("Character source");
            GameObject entity = new GameObject("Entity source");
            GameObject item = new GameObject("Item source");
            try
            {
                character.AddComponent<Character>();
                entity.AddComponent<Entity>();
                Assert.AreEqual(EffectOrigin.Healer, EffectDerivation.Origin(character));
                Assert.AreEqual(EffectOrigin.Creature, EffectDerivation.Origin(entity));
                Assert.AreEqual(EffectOrigin.Item, EffectDerivation.Origin(item));
            }
            finally
            {
                Object.DestroyImmediate(character);
                Object.DestroyImmediate(entity);
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
