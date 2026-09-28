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

            // The legacy CharacterSkills and PlayerItems rows below belong to no class and keep the plain context
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
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Light, EffectTrigger.RoundEnd),

            // Julien's Cleric, Druid and Warlock content (ade6ad91). A class's own skills and items are sized as it
            // casts them, at its base stats (Cleric HealPower 25, Druid 20, Warlock 15, no class has HealthMax so a
            // heal reads against 100). Periodic effects are sized per tick, like every creature effect.
            // Curse (Warlock): 0.3 x HealPower damage each second for 6s, 0.3 x 15 = 4.5 per tick, 4.5 / 100 = 0.045
            new HandlerRow("CharacterSkills/Curse/BuffHandlerFactory", EffectOperation.Damage,
                EffectAspect.Offence, EffectTempo.PerPeriod, EffectMagnitude.Light, EffectTrigger.Cast),
            // Divine Intervention (Cleric): invincible for 2s, no harm and no modifier, share 0
            new HandlerRow("CharacterSkills/DivineIntervention/BuffHandlerFactory", EffectOperation.Ward,
                EffectAspect.Prevention, EffectTempo.ForDuration, EffectMagnitude.Light, EffectTrigger.Cast),
            // Rejuvenation (Druid): 0.15 x HealPower heal each second for 8s, 0.15 x 20 = 3 per tick, 0.03
            new HandlerRow("CharacterSkills/Rejuvenation/BuffHandlerFactory", EffectOperation.Heal,
                EffectAspect.Offence, EffectTempo.PerPeriod, EffectMagnitude.Light, EffectTrigger.Cast),
            // Shield (Cleric): +0.5 PercentArmor added for 5s. Damage taken is scaled by (1 - PercentArmor), so the
            // added fraction is its own share: 0.5 > 0.4, Heavy
            new HandlerRow("CharacterSkills/Shield/BuffHandlerFactory", EffectOperation.Boon,
                EffectAspect.Defence, EffectTempo.ForDuration, EffectMagnitude.Heavy, EffectTrigger.Cast),
            // Wild Growth (Druid): 0.1 x HealPower heal each second for 6s, 0.1 x 20 = 2 per tick, 0.02
            new HandlerRow("CharacterSkills/WildGrowth/BuffHandlerFactory", EffectOperation.Heal,
                EffectAspect.Offence, EffectTempo.PerPeriod, EffectMagnitude.Light, EffectTrigger.Cast),
            // Grove Keeper: a flat 2 heal each second for 4s
            new HandlerRow("Entities/GroveKeeperEntity/BuffHandlerFactory", EffectOperation.Heal,
                EffectAspect.Offence, EffectTempo.PerPeriod, EffectMagnitude.Light, EffectTrigger.Cast),
            // Blood Bond: life steal heals the most wounded ally for as long as the item is held
            new HandlerRow("EntityItems/BloodBondItem/BuffHandlerFactory", EffectOperation.Heal,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Light, EffectTrigger.Cast),
            // Sap: a flat 3 heal every 2s while held
            new HandlerRow("EntityItems/SapItem/BuffHandlerFactory", EffectOperation.Heal,
                EffectAspect.Offence, EffectTempo.PerPeriod, EffectMagnitude.Light, EffectTrigger.Cast),
            // Zeal: Damage x1.5 while above 70% health, a +0.5 share
            new HandlerRow("EntityItems/ZealItem/BuffHandlerFactory", EffectOperation.Boon,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Heavy, EffectTrigger.Cast),
            // Sacred Tome (Cleric's starting item): +10 HealPower against the Cleric's 25, 10 / 25 = 0.40, on the
            // inclusive Solid bound
            new HandlerRow("PlayerItems/HealPowerItem/BuffHandlerFactory", EffectOperation.Boon,
                EffectAspect.Offence, EffectTempo.ForDuration, EffectMagnitude.Solid, EffectTrigger.Cast),
            // Verdant (Druid's starting item): 0.05 x HealPower heal on every player entity every 2s, 0.05 x 20 = 1
            // per tick, 0.01
            new HandlerRow("PlayerItems/VerdantItem/BuffHandlerFactory", EffectOperation.Heal,
                EffectAspect.Offence, EffectTempo.PerPeriod, EffectMagnitude.Light, EffectTrigger.Cast)
        };

        // Pinned from handler duration fields and referenced buff/consumer assets. Cast is the
        // no-owner-context default, not a claim that an equipped/on-hit item is cast by the healer.
        // Multipliers use their fractional deltas directly; additive/consumer values retain DefaultHealth=100
        // and expression base=1 until live target context is supplied, except a class's own skills and items,
        // sized at that class's base stats (PlayerClassContext).
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
        public void Magnitude_LiveHandlers_SizedByTheirOwningClass_MatchesAssetRows()
        {
            List<CharacterData> characters = Characters();
            AssertRows((row, handler) => Assert.AreEqual(row.magnitude, EffectDerivation.Magnitude(handler,
                PlayerClassContext.For(handler, EffectContext.Default, characters)), row.path));
        }

        // Every creature row is sized with no class at all: none of them may read a class's stats
        [Test]
        public void Owner_CreatureHandlers_BelongToNoClass()
        {
            List<CharacterData> characters = Characters();
            AssertRows((row, handler) =>
            {
                if (row.path.StartsWith("Entities/") || row.path.StartsWith("EntityItems/"))
                    Assert.IsNull(PlayerClassContext.Owner(handler, characters), row.path);
            });
        }

        [TestCase("CharacterSkills/Shield/BuffHandlerFactory", "ClericCharacter")]
        [TestCase("CharacterSkills/DivineIntervention/BuffHandlerFactory", "ClericCharacter")]
        [TestCase("PlayerItems/HealPowerItem/BuffHandlerFactory", "ClericCharacter")]
        [TestCase("CharacterSkills/Rejuvenation/BuffHandlerFactory", "DruidCharacter")]
        [TestCase("CharacterSkills/WildGrowth/BuffHandlerFactory", "DruidCharacter")]
        [TestCase("PlayerItems/VerdantItem/BuffHandlerFactory", "DruidCharacter")]
        [TestCase("CharacterSkills/Curse/BuffHandlerFactory", "WarlockCharacter")]
        [TestCase("CharacterSkills/MultiTargetBuffAttackRate/BuffHandlerFactory", null)]
        [TestCase("PlayerItems/DamageAllEnemyItem/BuffHandlerFactory", null)]
        public void Owner_LiveHandlers_IsTheClassListingThem(string path, string expected)
        {
            ABuffHandlerFactory handler = AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>("Assets/Data/" + path + ".asset");
            Assert.IsNotNull(handler, path);
            CharacterData owner = PlayerClassContext.Owner(handler, Characters());
            Assert.AreEqual(expected, owner == null ? null : owner.name, path);
        }

        // The instant class heals are skills, not handlers: 1 x HealPower scaled by the skill multiplier.
        // Heal (Cleric): 25 x 1.5 = 37.5, 0.375 Solid. Heal Group (Cleric): 25 x 0.8 = 20, 0.2 Solid
        [TestCase("CharacterSkills/HealSingleTarget/HealSingleTarget", EffectMagnitude.Solid)]
        [TestCase("CharacterSkills/HealMultiTarget/HealMultiTarget", EffectMagnitude.Solid)]
        public void Magnitude_LiveClassHealSkills_SizedByTheirOwningClass(string path, EffectMagnitude expected)
        {
            ACharacterSkillFactory factory = AssetDatabase.LoadAssetAtPath<ACharacterSkillFactory>(
                "Assets/Data/" + path + ".asset");
            CharacterSkillData data = ((IGameDataSource)factory).sourceData as CharacterSkillData;
            SpellIconDescription description = SpellIconDerivation.Read(data, PlayerClassContext.Owner(data, Characters()));
            Assert.AreEqual(1, description.layers.Count, path);
            Assert.AreEqual(EffectOperation.Heal, description.layers[0].operation, path);
            Assert.AreEqual(expected, description.layers[0].magnitude, path);
        }

        static List<CharacterData> Characters()
        {
            List<string> paths = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:CharacterData", new[] { "Assets/Data" }))
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            paths.Sort(StringComparer.Ordinal);
            List<CharacterData> characters = new List<CharacterData>();
            foreach (string path in paths) characters.Add(AssetDatabase.LoadAssetAtPath<CharacterData>(path));
            Assert.AreEqual(3, characters.Count);
            return characters;
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
