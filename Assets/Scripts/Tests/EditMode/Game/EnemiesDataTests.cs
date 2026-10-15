using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game
{

// Checks the enemies of the project data and the waves fielding them
public class EnemiesDataTests
{
    const string EntitiesPath = "Assets/Data/Entities/";
    const string WavesPath = "Assets/Data/WavePatterns/";

    static EntityData LoadEnemy(string name)
    {
        EntityData enemy = AssetDatabase.LoadAssetAtPath<EntityData>(EntitiesPath + name + "Entity/" + name + "Entity.asset");
        Assert.IsNotNull(enemy, name);
        return enemy;
    }

    static WavePatternData LoadWave(string name)
    {
        WavePatternData wave = AssetDatabase.LoadAssetAtPath<WavePatternData>(WavesPath + name + ".asset");
        Assert.IsNotNull(wave, name);
        return wave;
    }

    // Titles of the enemies of a rank of the wave, 0 being the front one, empty slots left out
    static List<string> GetRank(WavePatternData wave, int rank)
    {
        List<string> titles = new List<string>();
        foreach ((int rank, int y, EntityData entity) unit in wave.GetUnitsByRank())
        {
            if (unit.rank == rank)
            {
                titles.Add(unit.entity.title);
            }
        }
        return titles;
    }

    // Ranks from the front one to the last one holding a unit
    static int GetRankCount(WavePatternData wave)
    {
        int count = 0;
        foreach ((int rank, int y, EntityData entity) unit in wave.GetUnitsByRank())
        {
            count = Mathf.Max(count, unit.rank + 1);
        }
        return count;
    }

    // Damage of a flat consumer
    static float GetFlatDamage(AConsumerFactory consumer)
    {
        ConsumerFactory factory = consumer as ConsumerFactory;
        Assert.IsNotNull(factory);
        FlatValue value = factory.data.value as FlatValue;
        Assert.IsNotNull(value);
        return value.GetValue(null);
    }

    // The buff of the single always-on buff handler of the single item of the enemy
    static T GetItemBuff<T>(EntityData enemy) where T : ABuffFactory
    {
        Assert.AreEqual(1, enemy.items.Count, enemy.title);
        ItemFactory item = enemy.items[0] as ItemFactory;
        Assert.IsNotNull(item, enemy.title);
        Assert.IsFalse(string.IsNullOrEmpty(item.data.name), enemy.title);
        Assert.IsFalse(string.IsNullOrEmpty(item.data.description), enemy.title);
        ABuffHandlerFactory handler = item.data.buffs[0];
        Assert.AreEqual(DurationType.Infinite, handler.durationType, enemy.title);
        T buff = handler.buffFactoryList[0] as T;
        Assert.IsNotNull(buff, enemy.title);
        return buff;
    }

    // Armored, strikes twice then raises its guard: blocks 2 damage of each hit, then the next one whole
    [Test]
    public void ArmoredFootman_StrikesTwiceThenRaisesItsGuard()
    {
        EntityData footman = LoadEnemy("ArmoredFootman");

        Assert.AreEqual("Armored Footman", footman.title);
        Assert.IsFalse(string.IsNullOrEmpty(footman.description));
        Assert.IsNotNull(footman.model);
        Assert.AreEqual(2f, footman.attributes[AttributeType.FlatArmor]);
        Assert.AreEqual(TargetBehaviourType.Nearest, footman.targetBehaviourType);
        Assert.AreEqual(1, footman.skillFactories.Count);
        ConfigurableSkillFactory skill = footman.skillFactories[0] as ConfigurableSkillFactory;
        Assert.IsNotNull(skill);
        List<ASkillStepFactory> steps = skill.data.skillStepFactories;
        Assert.AreEqual(3, steps.Count);

        // Two strikes, each followed by a wait of half its attack rate (0.5s)
        RepeatSkillStepFactory strikes = steps[0] as RepeatSkillStepFactory;
        Assert.IsNotNull(strikes);
        Assert.AreEqual(2, strikes.data.count);
        Assert.IsTrue(strikes.shootsAtTargets);
        DurationSkillStepFactory strikeDelay = strikes.data.skillStepFactories[1] as DurationSkillStepFactory;
        Assert.IsNotNull(strikeDelay);
        AttributeValue delay = strikeDelay.data.duration as AttributeValue;
        Assert.IsNotNull(delay);
        Assert.AreEqual(AttributeType.AttackRate, delay.data.type);
        Assert.AreEqual(0.5f, delay.data.multiplier * footman.attributes[AttributeType.AttackRate], 0.0001f);

        // The guard: the next hit blocked, a single one however long it is not hit
        ApplyBuffSkillStepFactory guard = steps[1] as ApplyBuffSkillStepFactory;
        Assert.IsNotNull(guard);
        Assert.AreEqual(DurationType.Instant, guard.data.buffHandlerFactory.durationType);
        FlatModifierFactory hitArmor = guard.data.buffHandlerFactory.buffFactoryList[0] as FlatModifierFactory;
        Assert.IsNotNull(hitArmor);
        Assert.AreEqual(AttributeType.HitArmor, hitArmor.data.type);
        Assert.AreEqual(AttributeModifierType.Override, hitArmor.data.modifierType);
        Assert.AreEqual(1f, hitArmor.data.value);

        // Then a pause in guard
        Assert.IsInstanceOf<DurationSkillStepFactory>(steps[2]);
    }

    // Goes for the unit with the most health (the tank), strikes it 3 times in a row, then pauses for 2s
    [Test]
    public void Duelist_StrikesTheHealthiestUnitThreeTimesThenPauses()
    {
        EntityData duelist = LoadEnemy("Duelist");

        Assert.AreEqual("Duelist", duelist.title);
        Assert.IsFalse(string.IsNullOrEmpty(duelist.description));
        Assert.IsNotNull(duelist.model);
        Assert.AreEqual(TargetBehaviourType.HighestHealth, duelist.targetBehaviourType);
        Assert.AreEqual(1, duelist.skillFactories.Count);
        ConfigurableSkillFactory skill = duelist.skillFactories[0] as ConfigurableSkillFactory;
        Assert.IsNotNull(skill);
        List<ASkillStepFactory> steps = skill.data.skillStepFactories;
        Assert.AreEqual(2, steps.Count);

        // Three strikes in a row, a short wait after each one
        RepeatSkillStepFactory strikes = steps[0] as RepeatSkillStepFactory;
        Assert.IsNotNull(strikes);
        Assert.AreEqual(3, strikes.data.count);
        Assert.IsTrue(strikes.shootsAtTargets);
        AttributeValue strikeDelay = (strikes.data.skillStepFactories[1] as DurationSkillStepFactory).data.duration as AttributeValue;
        Assert.IsNotNull(strikeDelay);
        Assert.AreEqual(AttributeType.AttackRate, strikeDelay.data.type);
        Assert.Less(strikeDelay.data.multiplier * duelist.attributes[AttributeType.AttackRate], 0.5f);

        // Then the pause of 2s, shorter when its attacks are sped up (e.g. by a War Drum)
        DurationSkillStepFactory pause = steps[1] as DurationSkillStepFactory;
        Assert.IsNotNull(pause);
        AttributeValue pauseDuration = pause.data.duration as AttributeValue;
        Assert.IsNotNull(pauseDuration);
        Assert.AreEqual(AttributeType.AttackRate, pauseDuration.data.type);
        Assert.AreEqual(2f, pauseDuration.data.multiplier * duelist.attributes[AttributeType.AttackRate], 0.0001f);
    }

    // Strikes the most wounded unit, twice as hard when it is under half health
    [Test]
    public void Headsman_HitsTheMostWoundedUnit_TwiceAsHardUnderHalfHealth()
    {
        EntityData headsman = LoadEnemy("Headsman");

        Assert.AreEqual("Headsman", headsman.title);
        Assert.IsFalse(string.IsNullOrEmpty(headsman.description));
        Assert.IsNotNull(headsman.model);
        Assert.AreEqual(TargetBehaviourType.LowestHealth, headsman.targetBehaviourType);
        Assert.AreEqual(1, headsman.skillFactories.Count);
        ShootProjectileSkillFactory skill = headsman.skillFactories[0] as ShootProjectileSkillFactory;
        Assert.IsNotNull(skill);

        WoundedTargetConsumerFactory hit = skill.data.projectiles[0].onHitConsumer[0] as WoundedTargetConsumerFactory;
        Assert.IsNotNull(hit);
        Assert.AreEqual(0.5f, hit.data.healthThreshold, 0.0001f);
        Assert.AreEqual(2f, hit.data.multiplier, 0.0001f);
        AttributeValue damage = hit.data.value as AttributeValue;
        Assert.IsNotNull(damage);
        Assert.AreEqual(AttributeType.Damage, damage.data.type);
        Assert.AreEqual(ConsumerValueOwner.Source, hit.data.valueOwner);
    }

    [Test]
    public void Kamikaze_HasNoFuse()
    {
        EntityData kamikaze = LoadEnemy("Kamikaze");

        Assert.IsFalse(kamikaze.skillFactories.Exists(factory => factory is FuseSkillFactory));
    }

    [Test]
    public void LitKamikaze_BlowsUpByItselfAfter10To13sFor20DamageInAll()
    {
        EntityData kamikaze = LoadEnemy("LitKamikaze");
        FuseSkillFactory fuse = (FuseSkillFactory)kamikaze.skillFactories.Find(factory => factory is FuseSkillFactory);
        Assert.IsNotNull(fuse, "the Lit Kamikaze has no fuse");
        DamageAllEntityOnEntityDieBuffFactory selfDestruct = GetItemBuff<DamageAllEntityOnEntityDieBuffFactory>(kamikaze);

        Assert.AreEqual("Lit Kamikaze", kamikaze.title);
        Assert.AreEqual(10f, fuse.data.minDelay, 0.0001f);
        Assert.AreEqual(13f, fuse.data.maxDelay, 0.0001f);
        // Its death, caused by the fuse, also triggers its Self-Destruct
        Assert.AreEqual(12f, GetFlatDamage(selfDestruct.data.damageToAllEntity), 0.0001f);
        Assert.AreEqual(20f, GetFlatDamage(fuse.data.explosion) + GetFlatDamage(selfDestruct.data.damageToAllEntity), 0.0001f);
        StringAssert.Contains("20", kamikaze.description);
    }

    [Test]
    public void VengefulSniper_ShootsLikeTheSniper()
    {
        EntityData sniper = LoadEnemy("Sniper");
        EntityData vengeful = LoadEnemy("VengefulSniper");

        Assert.AreEqual("Vengeful Sniper", vengeful.title);
        Assert.IsFalse(string.IsNullOrEmpty(vengeful.description));
        Assert.IsNotNull(vengeful.model);
        Assert.AreEqual(sniper.targetBehaviourType, vengeful.targetBehaviourType);
        CollectionAssert.AreEquivalent(sniper.attributes, vengeful.attributes);
        Assert.AreEqual(1, vengeful.skillFactories.Count, "it attacks");
    }

    [Test]
    public void VengefulSniper_EachAllyDeathGivesPlus100PercentAttackSpeedFor6s_UpTo200()
    {
        ABuffHandlerFactory vengeance = GetItemBuff<ApplyBuffOnAllyDeathBuffFactory>(LoadEnemy("VengefulSniper")).data.buffHandlerFactory;

        Assert.AreEqual(DurationType.Duration, vengeance.durationType);
        Assert.AreEqual(6f, vengeance.duration, 0.0001f);
        // A second death stacks it, a third one only refreshes it
        Assert.AreEqual(2, vengeance.maxStacks);
        Assert.AreEqual(1, vengeance.buffFactoryList.Count);
        AttackSpeedModifierFactory modifier = vengeance.buffFactoryList[0] as AttackSpeedModifierFactory;
        Assert.IsNotNull(modifier);
        Assert.AreEqual(AttributeType.AttackRate, modifier.data.type);
        Assert.AreEqual(AttributeModifierType.Multiply, modifier.data.modifierType);
        // +100% per stack: twice then three times as many attacks
        Assert.AreEqual(1f, modifier.data.value, 0.0001f);
        Assert.IsEmpty(vengeance.tags, "removed at the end of the fight");
    }

    [Test]
    public void Fuse_FieldsOneLitKamikazeInFrontAndTwoVengefulSnipersBehind()
    {
        WavePatternData wave = LoadWave("Wave_Fuse");

        CollectionAssert.AreEqual(new[] { "Lit Kamikaze" }, GetRank(wave, 0));
        CollectionAssert.AreEqual(new[] { "Vengeful Sniper", "Vengeful Sniper" }, GetRank(wave, 1));
    }

    [Test]
    public void ArcMage_LightningChainsThroughFourUnits()
    {
        BounceProjectileBehaviourFactory bounce = AssetDatabase.LoadAssetAtPath<BounceProjectileBehaviourFactory>("Assets/Data/EntityItems/ArcItem/BounceProjectileBehaviourFactory.asset");
        Assert.IsNotNull(bounce);

        // The first target, then 3 jumps
        Assert.AreEqual(3, bounce.data.bounce);
        StringAssert.Contains("4", LoadEnemy("ArcMage").description);
    }

    [Test]
    public void Storm_FieldsThreeSoldiersInFrontAndTheWarDrumBetweenTwoArcMages()
    {
        WavePatternData wave = LoadWave("Wave_Storm");

        CollectionAssert.AreEqual(new[] { "Soldier", "Soldier", "Soldier" }, GetRank(wave, 0));
        CollectionAssert.AreEqual(new[] { "Arc Mage", "War Drum", "Arc Mage" }, GetRank(wave, 1));
    }

    [Test]
    public void Bannerman_GivesPlus2DamageToTheAlliesOnThe8CellsAroundIt()
    {
        EntityData bannerman = LoadEnemy("Bannerman");
        BoostEntitiesOnRelativeCellBuffFactory banner = GetItemBuff<BoostEntitiesOnRelativeCellBuffFactory>(bannerman);

        Assert.AreEqual("Bannerman", bannerman.title);
        Assert.IsFalse(string.IsNullOrEmpty(bannerman.description));
        Assert.IsNotNull(bannerman.model);
        Assert.IsEmpty(bannerman.skillFactories, "it doesn't attack");
        Assert.IsFalse(bannerman.HasTag(TagNames.Reward), "an enemy, never a reward");
        Assert.AreEqual(RelativeCellPatternType.Adjacent, banner.data.pattern);
        Assert.AreEqual(1, banner.data.range);
        FlatModifierFactory modifier = banner.data.buffHandlerFactory.buffFactoryList[0] as FlatModifierFactory;
        Assert.IsNotNull(modifier);
        Assert.AreEqual(AttributeType.Damage, modifier.data.type);
        Assert.AreEqual(AttributeModifierType.Add, modifier.data.modifierType);
        Assert.AreEqual(2f, modifier.data.value, 0.0001f);
    }

    [Test]
    public void EliteBastion_FieldsTheBannermanNextToAMortarBehindTheColossusAndTheShamanBehindIt()
    {
        WavePatternData wave = LoadWave("Wave_Elite_Bastion");

        Assert.AreEqual(3, GetRankCount(wave));
        CollectionAssert.AreEqual(new[] { "Soldier", "Colossus" }, GetRank(wave, 0));
        CollectionAssert.AreEqual(new[] { "Bannerman", "Mortar" }, GetRank(wave, 1));
        CollectionAssert.AreEqual(new[] { "Shaman" }, GetRank(wave, 2));
        // In the middle, the Bannerman boosts every unit of the wave
        Assert.AreEqual("Bannerman", wave.GetEntity(wave.frontColumn + 1, 1).title);
    }

    [Test]
    public void BloodWarden_GivesPlus50PercentArmorToItsAlliesUnderHalfHealth()
    {
        EntityData warden = LoadEnemy("BloodWarden");
        ApplyBuffToAlliesBuffFactory ward = GetItemBuff<ApplyBuffToAlliesBuffFactory>(warden);

        Assert.AreEqual("Blood Warden", warden.title);
        Assert.IsFalse(string.IsNullOrEmpty(warden.description));
        Assert.IsNotNull(warden.model);
        Assert.IsEmpty(warden.skillFactories, "it doesn't attack");
        Assert.IsFalse(warden.HasTag(TagNames.Reward), "an enemy, never a reward");
        Assert.AreEqual(DurationType.Infinite, ward.data.buffHandlerFactory.durationType);
        HealthThresholdModifierFactory modifier = ward.data.buffHandlerFactory.buffFactoryList[0] as HealthThresholdModifierFactory;
        Assert.IsNotNull(modifier);
        Assert.AreEqual(AttributeType.PercentArmor, modifier.data.type);
        Assert.AreEqual(AttributeModifierType.Add, modifier.data.modifierType);
        Assert.AreEqual(0.5f, modifier.data.value, 0.0001f);
        Assert.AreEqual(0.5f, modifier.data.threshold, 0.0001f);
        Assert.IsTrue(modifier.data.isBelow);
    }

    [Test]
    public void Berserker_Deals5Damage()
    {
        Assert.AreEqual(5f, LoadEnemy("Berserker").attributes[AttributeType.Damage]);
    }

    [Test]
    public void EliteCult_FieldsThreeBerserkersInFrontProtectedByTheGuardianAndTheBloodWarden()
    {
        WavePatternData wave = LoadWave("Wave_Elite_Cult");

        Assert.AreEqual(3, GetRankCount(wave));
        CollectionAssert.AreEqual(new[] { "Berserker", "Berserker", "Berserker" }, GetRank(wave, 0));
        CollectionAssert.AreEqual(new[] { "Hexer", "Guardian", "Hexer" }, GetRank(wave, 1));
        CollectionAssert.AreEqual(new[] { "War Drum", "Blood Warden" }, GetRank(wave, 2));
    }

    [Test]
    public void Necromancer_KeepsUpTo2FrailSkeletonsAtATime()
    {
        EntityData necromancer = LoadEnemy("Necromancer");
        SummonSkillFactory summon = (SummonSkillFactory)necromancer.skillFactories.Find(factory => factory is SummonSkillFactory);
        Assert.IsNotNull(summon, "the Necromancer raises nothing");

        Assert.AreEqual(2, summon.data.maxAlive);
        Assert.AreEqual("Skeleton", summon.data.entity.title);
        Assert.AreEqual(15f, summon.data.entity.attributes[AttributeType.HealthMax]);
        StringAssert.Contains("up to 2", necromancer.description);
    }

    [Test]
    public void Siphoner_EachHitDrains1Mana()
    {
        ItemFactory siphon = LoadEnemy("Siphoner").items[0] as ItemFactory;
        Assert.IsNotNull(siphon);
        DrainCharacterManaBuffFactory drain = siphon.data.onHitEffects[0].buffFactoryList[0] as DrainCharacterManaBuffFactory;
        Assert.IsNotNull(drain);

        Assert.AreEqual(1f, GetFlatDamage(drain.data.consumerFactory), 0.0001f);
        StringAssert.Contains("drain 1 ", siphon.data.description);
    }

    [Test]
    public void Archer_ShootsWeakShotsAtYourFarthestUnit()
    {
        EntityData archer = LoadEnemy("Archer");

        Assert.AreEqual("Archer", archer.title);
        Assert.IsFalse(string.IsNullOrEmpty(archer.description));
        Assert.IsNotNull(archer.model);
        Assert.AreEqual(TargetBehaviourType.Farest, archer.targetBehaviourType);
        Assert.AreEqual(100f, archer.attributes[AttributeType.HealthMax]);
        Assert.AreEqual(3f, archer.attributes[AttributeType.Damage]);
        // A shot every second
        Assert.AreEqual(1f, archer.attributes[AttributeType.AttackRate]);
        Assert.AreEqual(1, archer.skillFactories.Count, "it attacks");
    }

    [Test]
    public void Crossbowman_ShootsSlowHeavyBoltsAtYourFarthestUnit()
    {
        EntityData crossbowman = LoadEnemy("Crossbowman");

        Assert.AreEqual("Crossbowman", crossbowman.title);
        Assert.IsFalse(string.IsNullOrEmpty(crossbowman.description));
        Assert.IsNotNull(crossbowman.model);
        Assert.AreEqual(TargetBehaviourType.Farest, crossbowman.targetBehaviourType);
        Assert.AreEqual(100f, crossbowman.attributes[AttributeType.HealthMax]);
        Assert.AreEqual(7f, crossbowman.attributes[AttributeType.Damage]);
        // A bolt every 2.5s
        Assert.AreEqual(2.5f, crossbowman.attributes[AttributeType.AttackRate]);
        Assert.AreEqual(1, crossbowman.skillFactories.Count, "it attacks");
    }

    [Test]
    public void HiddenHealer_FieldsTwoSoldiersInFrontAndTheShamanBetweenTwoCrossbowmen()
    {
        WavePatternData wave = LoadWave("Wave_HiddenHealer");

        CollectionAssert.AreEqual(new[] { "Soldier", "Soldier" }, GetRank(wave, 0));
        CollectionAssert.AreEqual(new[] { "Crossbowman", "Shaman", "Crossbowman" }, GetRank(wave, 1));
    }

    [Test]
    public void FrontLine_FieldsTwoSoldiersInFrontAndThreeArchersBehind()
    {
        WavePatternData wave = LoadWave("Wave_FrontLine");

        Assert.AreEqual(2, GetRankCount(wave));
        CollectionAssert.AreEqual(new[] { "Soldier", "Soldier" }, GetRank(wave, 0));
        CollectionAssert.AreEqual(new[] { "Archer", "Archer", "Archer" }, GetRank(wave, 1));
    }
}

}
